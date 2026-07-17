using System.IO.Compression;
using Bobeta.API.App.Extensions;
using Bobeta.API.App.Filters;
using Bobeta.API.App.HostedServices;
using Bobeta.API.App.Json;
using Bobeta.API.Hubs;
using Microsoft.AspNetCore.ResponseCompression;

static void WriteStartupDiagnostics(string stage, Exception? ex = null)
{
    try
    {
        var path = Path.Combine(AppContext.BaseDirectory, "startup-crash.log");
        var text =
            $"{DateTimeOffset.Now:O} stage={stage}{Environment.NewLine}" +
            $"ASPNETCORE_ENVIRONMENT={Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}{Environment.NewLine}" +
            $"ASPNETCORE_URLS={Environment.GetEnvironmentVariable("ASPNETCORE_URLS")}{Environment.NewLine}" +
            $"DOTNET_STARTUP_HOOKS={Environment.GetEnvironmentVariable("DOTNET_STARTUP_HOOKS")}{Environment.NewLine}" +
            (ex is null ? string.Empty : ex.ToString());
        File.WriteAllText(path, text);
    }
    catch
    {
        // Diagnostics must never block startup.
    }
}

WriteStartupDiagnostics("enter-main");

try
{
    // Build the web application and configure services.
    var builder = WebApplication.CreateBuilder(args);
    // Windows often reserves default Kestrel port 5000 (Hyper-V / excluded ranges). Without a launch
    // profile ASPNETCORE_URLS, binding fails with SocketException 10013 and the process exits 0xffffffff.
    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
        builder.WebHost.UseUrls("http://127.0.0.1:5163");
    var configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

    // Controllers with global validation filter (FluentValidation on request DTOs).
    builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>())
        .AddJsonOptions(options => ApiJsonSerializerOptions.Configure(options.JsonSerializerOptions));
    builder.Services.AddAuthorization();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddBobetaSwagger();

    // Bobeta: persistence, application, identity, infrastructure, JWT, SignalR.
    builder.Services.AddBobetaServices(builder.Configuration, builder.Environment);
    builder.Services.AddHostedService<DatabaseMigrationHostedService>();

    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
    });
    builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
        options.Level = CompressionLevel.Fastest);
    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
        options.Level = CompressionLevel.Fastest);

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
    WriteStartupDiagnostics($"built env={app.Environment.EnvironmentName}");

    // Must run early so OPTIONS preflight gets Access-Control-* headers before auth/endpoints (see browser CORS errors).
    app.UseCors();
    app.UseResponseCompression();
    app.UseBobetaSwagger();
    // In Development the mobile app often uses http://localhost:5163. UseHttpsRedirection would 307 to https on
    // another port; HttpClient follows but drops the Authorization header, so wallet calls return 401 after login.
    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<GameHub>("/hubs/game"); // SignalR game hub for real-time gameplay.
    app.MapHub<NotificationHub>("/hubs/notifications");
    app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

    WriteStartupDiagnostics("before-run");
    app.Run();
}
catch (Exception ex)
{
    WriteStartupDiagnostics("exception", ex);
    Console.Error.WriteLine(ex);
    throw;
}

public partial class Program;
