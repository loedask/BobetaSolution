using Bobeta.API.Extensions;
using Bobeta.API.Filters;
using Bobeta.API.Hubs;
using Microsoft.OpenApi.Models;

// Build the web application and configure services.
var builder = WebApplication.CreateBuilder(args);

// Controllers with global validation filter (FluentValidation on request DTOs).
builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
builder.Services.AddEndpointsApiExplorer();
// Swagger/OpenAPI with Bearer security definition for JWT.
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bobeta API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Bearer token",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

// Bobeta: persistence, application, identity, infrastructure, JWT, SignalR.
builder.Services.AddBobetaServices(builder.Configuration);

var app = builder.Build();

// Swagger UI in development.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GameHub>("/hubs/game"); // SignalR game hub for real-time gameplay.

app.Run();
