using Bobeta.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bobeta.API.Tests.Infrastructure;

public sealed class BobetaApiFactory : WebApplicationFactory<Program>
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Testing");
    builder.ConfigureServices(services =>
    {
      services.RemoveAll<IGameSessionService>();
      services.AddSingleton<IGameSessionService, FakeGameSessionService>();
      services.RemoveAll<INotificationService>();
      services.AddSingleton<INotificationService, FakeNotificationService>();
    });
  }

  public FakeNotificationService Notifications =>
    Services.GetRequiredService<INotificationService>() as FakeNotificationService
    ?? throw new InvalidOperationException("FakeNotificationService is not registered.");

  internal FakeGameSessionService GameSessions =>
    Services.GetRequiredService<IGameSessionService>() as FakeGameSessionService
    ?? throw new InvalidOperationException("FakeGameSessionService is not registered.");
}
