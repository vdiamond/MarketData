using System.Globalization;

namespace eodhd_statistics;

/// <summary>
/// A named subset of rows used to compute statistics (full set, year, month, weekday, or day of month).
/// </summary>
public sealed class RecordGroup
{
    /// <summary>
    /// Creates a group over the supplied row indexes.
    /// </summary>
    public RecordGroup(
        string sectionTitle,
        string keyName,
        string keyValue,
        IReadOnlyList<int> rowIndexes,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyValue);
        ArgumentNullException.ThrowIfNull(rowIndexes);

        SectionTitle = sectionTitle;
        KeyName = keyName;
        KeyValue = keyValue;
        RowIndexes = rowIndexes;
        DateFrom = dateFrom;
        DateTo = dateTo;
    }

    /// <summary>Section heading (for example, "Year").</summary>
    public string SectionTitle { get; }

    /// <summary>Label of the grouping key (for example, "Month").</summary>
    public string KeyName { get; }

    /// <summary>Value of the grouping key (for example, "January").</summary>
    public string KeyValue { get; }

    /// <summary>Row indexes into <see cref="FeatureTable.Dates"/> that belong to this group.</summary>
    public IReadOnlyList<int> RowIndexes { get; }

    /// <summary>Number of records in this group.</summary>
    public int RecordCount => RowIndexes.Count;

    /// <summary>Inclusive start date when this group represents a date range.</summary>
    public DateOnly? DateFrom { get; }

    /// <summary>Inclusive end date when this group represents a date range.</summary>
    public DateOnly? DateTo { get; }
}

/// <summary>
/// Builds the standard report groupings from a <see cref="FeatureTable"/>.
/// Empty buckets are omitted.
/// </summary>
public static class RecordGroups
{
    /// <summary>Section title for the entire file.</summary>
    public const string FullDataSetTitle = "Full Data Set";

    /// <summary>Section title for calendar-year groups.</summary>
    public const string YearTitle = "Year";

    /// <summary>Section title for calendar-month groups (all years combined).</summary>
    public const string MonthTitle = "Month";

    /// <summary>Section title for weekday groups (all years and months combined).</summary>
    public const string WeekdayTitle = "Weekday";

    /// <summary>Section title for day-of-month groups (all years combined).</summary>
    public const string DayOfMonthTitle = "Day of Month";

    /// <summary>
    /// One group covering every row, with date-from / date-to of the full file.
    /// </summary>
    public static RecordGroup FullDataSet(FeatureTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        return new RecordGroup(
            FullDataSetTitle,
            "Date range",
            $"{table.DateFrom.ToString(FeatureTable.DateFormat, CultureInfo.InvariantCulture)} to {table.DateTo.ToString(FeatureTable.DateFormat, CultureInfo.InvariantCulture)}",
            CreateIndexRange(table.RecordCount),
            table.DateFrom,
            table.DateTo);
    }

    /// <summary>One group per calendar year, ordered ascending.</summary>
    public static IReadOnlyList<RecordGroup> ByYear(FeatureTable table)
    {
        return GroupBy(
            table,
            YearTitle,
            "Year",
            i => table.Dates[i].Year,
            year => year.ToString(CultureInfo.InvariantCulture),
            Comparer<int>.Default);
    }

    /// <summary>One group per calendar month, combining all years, January through December.</summary>
    public static IReadOnlyList<RecordGroup> ByMonth(FeatureTable table)
    {
        return GroupBy(
            table,
            MonthTitle,
            "Month",
            i => table.Dates[i].Month,
            month => CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
            Comparer<int>.Default);
    }

    /// <summary>One group per weekday, combining all years and months, Monday through Sunday.</summary>
    public static IReadOnlyList<RecordGroup> ByWeekday(FeatureTable table)
    {
        return GroupBy(
            table,
            WeekdayTitle,
            "Weekday",
            i => table.Dates[i].DayOfWeek,
            day => day.ToString(),
            Comparer<DayOfWeek>.Create(static (left, right) => WeekdaySortOrder(left).CompareTo(WeekdaySortOrder(right))));
    }

    /// <summary>One group per day of month (1–31 as present), combining all years.</summary>
    public static IReadOnlyList<RecordGroup> ByDayOfMonth(FeatureTable table)
    {
        return GroupBy(
            table,
            DayOfMonthTitle,
            "Day",
            i => table.Dates[i].Day,
            day => day.ToString(CultureInfo.InvariantCulture),
            Comparer<int>.Default);
    }

    #region private

    private static IReadOnlyList<RecordGroup> GroupBy<TKey>(
        FeatureTable table,
        string sectionTitle,
        string keyName,
        Func<int, TKey> keySelector,
        Func<TKey, string> keyFormatter,
        IComparer<TKey> comparer)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(keyFormatter);
        ArgumentNullException.ThrowIfNull(comparer);

        var buckets = new Dictionary<TKey, List<int>>();
        for (var i = 0; i < table.RecordCount; i++)
        {
            var key = keySelector(i);
            if (!buckets.TryGetValue(key, out var list))
            {
                list = [];
                buckets[key] = list;
            }

            list.Add(i);
        }

        var orderedKeys = new List<TKey>(buckets.Keys);
        orderedKeys.Sort(comparer);

        var groups = new RecordGroup[orderedKeys.Count];
        for (var g = 0; g < orderedKeys.Count; g++)
        {
            var key = orderedKeys[g];
            groups[g] = new RecordGroup(sectionTitle, keyName, keyFormatter(key), buckets[key]);
        }

        return groups;
    }

    private static int WeekdaySortOrder(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => 0,
            DayOfWeek.Tuesday => 1,
            DayOfWeek.Wednesday => 2,
            DayOfWeek.Thursday => 3,
            DayOfWeek.Friday => 4,
            DayOfWeek.Saturday => 5,
            DayOfWeek.Sunday => 6,
            _ => 7
        };
    }

    private static IReadOnlyList<int> CreateIndexRange(int count)
    {
        var indexes = new int[count];
        for (var i = 0; i < count; i++)
        {
            indexes[i] = i;
        }

        return indexes;
    }

    #endregion
}
