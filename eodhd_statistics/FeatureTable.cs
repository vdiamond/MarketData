using System.Globalization;
using Table;
using TableCsv;
using TableExtension;

namespace eodhd_statistics;

/// <summary>
/// In-memory feature CSV: a date spine plus numeric columns from <c>eodhd_transform</c>.
/// </summary>
public sealed class FeatureTable
{
    /// <summary>
    /// Creates a feature table from already-parsed dates and numeric series.
    /// </summary>
    /// <param name="dates">One date per row, in file order.</param>
    /// <param name="numericColumns">Numeric columns keyed by CSV header name.</param>
    public FeatureTable(IReadOnlyList<DateOnly> dates, IReadOnlyDictionary<string, IReadOnlyList<double>> numericColumns)
    {
        ArgumentNullException.ThrowIfNull(dates);
        ArgumentNullException.ThrowIfNull(numericColumns);
        if (dates.Count == 0)
        {
            throw new InvalidOperationException("Feature table contains no records.");
        }

        foreach (var (name, values) in numericColumns)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(values);
            if (values.Count != dates.Count)
            {
                throw new InvalidOperationException(
                    $"Column '{name}' has {values.Count} rows but Date has {dates.Count} rows.");
            }
        }

        Dates = dates;
        DateFrom = dates.Min();
        DateTo = dates.Max();
        _numericColumns = numericColumns;
    }

    /// <summary>CSV header name of the date column.</summary>
    public const string DateColumnName = "Date";

    /// <summary>Date format written by extract and transform CSVs.</summary>
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>Row dates in file order.</summary>
    public IReadOnlyList<DateOnly> Dates { get; }

    /// <summary>Number of records (rows) in the table.</summary>
    public int RecordCount => Dates.Count;

    /// <summary>Earliest date in the table.</summary>
    public DateOnly DateFrom { get; }

    /// <summary>Latest date in the table.</summary>
    public DateOnly DateTo { get; }

    /// <summary>
    /// Returns the numeric series for <paramref name="name"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The column is missing.</exception>
    public IReadOnlyList<double> GetNumericColumn(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!_numericColumns.TryGetValue(name, out var values))
        {
            throw new InvalidOperationException(
                $"Required numeric column '{name}' was not found in the feature CSV.");
        }

        return values;
    }

    /// <summary>
    /// Loads a transform feature CSV. <see cref="DateColumnName"/> is parsed as
    /// <c>yyyy-MM-dd</c>; every other column is loaded as <see cref="double"/>.
    /// </summary>
    public static FeatureTable Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"Feature CSV was not found. Run eodhd_transform first, or pass a path. Path: {filePath}",
                filePath);
        }

        var attr = new DefaultFieldAttributes();
        attr.SetDefault(TypeCode.Double);
        attr.Add(new Field(DateColumnName, TypeCode.String, true));

        IDynamicTable table = new DynamicTable();
        table.LoadCsv(filePath, CsvHeaderType.Header, attr: attr);

        if (!table.columns.ColumnExists(DateColumnName))
        {
            throw new InvalidOperationException($"Feature CSV is missing the required '{DateColumnName}' column.");
        }

        var dateStrings = table.AsList<string>(DateColumnName);
        if (dateStrings.Count == 0)
        {
            throw new InvalidOperationException($"Feature CSV '{filePath}' contains no records.");
        }

        var dates = new DateOnly[dateStrings.Count];
        for (var i = 0; i < dateStrings.Count; i++)
        {
            if (!DateOnly.TryParseExact(
                    dateStrings[i],
                    DateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date))
            {
                throw new FormatException(
                    $"Invalid Date value '{dateStrings[i]}' at row {i}. Expected {DateFormat}.");
            }

            dates[i] = date;
        }

        var numericColumns = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);
        foreach (var column in table.columns)
        {
            if (string.Equals(column.Name, DateColumnName, StringComparison.Ordinal))
            {
                continue;
            }

            if (column.TypeCode != TypeCode.Double)
            {
                throw new InvalidOperationException(
                    $"Column '{column.Name}' has type {column.TypeCode}; expected Double.");
            }

            var values = table.AsList<double>(column.Name);
            var copy = new double[values.Count];
            for (var r = 0; r < values.Count; r++)
            {
                copy[r] = values[r];
            }

            numericColumns[column.Name] = copy;
        }

        return new FeatureTable(dates, numericColumns);
    }

    #region private

    private readonly IReadOnlyDictionary<string, IReadOnlyList<double>> _numericColumns;

    #endregion
}
