using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace RIIS.Academic.Logging;

public static class RiisSerilogConfiguration
{
    public static void InitializeBootstrap()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();
    }

    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, _, loggerConfiguration) =>
            ConfigureLogger(context, loggerConfiguration));
    }

    public static void UseRequestLogging(WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnostics, httpContext) =>
            {
                diagnostics.Set("RequestHost", httpContext.Request.Host.Value);
                diagnostics.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            };
        });
    }

    public static void Fatal(Exception exception, string message)
    {
        Log.Fatal(exception, message);
    }

    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }

    private static void ConfigureLogger(
        HostBuilderContext context,
        LoggerConfiguration loggerConfiguration)
    {
        var configuration = context.Configuration;
        var instanceId = Environment.MachineName;
        var environmentName = context.HostingEnvironment.EnvironmentName;
        var sqlCommandsEnabled = configuration.GetValue("Diagnostics:SqlCommands:Enabled", false);

        loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "RIIS.Academic")
            .Enrich.WithProperty("InstanceId", instanceId)
            .Enrich.WithProperty("Environment", environmentName)
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext} "
                + "({InstanceId}) {Message:lj}{NewLine}{Exception}");

        if (sqlCommandsEnabled)
        {
            loggerConfiguration
                .MinimumLevel.Override(
                    "Microsoft.EntityFrameworkCore.Database.Command",
                    LogEventLevel.Information)
                .MinimumLevel.Override(
                    "Microsoft.EntityFrameworkCore.Infrastructure",
                    LogEventLevel.Information);
        }

        if (configuration.GetValue("Diagnostics:File:Enabled", true))
        {
            loggerConfiguration.WriteTo.File(
                configuration["Diagnostics:File:Path"] ?? "Logs/riis-academic-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext} "
                    + "({InstanceId}) {Message:lj}{NewLine}{Exception}");
        }

        var seqEnabled = configuration.GetValue("Diagnostics:Seq:Enabled", false);
        var seqUrl = configuration["Diagnostics:Seq:Url"];
        if (seqEnabled && Uri.TryCreate(seqUrl, UriKind.Absolute, out var seqUri))
        {
            var seqApiKey = configuration["Diagnostics:Seq:ApiKey"];
            loggerConfiguration.WriteTo.Seq(
                seqUri.AbsoluteUri,
                apiKey: string.IsNullOrWhiteSpace(seqApiKey) ? null : seqApiKey);
        }
    }
}
