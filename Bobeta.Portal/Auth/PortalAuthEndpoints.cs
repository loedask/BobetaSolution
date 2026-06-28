using Bobeta.Portal.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.Portal.Auth;

public static class PortalAuthEndpoints
{
  public static IEndpointRouteBuilder MapPortalAuthEndpoints(this IEndpointRouteBuilder endpoints)
  {
    endpoints.MapPost("/account/login", async (
        HttpContext httpContext,
        PortalSignInService signIn,
        IAntiforgery antiforgery,
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] string? returnUrl) =>
    {
      await antiforgery.ValidateRequestAsync(httpContext);

      if (!await signIn.SignInAsync(httpContext, email, password))
        return Results.Redirect(BuildLoginRedirect(returnUrl, "invalid"));

      return Results.Redirect(ResolveReturnUrl(returnUrl));
    }).AllowAnonymous().DisableAntiforgery();

    endpoints.MapGet("/account/logout", async (HttpContext httpContext) =>
    {
      await PortalSignInService.SignOutAsync(httpContext);
      return Results.Redirect("/login");
    }).RequireAuthorization();

    return endpoints;
  }

  private static string BuildLoginRedirect(string? returnUrl, string error)
  {
    var query = $"error={Uri.EscapeDataString(error)}";
    if (!string.IsNullOrWhiteSpace(returnUrl))
      query += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
    return $"/login?{query}";
  }

  private static string ResolveReturnUrl(string? returnUrl) =>
    !string.IsNullOrWhiteSpace(returnUrl)
        && returnUrl.StartsWith('/')
        && !returnUrl.StartsWith("//", StringComparison.Ordinal)
      ? returnUrl
      : "/";
}
