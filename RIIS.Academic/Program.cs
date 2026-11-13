
using Radzen;
using RIIS.Academic.Infrastructure;
using RIIS.Academic.Infrastructure.Persistence;
using RIIS.Academic.Components;
using RIIS.Academic.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using RIIS.Academic.Authentication;
using QuestPDF.Infrastructure;
using RIIS.Academic.Logging;
using RIIS.Academic.Application.Abstractions.Services;
using RIIS.Academic.Application.Common.Loading;
using System.Text.Json.Serialization;

RiisSerilogConfiguration.InitializeBootstrap();

try
{
    var builder = WebApplication.CreateBuilder(args);
    QuestPDF.Settings.License = LicenseType.Community;
    RiisSerilogConfiguration.Configure(builder);

    var configuredApplicationUrl = builder.Configuration["Application:Url"];
    if (!Uri.TryCreate(configuredApplicationUrl, UriKind.Absolute, out var applicationUri)
        || (applicationUri.Scheme != Uri.UriSchemeHttp
            && applicationUri.Scheme != Uri.UriSchemeHttps))
    {
        throw new InvalidOperationException(
            "La configuration Application:Url doit contenir une URL HTTP ou HTTPS valide.");
    }

    builder.WebHost.UseUrls(applicationUri.AbsoluteUri);

    builder.Services.AddRadzenComponents();
    builder.Services.AddRazorComponents().AddInteractiveServerComponents();
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddAuthorizationCore();
    builder.Services.AddScoped<ApplicationAuthenticationStateProvider>();
    builder.Services.AddScoped<IDataLoadingState, DataLoadingState>();
    builder.Services.AddScoped<AuthenticationStateProvider>(services =>
        services.GetRequiredService<ApplicationAuthenticationStateProvider>());
    builder.Services.AddRiisAcademicInfrastructure(builder.Configuration);

    var app = builder.Build();
    RiisSerilogConfiguration.UseRequestLogging(app);

    app.Logger.LogInformation(
        "RIIS Academic demarre. InstanceId={InstanceId}, Environment={EnvironmentName}, "
        + "SeqEnabled={SeqEnabled}",
        Environment.MachineName,
        builder.Environment.EnvironmentName,
        builder.Configuration.GetValue("Diagnostics:Seq:Enabled", false));

    //await app.Services.InitializeRiisAcademicDatabaseAsync();

    app.Logger.LogInformation("Initialisation de la base terminée. Lancement du serveur web.");

    if (applicationUri.Scheme == Uri.UriSchemeHttps)
    {
        app.UseHttpsRedirection();
    }
    app.MapStaticAssets();
    app.UseAntiforgery();

    app.MapRiisAcademicExportEndpoints();
    app.MapRiisAcademicMobileApiEndpoints();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    var desktopIntegrationEnabled = builder.Configuration.GetValue(
        "Application:DesktopIntegrationEnabled",
        defaultValue: false);

    if (desktopIntegrationEnabled)
    {
        var createDesktopShortcut = builder.Configuration.GetValue(
            "Application:CreateDesktopShortcut",
            defaultValue: false);
        var openBrowserOnStartup = builder.Configuration.GetValue(
            "Application:OpenBrowserOnStartup",
            defaultValue: false);

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var availableUrl = app.Urls.FirstOrDefault() ?? applicationUri.AbsoluteUri;

            DesktopShortcutService.CreateAndOpen(
                availableUrl,
                createDesktopShortcut,
                openBrowserOnStartup);
        });
    }

    app.Run();
}
catch (Exception exception)
{
    RiisSerilogConfiguration.Fatal(exception, "RIIS Academic s'est arretee de maniere inattendue.");
}
finally
{
    RiisSerilogConfiguration.CloseAndFlush();
}
