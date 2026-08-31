using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using TableCsv;
using EodHdExtract;

namespace eodhd_download;

/// <summary>
/// EODHD USA macro-indicator dump and conservative known-as-of join onto extract cash sessions.
/// Annual Date is YYYY-12-31; known on the first cash session on or after the 15th of the following month.
/// </summary>
public static class MacroIndicators
{
    public const string CpiYoyColumn = "macro_cpi_yoy";
    public const string UnempColumn = "macro_unemp";
    public const string RealRateColumn = "macro_real_rate";
    public const string Country = "USA";

    public static readonly string[] RequestedKeys =
    [
        "inflation_consumer_prices_annual",
        "consumer_price_index",
        "unemployment_total_percent",
        "real_interest_rate"
    ];

    public static readonly Dictionary<string, string> ExtractColumnByKey = new(StringComparer.OrdinalIgnoreCase)
    {
        ["inflation_consumer_prices_annual"] = CpiYoyColumn,
        ["unemployment_total_percent"] = UnempColumn,
        ["real_interest_rate"] = RealRateColumn
    };

    public static DateOnly PeriodKnownFrom(DateOnly periodEnd)
    {
        var followingMonth = new DateOnly(periodEnd.Year, periodEnd.Month, 1).AddMonths(1);
        return new DateOnly(followingMonth.Year, followingMonth.Month, 15);
    }

    public static DateOnly? KnownAsOfCashSession(DateOnly periodEnd, IReadOnlyList<DateOnly> cashSessions)
    {
        ArgumentNullException.ThrowIfNull(cashSessions);
        var knownFrom = PeriodKnownFrom(periodEnd);
        for (int i = 0; i < cashSessions.Count; i++)
        {
            if (cashSessions[i] >= knownFrom)
            {
                return cashSessions[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Forward-fills annual prints onto extract dates. Values are assigned on cash sessions
    /// at/after known-as-of, then FillIn copies onto non-cash extract rows. Empty until first known print.
    /// </summary>
    public static double[] JoinAnnualPrints(
        IReadOnlyList<DateOnly> extractDates,
        IReadOnlyList<DateOnly> cashSessions,
        IReadOnlyList<(DateOnly PeriodEnd, double Value)> prints)
    {
        ArgumentNullException.ThrowIfNull(extractDates);
        ArgumentNullException.ThrowIfNull(cashSessions);
        ArgumentNullException.ThrowIfNull(prints);

        var cashSet = new HashSet<DateOnly>(cashSessions);
        var values = new double[extractDates.Count];
        Array.Fill(values, double.NaN);

        var steps = new List<(DateOnly KnownAsOf, double Value)>();
        foreach (var print in prints.OrderBy(p => p.PeriodEnd))
        {
            var known = KnownAsOfCashSession(print.PeriodEnd, cashSessions);
            if (known is null)
            {
                continue;
            }
            steps.Add((known.Value, print.Value));
        }

        for (int i = 0; i < extractDates.Count; i++)
        {
            if (!cashSet.Contains(extractDates[i]))
            {
                continue;
            }
            double v = double.NaN;
            foreach (var step in steps)
            {
                if (step.KnownAsOf <= extractDates[i])
                {
                    v = step.Value;
                }
            }
            values[i] = v;
        }

        ExtractUtil.FillIn(values);
        return values;
    }

    public static async Task<MacroFetchResult> FetchUsaAsync(
        Client client,
        IReadOnlyList<string>? keys = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        keys ??= RequestedKeys;
        var result = new MacroFetchResult();
        foreach (var key in keys)
        {
            try
            {
                var rows = await client.GetMacroIndicatorAsync(Country, key, ct);
                if (rows.Count == 0)
                {
                    result.EmptyKeys.Add(key);
                    continue;
                }
                result.Series[key] = rows;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                result.Forbidden = true;
                result.ForbiddenKey = key;
                result.StatusNote = "403 Forbidden: Macro Indicators are not on the current EODHD plan. Stopped. Did not scrape.";
                return result;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException("Macro Indicators API returned HTTP 401 (bad token).", ex);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                result.MissingKeys.Add(key);
            }
        }
        return result;
    }

    public static void WriteSidecarAndSeries(MacroFetchResult fetch, string sidecarPath, string seriesDirectory)
    {
        ArgumentNullException.ThrowIfNull(fetch);
        ArgumentException.ThrowIfNullOrWhiteSpace(sidecarPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(seriesDirectory);
        Directory.CreateDirectory(seriesDirectory);

        using (var writer = new StreamWriter(sidecarPath, false, new UTF8Encoding(false)))
        {
            var csv = new CsvWriter(writer);
            csv.WriteRow(["key", "indicator", "first_date", "last_date", "observation_count"]);
            foreach (var kv in fetch.Series)
            {
                var ordered = kv.Value
                    .Where(x => !string.IsNullOrWhiteSpace(x.Date))
                    .OrderBy(x => x.Date, StringComparer.Ordinal)
                    .ToList();
                var indicator = ordered.FirstOrDefault()?.Indicator ?? "";
                var first = ordered.FirstOrDefault()?.Date ?? "";
                var last = ordered.LastOrDefault()?.Date ?? "";
                csv.WriteRow(
                [
                    kv.Key,
                    indicator,
                    first,
                    last,
                    ordered.Count.ToString(CultureInfo.InvariantCulture)
                ]);

                var seriesPath = Path.Combine(seriesDirectory, $"{kv.Key}.csv");
                WriteSeriesCsv(seriesPath, ordered);
            }
        }
    }

    public static MacroJoinResult JoinOntoExtract(string extractPath, MacroFetchResult fetch)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extractPath);
        ArgumentNullException.ThrowIfNull(fetch);

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
        int dateIdx = ExtractJoin.IndexOf(header, "Date");
        int openIdx = ExtractJoin.IndexOf(header, "qqq.Open");
        int highIdx = ExtractJoin.IndexOf(header, "qqq.High");
        int lowIdx = ExtractJoin.IndexOf(header, "qqq.Low");
        int closeIdx = ExtractJoin.IndexOf(header, "qqq.Close");
        int volIdx = ExtractJoin.IndexOf(header, "qqq.Volume");

        var extractDates = new List<DateOnly>(before);
        var dates = new List<string>(before);
        var qqq = new List<(double, double, double, double, double)>(before);
        for (int r = 1; r < grid.Count; r++)
        {
            var row = grid[r];
            dates.Add(row[dateIdx]);
            extractDates.Add(DateOnly.Parse(row[dateIdx], CultureInfo.InvariantCulture));
            qqq.Add((
                ParseDouble(row[openIdx]),
                ParseDouble(row[highIdx]),
                ParseDouble(row[lowIdx]),
                ParseDouble(row[closeIdx]),
                ParseDouble(row[volIdx])));
        }
        var cash = EventCalendar.CashSessionsFromQqq(dates, qqq);

        var names = new List<string>();
        var cols = new List<double[]>();
        foreach (var kv in ExtractColumnByKey)
        {
            if (!fetch.Series.TryGetValue(kv.Key, out var rows))
            {
                continue;
            }
            var prints = new List<(DateOnly PeriodEnd, double Value)>();
            foreach (var obs in rows)
            {
                if (string.IsNullOrWhiteSpace(obs.Date) || obs.Value is null)
                {
                    continue;
                }
                prints.Add((DateOnly.Parse(obs.Date, CultureInfo.InvariantCulture), obs.Value.Value));
            }
            names.Add(kv.Value);
            cols.Add(JoinAnnualPrints(extractDates, cash, prints));
        }

        if (names.Count > 0)
        {
            var insertAt = ExtractJoin.InsertIndex(grid[0], [], EventCalendar.FlagColumns);
            ExtractJoin.AddOrReplaceDoubleColumns(grid, names, cols, insertAt);
        }

        if (grid.Count - 1 != before)
        {
            throw new InvalidOperationException($"Extract row count changed: before={before} after={grid.Count - 1}");
        }

        using (var writer = new StreamWriter(extractPath, false, new UTF8Encoding(false)))
        {
            CsvWriter.WriteAll(grid, writer);
        }

        return new MacroJoinResult
        {
            DataRows = before,
            CashSessions = cash.Count,
            ColumnsAdded = names.ToArray(),
            FirstDate = extractDates[0],
            LastDate = extractDates[^1]
        };
    }

    #region private

    static void WriteSeriesCsv(string path, List<MacroIndicator> rows)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        var csv = new CsvWriter(writer);
        csv.WriteRow(["CountryCode", "CountryName", "Indicator", "Date", "Period", "Value"]);
        foreach (var row in rows)
        {
            csv.WriteRow(
            [
                row.CountryCode ?? "",
                row.CountryName ?? "",
                row.Indicator ?? "",
                row.Date ?? "",
                row.Period ?? "",
                row.Value is null ? "" : EventCalendar.FormatNumber(row.Value)
            ]);
        }
    }

    static double ParseDouble(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return double.NaN;
        }
        return double.Parse(s, CultureInfo.InvariantCulture);
    }

    #endregion
}

public sealed class MacroFetchResult
{
    public Dictionary<string, List<MacroIndicator>> Series { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> MissingKeys { get; } = [];
    public List<string> EmptyKeys { get; } = [];
    public bool Forbidden { get; set; }
    public string? ForbiddenKey { get; set; }
    public string? StatusNote { get; set; }
}

public sealed class MacroJoinResult
{
    public int DataRows { get; set; }
    public int CashSessions { get; set; }
    public string[] ColumnsAdded { get; set; } = [];
    public DateOnly FirstDate { get; set; }
    public DateOnly LastDate { get; set; }
}
