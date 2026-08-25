using System.Globalization;

namespace eodhd_statistics;

/// <summary>
/// A named statistic computed over a group of row indexes into a <see cref="FeatureTable"/>.
/// Add new factory methods here as the report set grows.
/// </summary>
public sealed class Statistic
{
    /// <summary>
    /// Creates a statistic with an explicit compute function.
    /// </summary>
    public Statistic(string name, Func<FeatureTable, IReadOnlyList<int>, string> compute)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(compute);
        Name = name;
        _compute = compute;
    }

    /// <summary>Display name used in report headings.</summary>
    public string Name { get; }

    /// <summary>
    /// Computes this statistic for the given row indexes.
    /// </summary>
    public string Compute(FeatureTable table, IReadOnlyList<int> rowIndexes)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(rowIndexes);
        return _compute(table, rowIndexes);
    }

    /// <summary>
    /// Percentage of records in the group where <paramref name="columnName"/> is strictly greater than
    /// <paramref name="threshold"/>. The group record count is the denominator (NaN does not match).
    /// </summary>
    public static Statistic PercentWhereGreaterThan(string columnName, double threshold)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        var thresholdText = FormatThreshold(threshold);
        var name = $"% of records where {columnName} > {thresholdText}";
        return new Statistic(name, (table, rowIndexes) =>
            ComputePercentWhereGreaterThan(table, rowIndexes, columnName, threshold, given: null));
    }

    /// <summary>
    /// Percentage of records where <paramref name="columnName"/> is strictly greater than
    /// <paramref name="threshold"/>, among records in the group where
    /// <paramref name="givenLeftColumnName"/> is strictly greater than <paramref name="givenRightColumnName"/>.
    /// Returns <c>n/a</c> when no records in the group satisfy the given condition.
    /// </summary>
    public static Statistic PercentWhereGreaterThan(
        string columnName,
        double threshold,
        string givenLeftColumnName,
        string givenRightColumnName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        ArgumentException.ThrowIfNullOrWhiteSpace(givenLeftColumnName);
        ArgumentException.ThrowIfNullOrWhiteSpace(givenRightColumnName);
        var thresholdText = FormatThreshold(threshold);
        var name =
            $"% of records where {columnName} > {thresholdText} for records where {givenLeftColumnName} > {givenRightColumnName}";
        return new Statistic(name, (table, rowIndexes) =>
        {
            var givenLeft = table.GetNumericColumn(givenLeftColumnName);
            var givenRight = table.GetNumericColumn(givenRightColumnName);
            return ComputePercentWhereGreaterThan(
                table,
                rowIndexes,
                columnName,
                threshold,
                rowIndex =>
                {
                    EnsureRowIndex(rowIndex, givenLeft.Count, givenLeftColumnName);
                    EnsureRowIndex(rowIndex, givenRight.Count, givenRightColumnName);
                    return givenLeft[rowIndex] > givenRight[rowIndex];
                });
        });
    }

    #region private

    private readonly Func<FeatureTable, IReadOnlyList<int>, string> _compute;

    private static string FormatThreshold(double threshold)
    {
        return threshold.ToString("0.0", CultureInfo.InvariantCulture);
    }

    private static string ComputePercentWhereGreaterThan(
        FeatureTable table,
        IReadOnlyList<int> rowIndexes,
        string columnName,
        double threshold,
        Func<int, bool>? given)
    {
        if (rowIndexes.Count == 0)
        {
            return "n/a";
        }

        var series = table.GetNumericColumn(columnName);
        var hits = 0;
        var considered = 0;
        for (var i = 0; i < rowIndexes.Count; i++)
        {
            var rowIndex = rowIndexes[i];
            EnsureRowIndex(rowIndex, series.Count, columnName);
            if (given is not null && !given(rowIndex))
            {
                continue;
            }

            considered++;
            if (series[rowIndex] > threshold)
            {
                hits++;
            }
        }

        if (considered == 0)
        {
            return "n/a";
        }

        var percent = 100.0 * hits / considered;
        return percent.ToString("0.00", CultureInfo.InvariantCulture) + "%";
    }

    private static void EnsureRowIndex(int rowIndex, int length, string columnName)
    {
        if (rowIndex < 0 || rowIndex >= length)
        {
            throw new InvalidOperationException(
                $"Row index {rowIndex} is outside column '{columnName}' (length {length}).");
        }
    }

    #endregion
}
