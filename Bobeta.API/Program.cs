using Bobeta.API.App.Extensions;
using Bobeta.API.App.Filters;
using Bobeta.API.Hubs;
using Bobeta.Application.Common;

// Build the web application and configure services.
var builder = WebApplication.CreateBuilder(args);

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
        policy.SetIsOriginAllowed(static origin =>
            !string.IsNullOrEmpty(origin)
            && (origin.StartsWith("https://localhost:", StringComparison.OrdinalIgnoreCase)
                || origin.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase)
                || origin.StartsWith("http://127.0.0.1:", StringComparison.OrdinalIgnoreCase)));
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseBobetaSwagger();
app.UseCors();
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
