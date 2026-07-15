using ClosedXML.Excel;

namespace Bobeta.Application.Common;

public static class ExcelReportWriter
{
  private static readonly XLColor HeaderFill = XLColor.FromHtml("#0F6CBD");
  private static readonly XLColor HeaderFont = XLColor.White;
  private static readonly XLColor TitleFill = XLColor.FromHtml("#E8F0FA");

  public static byte[] Build(string sheetName, IReadOnlyList<string> headers, IEnumerable<object?[]> rows)
  {
    using var workbook = new XLWorkbook();
    var worksheet = workbook.Worksheets.Add(sheetName);

    for (var column = 0; column < headers.Count; column++)
      worksheet.Cell(1, column + 1).Value = headers[column];

    var rowIndex = 2;
    foreach (var row in rows)
    {
      for (var column = 0; column < headers.Count; column++)
      {
        var value = column < row.Length ? row[column] : null;
        SetCellValue(worksheet.Cell(rowIndex, column + 1), value);
      }

      rowIndex++;
    }

    StyleHeaderRow(worksheet, headers.Count);
    ApplyTableFormatting(worksheet, rowIndex - 1, headers.Count);
    worksheet.SheetView.FreezeRows(1);

    return Save(workbook);
  }

  public static byte[] BuildSummaryWorkbook(
      DateTime? fromUtc,
      DateTime? toUtc,
      IReadOnlyList<(string Category, string Metric, object? Value)> metrics,
      IReadOnlyList<(string CountryCode, string CountryName, int Count)>? playersByCountry = null,
      IReadOnlyList<(string Source, decimal Gross, decimal PartnerAmount, decimal InfluencerAmount, int TransactionCount)>? revenueBySource = null,
      IReadOnlyList<(string Partner, decimal PartnerAmount, decimal InfluencerAmount, decimal Gross, int TransactionCount)>? revenueByPartner = null,
      IReadOnlyList<(string Influencer, decimal InfluencerAmount, decimal Gross, int TransactionCount)>? revenueByInfluencer = null)
  {
    using var workbook = new XLWorkbook();

    WriteSummarySheet(workbook, fromUtc, toUtc, metrics);

    if (playersByCountry is { Count: > 0 })
      WriteDataSheet(workbook, "Players by country",
        ["Country code", "Country", "Players"],
        playersByCountry.Select(r => new object?[] { r.CountryCode, r.CountryName, r.Count }));

    if (revenueBySource is { Count: > 0 })
      WriteDataSheet(workbook, "Revenue by source",
        ["Source", "Gross revenue", "Partner share", "Influencer share", "Transactions"],
        revenueBySource.Select(r => new object?[] { r.Source, r.Gross, r.PartnerAmount, r.InfluencerAmount, r.TransactionCount }));

    if (revenueByPartner is { Count: > 0 })
      WriteDataSheet(workbook, "Revenue by partner",
        ["Partner", "Partner share", "Influencer share", "Gross revenue", "Transactions"],
        revenueByPartner.Select(r => new object?[] { r.Partner, r.PartnerAmount, r.InfluencerAmount, r.Gross, r.TransactionCount }));

    if (revenueByInfluencer is { Count: > 0 })
      WriteDataSheet(workbook, "Revenue by influencer",
        ["Influencer", "Influencer share", "Gross revenue", "Transactions"],
        revenueByInfluencer.Select(r => new object?[] { r.Influencer, r.InfluencerAmount, r.Gross, r.TransactionCount }));

    return Save(workbook);
  }

  private static void WriteSummarySheet(
      XLWorkbook workbook,
      DateTime? fromUtc,
      DateTime? toUtc,
      IReadOnlyList<(string Category, string Metric, object? Value)> metrics)
  {
    var worksheet = workbook.Worksheets.Add("Summary");

    worksheet.Cell(1, 1).Value = "Bobeta Portal — Dashboard summary";
    worksheet.Range(1, 1, 1, 3).Merge().Style
      .Font.SetBold()
      .Fill.SetBackgroundColor(TitleFill)
      .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

    worksheet.Cell(2, 1).Value = "Period from (UTC)";
    worksheet.Cell(2, 2).Value = fromUtc?.ToString("yyyy-MM-dd") ?? "All time";
    worksheet.Cell(3, 1).Value = "Period to (UTC)";
    worksheet.Cell(3, 2).Value = toUtc?.ToString("yyyy-MM-dd") ?? "All time";
    worksheet.Cell(4, 1).Value = "Generated (UTC)";
    worksheet.Cell(4, 2).Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");

    const int headerRow = 6;
    worksheet.Cell(headerRow, 1).Value = "Category";
    worksheet.Cell(headerRow, 2).Value = "Metric";
    worksheet.Cell(headerRow, 3).Value = "Value";

    var dataRow = headerRow + 1;
    foreach (var (category, metric, value) in metrics)
    {
      worksheet.Cell(dataRow, 1).Value = category;
      worksheet.Cell(dataRow, 2).Value = metric;
      SetCellValue(worksheet.Cell(dataRow, 3), value);
      dataRow++;
    }

    StyleHeaderRow(worksheet, 3, headerRow);
    ApplyTableFormatting(worksheet, dataRow - 1, 3, headerRow);
    worksheet.SheetView.FreezeRows(headerRow);
    worksheet.Column(1).Width = 18;
    worksheet.Column(2).Width = 28;
    worksheet.Column(3).Width = 18;
  }

  private static void WriteDataSheet(
      XLWorkbook workbook,
      string sheetName,
      IReadOnlyList<string> headers,
      IEnumerable<object?[]> rows)
  {
    var worksheet = workbook.Worksheets.Add(sheetName);

    for (var column = 0; column < headers.Count; column++)
      worksheet.Cell(1, column + 1).Value = headers[column];

    var rowIndex = 2;
    foreach (var row in rows)
    {
      for (var column = 0; column < headers.Count; column++)
      {
        var value = column < row.Length ? row[column] : null;
        SetCellValue(worksheet.Cell(rowIndex, column + 1), value);
      }

      rowIndex++;
    }

    StyleHeaderRow(worksheet, headers.Count);
    ApplyTableFormatting(worksheet, rowIndex - 1, headers.Count);
    worksheet.SheetView.FreezeRows(1);
  }

  private static void StyleHeaderRow(IXLWorksheet worksheet, int columnCount, int headerRow = 1)
  {
    var headerRange = worksheet.Range(headerRow, 1, headerRow, columnCount);
    headerRange.Style.Font.SetBold().Font.SetFontColor(HeaderFont);
    headerRange.Style.Fill.SetBackgroundColor(HeaderFill);
    headerRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
  }

  private static void ApplyTableFormatting(IXLWorksheet worksheet, int lastRow, int columnCount, int headerRow = 1)
  {
    if (lastRow < headerRow)
      return;

    var tableRange = worksheet.Range(headerRow, 1, lastRow, columnCount);
    tableRange.SetAutoFilter();
    worksheet.Columns(1, columnCount).AdjustToContents();
  }

  private static void SetCellValue(IXLCell cell, object? value)
  {
    switch (value)
    {
      case null:
        cell.Value = Blank.Value;
        break;
      case int intValue:
        cell.Value = intValue;
        break;
      case long longValue:
        cell.Value = longValue;
        break;
      case decimal decimalValue:
        cell.Value = decimalValue;
        cell.Style.NumberFormat.Format = "#,##0.00";
        break;
      case double doubleValue:
        cell.Value = doubleValue;
        cell.Style.NumberFormat.Format = "#,##0.00";
        break;
      case DateTime dateTime:
        cell.Value = dateTime;
        cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
        break;
      default:
        cell.Value = value.ToString();
        break;
    }
  }

  private static byte[] Save(XLWorkbook workbook)
  {
    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    return stream.ToArray();
  }
}
