using System.Security.Claims;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Bobeta.Portal.Services;

public sealed class PortalSignInService(
    IPortalAuthService auth,
    IHttpContextAccessor httpContextAccessor)
{
  public async Task<bool> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
  {
    var httpContext = httpContextAccessor.HttpContext;
    if (httpContext is null)
      return false;

    var user = await auth.ValidateCredentialsAsync(email, password, cancellationToken);
    if (user is null)
      return false;

    var claims = new List<Claim>
    {
      new(ClaimTypes.NameIdentifier, user.Id.ToString()),
      new(ClaimTypes.Email, user.Email),
      new(ClaimTypes.Name, user.DisplayName),
      new(ClaimTypes.Role, user.Role.ToString())
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
          IsPersistent = true,
          ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

    return true;
  }

  public async Task SignOutAsync()
  {
    var httpContext = httpContextAccessor.HttpContext;
    if (httpContext is not null)
      await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
  }

  public static bool IsAdmin(ClaimsPrincipal user) =>
    user.IsInRole(nameof(PortalUserRole.Admin));
}
