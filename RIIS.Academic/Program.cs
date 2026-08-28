
using Radzen;
using RIIS.Academic.Infrastructure;
using RIIS.Academic.Infrastructure.Persistence;
using RIIS.Academic.Components;
using RIIS.Academic.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRadzenComponents();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddRiisAcademicInfrastructure(builder.Configuration);

var app = builder.Build();

//await app.Services.InitializeRiisAcademicDatabaseAsync();

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapRiisAcademicExportEndpoints();

app.Run();
