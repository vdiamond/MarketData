using System.Globalization;
using System.Text;
using LiteDB;
using Table;
using TableCsv;
using TableExtension;

namespace eodhd_download;

/// <summary>
/// Left-joins LiteDB historical prices onto an existing extract CSV without rewriting the full book.
/// Preserves all existing cells (including evt_*).
/// </summary>
public static class ExtractJoin
{
    public static readonly string[] OhlcvFields = ["Open", "Low", "High", "Close", "Volume"];

    public static int JoinHistoricalPricesOntoExtract(
        ILiteCollection<MarketApi.HistoricalStockPrice> coll,
        string extractPath,
        IReadOnlyList<string> exchangeSymbolIds,
        DateTime fromDate,
        IReadOnlyList<string>? afterColumnNames = null,
        bool skipEmpty = false)
    {
        ArgumentNullException.ThrowIfNull(coll);
        ArgumentException.ThrowIfNullOrWhiteSpace(extractPath);
        ArgumentNullException.ThrowIfNull(exchangeSymbolIds);
        if (exchangeSymbolIds.Count == 0)
        {
            throw new ArgumentException("Must supply at least one symbol to join.", nameof(exchangeSymbolIds));
        }

        List<List<string>> grid;
        using (var reader = new StreamReader(extractPath))
        {
            grid = CsvReader.ReadAll(reader)
                ?? throw new InvalidOperationException($"Failed to read extract {extractPath}");
        }
        if (grid.Count < 2)
        {
            throw new InvalidOperationException("Extract has no data rows.");
        }

        var before = grid.Count - 1;
        var header = grid[0];
        int dateIdx = IndexOf(header, "Date");

        var dateTable = new DynamicTable();
        dateTable.columns.NewColumn("Date", TypeCode.String);
        for (int r = 1; r < grid.Count; r++)
        {
            var row = dateTable.rows.NewRow();
            row.SetField("Date", grid[r][dateIdx]);
        }
        var dict = dateTable.BuildKey<string>((IColumn<string>)dateTable.columns["Date"]);

        var added = new List<string>();
        foreach (var exchangeSymbolId in exchangeSymbolIds)
        {
            var tt = ExtractHistoricalStockPrices(coll, exchangeSymbolId, fromDate);
            if (tt.rows.Count == 0)
            {
                if (!skipEmpty)
                {
                    throw new InvalidOperationException($"No historical_day_prices for {exchangeSymbolId}.");
                }
                Console.WriteLine($"No historical_day_prices for {exchangeSymbolId}; skipping join.");
                continue;
            }
            var dateCol = (IColumn<string>)tt.columns["Date"];
            Console.WriteLine($"{exchangeSymbolId}: bars={tt.rows.Count} first={dateCol[0]} last={dateCol[tt.rows.Count - 1]}");
            var prefix = ExtractUtil.ColumnPrefixFromExchangeSymbolId(exchangeSymbolId);
            dateTable.LeftJoin(
                dict,
                (IColumn<string>)tt.columns["Date"],
                prefix,
                (IColumn<double>)tt.columns["Open"],
                (IColumn<double>)tt.columns["Low"],
                (IColumn<double>)tt.columns["High"],
                (IColumn<double>)tt.columns["Close"],
                (IColumn<double>)tt.columns["Volume"]);
            foreach (var field in OhlcvFields)
            {
                added.Add($"{prefix}.{field}");
            }
        }

        if (added.Count == 0)
        {
            Console.WriteLine("No symbols had historical_day_prices; extract unchanged.");
            return before;
        }

        foreach (var col in dateTable.columns)
        {
            if (col.TypeCode == TypeCode.Double)
            {
                ExtractUtil.FillIn((IList<double>)col);
            }
        }

        StripColumns(grid, added);
        header = grid[0];
        var after = afterColumnNames ?? (IReadOnlyList<string>)["vix9d.Volume", "vix3m.Volume", "vvix.Volume", "vix.Volume"];
        int insertAt = InsertIndex(header, after, EventCalendar.FlagColumns);
        InsertDoubleColumns(grid, added, dateTable, insertAt);

        if (grid.Count - 1 != before)
        {
            throw new InvalidOperationException($"Extract row count changed: before={before} after={grid.Count - 1}");
        }

        using (var writer = new StreamWriter(extractPath, false, new UTF8Encoding(false)))
        {
            CsvWriter.WriteAll(grid, writer);
        }
        return before;
    }

    public static void AddOrReplaceDoubleColumns(
        List<List<string>> grid,
        IReadOnlyList<string> columnNames,
        IReadOnlyList<double[]> values,
        int insertAt)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(columnNames);
        ArgumentNullException.ThrowIfNull(values);
        if (grid.Count == 0)
        {
            throw new InvalidOperationException("Extract is empty.");
        }
        if (columnNames.Count != values.Count)
        {
            throw new ArgumentException("Column names and values must be the same length.");
        }
        var dataRows = grid.Count - 1;
        foreach (var col in values)
        {
            if (col.Length != dataRows)
            {
                throw new ArgumentException($"Column length {col.Length} does not match extract data rows {dataRows}.");
            }
        }

        StripColumns(grid, columnNames);
        if (insertAt < 0 || insertAt > grid[0].Count)
        {
            insertAt = grid[0].Count;
        }
        for (int c = 0; c < columnNames.Count; c++)
        {
            grid[0].Insert(insertAt + c, columnNames[c]);
            for (int r = 1; r < grid.Count; r++)
            {
                grid[r].Insert(insertAt + c, FormatDouble(values[c][r - 1]));
            }
        }
    }


    public static void AddOrReplaceStringColumns(
        List<List<string>> grid,
        IReadOnlyList<string> columnNames,
        IReadOnlyList<string[]> values,
        int insertAt)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(columnNames);
        ArgumentNullException.ThrowIfNull(values);
        if (grid.Count == 0)
        {
            throw new InvalidOperationException("Extract is empty.");
        }
        if (columnNames.Count != values.Count)
        {
            throw new ArgumentException("Column names and values must be the same length.");
        }
        var dataRows = grid.Count - 1;
        foreach (var col in values)
        {
            if (col.Length != dataRows)
            {
                throw new ArgumentException($"Column length {col.Length} does not match extract data rows {dataRows}.");
            }
        }

        StripColumns(grid, columnNames);
        if (insertAt < 0 || insertAt > grid[0].Count)
        {
            insertAt = grid[0].Count;
        }
        for (int c = 0; c < columnNames.Count; c++)
        {
            grid[0].Insert(insertAt + c, columnNames[c]);
            for (int r = 1; r < grid.Count; r++)
            {
                grid[r].Insert(insertAt + c, values[c][r - 1] ?? "");
            }
        }
    }

    public static void StripColumns(List<List<string>> grid, IReadOnlyCollection<string> names)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(names);
        if (grid.Count == 0)
        {
            return;
        }
        var drop = new HashSet<int>();
        var header = grid[0];
        var nameSet = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Count; i++)
        {
            if (nameSet.Contains(header[i]))
            {
                drop.Add(i);
            }
        }
        if (drop.Count == 0)
        {
            return;
        }
        for (int r = 0; r < grid.Count; r++)
        {
            var row = grid[r];
            var kept = new List<string>(row.Count - drop.Count);
            for (int i = 0; i < row.Count; i++)
            {
                if (!drop.Contains(i))
                {
                    kept.Add(row[i]);
                }
            }
            grid[r] = kept;
        }
    }

    public static int InsertIndex(List<string> header, IReadOnlyList<string> afterNames, IReadOnlyList<string> beforeNames)
    {
        ArgumentNullException.ThrowIfNull(header);
        if (afterNames != null)
        {
            for (int i = afterNames.Count - 1; i >= 0; i--)
            {
                var idx = header.FindIndex(h => string.Equals(h, afterNames[i], StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    return idx + 1;
                }
            }
        }
        if (beforeNames != null)
        {
            for (int i = 0; i < beforeNames.Count; i++)
            {
                var idx = header.FindIndex(h => string.Equals(h, beforeNames[i], StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    return idx;
                }
            }
        }
        return header.Count;
    }

    public static int IndexOf(List<string> header, string name)
    {
        var i = header.FindIndex(h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase));
        if (i < 0)
        {
            throw new InvalidOperationException($"Extract is missing column {name}");
        }
        return i;
    }

    public static string FormatDouble(double value)
    {
        if (double.IsNaN(value))
        {
            return "NaN";
        }
        return value.ToString(CultureInfo.InvariantCulture);
    }

    #region private

    static IDynamicTable ExtractHistoricalStockPrices(
        ILiteCollection<MarketApi.HistoricalStockPrice> coll,
        string exchangeSymbolId,
        DateTime fromDate)
    {
        var fromDateA = fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var results = coll.Find(Query.And(Query.EQ("ExchangeSymbolId", exchangeSymbolId), Query.GTE("Date", fromDateA)))
            .OrderBy(x => x.Date)
            .ToList();
        return results.ToTable();
    }

    static void InsertDoubleColumns(List<List<string>> grid, List<string> names, IDynamicTable table, int insertAt)
    {
        if (insertAt < 0 || insertAt > grid[0].Count)
        {
            insertAt = grid[0].Count;
        }
        for (int c = 0; c < names.Count; c++)
        {
            var col = (IColumn<double>)table.columns[names[c]];
            grid[0].Insert(insertAt + c, names[c]);
            for (int r = 1; r < grid.Count; r++)
            {
                grid[r].Insert(insertAt + c, FormatDouble(col[r - 1]));
            }
        }
    }

    #endregion
}
