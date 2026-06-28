using System.Security.Claims;
using Bobeta.Application.DTOs.Portal;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.Portal.Auth;

public static class DashboardReportEndpoints
{
  private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

  public static IEndpointRouteBuilder MapDashboardReportEndpoints(this IEndpointRouteBuilder endpoints)
  {
    endpoints.MapGet("/reports/summary.xlsx", DownloadAsync(ExportKind.Summary)).RequireAuthorization();
    endpoints.MapGet("/reports/players.xlsx", DownloadAsync(ExportKind.Players)).RequireAuthorization();
    endpoints.MapGet("/reports/revenue.xlsx", DownloadAsync(ExportKind.Revenue)).RequireAuthorization();
    endpoints.MapGet("/reports/payments.xlsx", DownloadAsync(ExportKind.Payments)).RequireAuthorization();
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
        var content = kind switch
        {
          ExportKind.Summary => await dashboard.ExportSummaryExcelAsync(query, user.Value.Role, user.Value.UserId),
          ExportKind.Players => await dashboard.ExportPlayersExcelAsync(query, user.Value.Role, user.Value.UserId),
          ExportKind.Revenue => await dashboard.ExportRevenueExcelAsync(query, user.Value.Role, user.Value.UserId),
          ExportKind.Payments => await dashboard.ExportPaymentsExcelAsync(query, user.Value.Role, user.Value.UserId),
          _ => Array.Empty<byte>()
        };

        var fileName = kind switch
        {
          ExportKind.Summary => "dashboard-summary.xlsx",
          ExportKind.Players => "players-by-country.xlsx",
          ExportKind.Revenue => "revenue-detail.xlsx",
          ExportKind.Payments => "payments-summary.xlsx",
          _ => "report.xlsx"
        };

        return Results.File(content, ExcelContentType, fileName);
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
