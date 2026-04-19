using Bobeta.API.App.Extensions;
using Bobeta.API.App.Filters;
using Bobeta.API.Hubs;
using Bobeta.Application.Common;

// Build the web application and configure services.
var builder = WebApplication.CreateBuilder(args);
var configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

// Controllers with global validation filter (FluentValidation on request DTOs).
builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddBobetaSwagger();

// Bobeta: persistence, application, identity, infrastructure, JWT, SignalR.
builder.Services.AddBobetaServices(builder.Configuration);

// Blazor WebAssembly (and other browser clients) call the API from a different origin/port than Kestrel — browsers require CORS.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrEmpty(origin)) return false;
            if (configuredCorsOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)) return true;
            if (origin.StartsWith("https://localhost:", StringComparison.OrdinalIgnoreCase)) return true;
            if (origin.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase)) return true;
            if (origin.StartsWith("http://127.0.0.1:", StringComparison.OrdinalIgnoreCase)) return true;
            // Azure App Service (global + regional hosts like *.southafricanorth-01.azurewebsites.net).
            return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && uri.Host.EndsWith(".azurewebsites.net", StringComparison.OrdinalIgnoreCase);
        });
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
    });
});

var app = builder.Build();

// Must run early so OPTIONS preflight gets Access-Control-* headers before auth/endpoints (see browser CORS errors).
app.UseCors();
app.UseBobetaSwagger();
// In Development the mobile app often uses http://localhost:5163. UseHttpsRedirection would 307 to https on
// another port; HttpClient follows but drops the Authorization header, so wallet calls return 401 after login.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GameHub>("/hubs/game"); // SignalR game hub for real-time gameplay.

if (DemoEnvironmentHelper.AllowsDemoAuthFeatures(app.Environment))
    await app.ApplyMigrationsAsync();

app.Run();
