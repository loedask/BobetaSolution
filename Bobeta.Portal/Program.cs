using System.Security.Claims;
using Bobeta.Application.Extensions;
using Bobeta.Domain.Enums;
using Bobeta.Persistence.Context;
using Bobeta.Persistence.Extensions;
using Bobeta.Persistence.Seeding;
using Bobeta.Portal.Auth;
using Bobeta.Portal.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString(PersistenceServiceCollectionExtensions.AzurePostgresConnectionStringName)
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=Bobeta;Username=postgres;Password=postgres";

builder.Services.AddBobetaPersistence(connectionString);
builder.Services.AddBobetaPortalServices(builder.Configuration);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        if (!builder.Environment.IsDevelopment())
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PortalPlatformOwner", policy => policy.RequireRole(nameof(PortalUserRole.PlatformOwner)));
});

builder.Services.AddHttpsRedirection(options => options.HttpsPort = 443);

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<PortalSignInService>();
builder.Services.AddScoped<PortalUserContext>();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<Bobeta.Portal.Components.App>()
    .AddInteractiveServerRenderMode();

app.MapPortalAuthEndpoints();
app.MapDashboardReportEndpoints();

await ApplyPortalMigrationsAsync(app);

app.Run();

static async Task ApplyPortalMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("PortalStartup");
    var db = services.GetRequiredService<BobetaDbContext>();

    logger.LogInformation("Applying database migrations for Bobeta.Portal");
    await db.Database.MigrateAsync();

    var settings = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Bobeta.Application.Configuration.PortalSettings>>();
    var passwordHasher = services.GetRequiredService<Bobeta.Application.Services.PortalPasswordHasher>();
    await PortalPlatformOwnerSeeder.SeedAsync(db, settings, passwordHasher, logger);
}
