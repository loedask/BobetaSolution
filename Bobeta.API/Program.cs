using System.Diagnostics;
using System.IO.Compression;
using Bobeta.API.App.Extensions;
using Bobeta.API.App.Filters;
using Bobeta.API.App.HostedServices;
using Bobeta.API.App.Json;
using Bobeta.API.Hubs;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Windows often reserves default Kestrel port 5000. Avoid that when no launch URLs are set.
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
    builder.WebHost.UseUrls("http://127.0.0.1:5163");

// VS Debug Console can cancel ConsoleLifetime immediately after listen (process exits -1).
if (Debugger.IsAttached)
{
    foreach (var descriptor in builder.Services
                 .Where(d => d.ServiceType == typeof(IHostLifetime)
                     && d.ImplementationType?.Name.Contains("Console", StringComparison.Ordinal) == true)
                 .ToList())
        builder.Services.Remove(descriptor);

    builder.Services.TryAddSingleton<IHostLifetime, DebuggerKeepAliveLifetime>();
}

var configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options => ApiJsonSerializerOptions.Configure(options.JsonSerializerOptions));
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddBobetaSwagger();

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
            return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && uri.Host.EndsWith(".azurewebsites.net", StringComparison.OrdinalIgnoreCase);
        });
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.UseResponseCompression();
app.UseBobetaSwagger();
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GameHub>("/hubs/game");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

app.Run();

internal sealed class DebuggerKeepAliveLifetime : IHostLifetime
{
    private readonly CancellationTokenSource _started = new();
    private readonly CancellationTokenSource _stopping = new();
    private readonly CancellationTokenSource _stopped = new();

    public CancellationToken ApplicationStarted => _started.Token;
    public CancellationToken ApplicationStopped => _stopped.Token;
    public CancellationToken ApplicationStopping => _stopping.Token;

    public void StopApplication()
    {
        _stopping.Cancel();
        _stopped.Cancel();
    }

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        _started.Cancel();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        StopApplication();
        return Task.CompletedTask;
    }
}

public partial class Program;
