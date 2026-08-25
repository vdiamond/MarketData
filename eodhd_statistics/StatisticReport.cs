using System.Globalization;
using System.Text;

namespace eodhd_statistics;

/// <summary>
/// Writes grouped statistic results for a feature table to a text writer.
/// </summary>
public sealed class StatisticReport
{
    /// <summary>
    /// Creates a report for the given table and statistic set.
    /// </summary>
    public StatisticReport(FeatureTable table, IReadOnlyList<Statistic> statistics)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(statistics);
        if (statistics.Count == 0)
        {
            throw new ArgumentException("At least one statistic is required.", nameof(statistics));
        }

        _table = table;
        _statistics = statistics;
    }

    /// <summary>
    /// Writes the full-data-set block and the year / month / weekday / day-of-month tables.
    /// </summary>
    public void Write(TextWriter writer, string? sourcePath = null)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (!string.IsNullOrWhiteSpace(sourcePath))
        {
            writer.WriteLine($"Source: {sourcePath}");
            writer.WriteLine();
        }

        WriteFullDataSet(writer);
        WriteGroupedSection(writer, RecordGroups.ByYear(_table));
        WriteGroupedSection(writer, RecordGroups.ByMonth(_table));
        WriteGroupedSection(writer, RecordGroups.ByWeekday(_table));
        WriteGroupedSection(writer, RecordGroups.ByDayOfMonth(_table));
    }

    #region private

    private readonly FeatureTable _table;
    private readonly IReadOnlyList<Statistic> _statistics;

    private void WriteFullDataSet(TextWriter writer)
    {
        var group = RecordGroups.FullDataSet(_table);
        WriteSectionHeading(writer, group.SectionTitle);
        writer.WriteLine($"Records:   {FormatCount(group.RecordCount)}");
        writer.WriteLine($"Date From: {group.DateFrom!.Value.ToString(FeatureTable.DateFormat, CultureInfo.InvariantCulture)}");
        writer.WriteLine($"Date To:   {group.DateTo!.Value.ToString(FeatureTable.DateFormat, CultureInfo.InvariantCulture)}");
        writer.WriteLine();
        foreach (var statistic in _statistics)
        {
            writer.WriteLine($"{statistic.Name}: {statistic.Compute(_table, group.RowIndexes)}");
        }

        writer.WriteLine();
    }

    private void WriteGroupedSection(TextWriter writer, IReadOnlyList<RecordGroup> groups)
    {
        if (groups.Count == 0)
        {
            return;
        }

        WriteSectionHeading(writer, groups[0].SectionTitle);

        var keyHeader = groups[0].KeyName;
        var headers = new List<string> { keyHeader, "Records" };
        foreach (var statistic in _statistics)
        {
            headers.Add(statistic.Name);
        }

        var rows = new string[groups.Count][];
        for (var r = 0; r < groups.Count; r++)
        {
            var group = groups[r];
            var cells = new string[headers.Count];
            cells[0] = group.KeyValue;
            cells[1] = FormatCount(group.RecordCount);
            for (var s = 0; s < _statistics.Count; s++)
            {
                cells[2 + s] = _statistics[s].Compute(_table, group.RowIndexes);
            }

            rows[r] = cells;
        }

        var widths = new int[headers.Count];
        for (var c = 0; c < headers.Count; c++)
        {
            var width = headers[c].Length;
            for (var r = 0; r < rows.Length; r++)
            {
                width = Math.Max(width, rows[r][c].Length);
            }

            widths[c] = width;
        }

        writer.WriteLine(FormatTableRow(headers, widths, rightAlign: false));
        writer.WriteLine(FormatSeparator(widths));
        for (var r = 0; r < rows.Length; r++)
        {
            writer.WriteLine(FormatTableRow(rows[r], widths, rightAlign: true));
        }

        writer.WriteLine();
    }

    private static void WriteSectionHeading(TextWriter writer, string title)
    {
        writer.WriteLine(new string('=', 78));
        writer.WriteLine(title);
        writer.WriteLine(new string('=', 78));
    }

    private static string FormatCount(int count)
    {
        return count.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string FormatTableRow(IReadOnlyList<string> cells, IReadOnlyList<int> widths, bool rightAlign)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < cells.Count; i++)
        {
            if (i > 0)
            {
                builder.Append("  ");
            }

            var alignRight = rightAlign && i > 0;
            var cell = cells[i];
            builder.Append(alignRight ? cell.PadLeft(widths[i]) : cell.PadRight(widths[i]));
        }

        return builder.ToString();
    }

    private static string FormatSeparator(IReadOnlyList<int> widths)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < widths.Count; i++)
        {
            if (i > 0)
            {
                builder.Append("  ");
            }

            builder.Append(new string('-', widths[i]));
        }

        return builder.ToString();
    }

    #endregion
}
