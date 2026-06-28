using System.Text;

namespace Bobeta.Application.Common;

public static class CsvBuilder
{
  public static string Build(IEnumerable<string[]> rows)
  {
    var sb = new StringBuilder();
    foreach (var row in rows)
      sb.AppendLine(string.Join(",", row.Select(Escape)));
    return sb.ToString();
  }

  private static string Escape(string? value)
  {
    if (string.IsNullOrEmpty(value))
      return string.Empty;

    if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
      return $"\"{value.Replace("\"", "\"\"")}\"";

    return value;
  }
}
