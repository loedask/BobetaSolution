using Bobeta.API.App.Extensions;
using Bobeta.API.App.Filters;
using Bobeta.API.Hubs;

// Build the web application and configure services.
var builder = WebApplication.CreateBuilder(args);

// Controllers with global validation filter (FluentValidation on request DTOs).
builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddBobetaSwagger();

// Bobeta: persistence, application, identity, infrastructure, JWT, SignalR.
builder.Services.AddBobetaServices(builder.Configuration);

var app = builder.Build();

app.UseBobetaSwagger();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GameHub>("/hubs/game"); // SignalR game hub for real-time gameplay.

//if (app.Environment.IsDevelopment())
//    await app.ApplyMigrationsAsync();

app.Run();
