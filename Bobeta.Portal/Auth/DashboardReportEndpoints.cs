using System.Security.Claims;
using System.Text;
using Bobeta.Application.DTOs.Portal;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.Portal.Auth;

public static class DashboardReportEndpoints
{
  public static IEndpointRouteBuilder MapDashboardReportEndpoints(this IEndpointRouteBuilder endpoints)
  {
    endpoints.MapGet("/reports/summary.csv", DownloadAsync(ExportKind.Summary)).RequireAuthorization();
    endpoints.MapGet("/reports/players.csv", DownloadAsync(ExportKind.Players)).RequireAuthorization();
    endpoints.MapGet("/reports/revenue.csv", DownloadAsync(ExportKind.Revenue)).RequireAuthorization();
    endpoints.MapGet("/reports/payments.csv", DownloadAsync(ExportKind.Payments)).RequireAuthorization();
    return endpoints;
  }

  private enum ExportKind { Summary, Players, Revenue, Payments }

  private static Func<HttpContext, IDashboardService, string?, string?, Task<IResult>> DownloadAsync(ExportKind kind) =>
    async (httpContext, dashboard, from, to) =>
    {
      var user = ResolvePortalUser(httpContext.User);
      if (user is null)
        return Results.Unauthorized();

      var query = BuildQuery(from, to);

      try
      {
        var csv = kind switch
        {
          ExportKind.Summary => await dashboard.ExportSummaryCsvAsync(query, user.Value.Role, user.Value.UserId),
          ExportKind.Players => await dashboard.ExportPlayersCsvAsync(query, user.Value.Role, user.Value.UserId),
          ExportKind.Revenue => await dashboard.ExportRevenueCsvAsync(query, user.Value.Role, user.Value.UserId),
          ExportKind.Payments => await dashboard.ExportPaymentsCsvAsync(query, user.Value.Role, user.Value.UserId),
          _ => string.Empty
        };

        var fileName = kind switch
        {
          ExportKind.Summary => "dashboard-summary.csv",
          ExportKind.Players => "players-by-country.csv",
          ExportKind.Revenue => "revenue-detail.csv",
          ExportKind.Payments => "payments-summary.csv",
          _ => "report.csv"
        };

        return Results.File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
      }
      catch (UnauthorizedAccessException)
      {
        return Results.Forbid();
      }
    };

  private static DashboardQuery BuildQuery(string? from, string? to) => new()
  {
    FromUtc = ParseFromDate(from),
    ToUtc = ParseToDate(to)
  };

  private static DateTime? ParseFromDate(string? value) =>
    DateTime.TryParse(value, out var date) ? DateTime.SpecifyKind(date.Date, DateTimeKind.Utc) : null;

  private static DateTime? ParseToDate(string? value) =>
    DateTime.TryParse(value, out var date)
      ? DateTime.SpecifyKind(date.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
      : null;

  private static (Guid UserId, PortalUserRole Role)? ResolvePortalUser(ClaimsPrincipal user)
  {
    var rawId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var rawRole = user.FindFirst(ClaimTypes.Role)?.Value;
    if (!Guid.TryParse(rawId, out var userId))
      return null;
    if (!Enum.TryParse<PortalUserRole>(rawRole, out var role))
      return null;
    return (userId, role);
  }
}
