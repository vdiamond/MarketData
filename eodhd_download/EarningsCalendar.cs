using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using EodHdExtract;
using TableCsv;

namespace eodhd_download;

/// <summary>
/// QQQ-member earnings calendar dump and known-as-of join onto extract cash sessions (16:00 ET cutoff).
/// BeforeMarket joins report_date cash (or prior if weekend/holiday). AfterMarket joins the next cash session.
/// Date-only / null timing sets evt_earn on that session and leaves BMO/AMC 0.
/// </summary>
public static class EarningsCalendar
{
    public const string EarnFlagColumn = "evt_earn";
    public const string EarnBmoColumn = "evt_earn_bmo";
    public const string EarnAmcColumn = "evt_earn_amc";
    public const string EarnCountColumn = "evt_earn_n";

    public static readonly string[] FlagColumns =
    [
        EarnFlagColumn,
        EarnBmoColumn,
        EarnAmcColumn,
        EarnCountColumn
    ];

    public static readonly string[] CandidateTickers =
    [
        "AAPL", "MSFT", "NVDA", "AMZN", "META", "GOOGL", "GOOG", "TSLA", "AVGO", "NFLX",
        "COST", "AMD", "ADBE", "PEP", "CSCO", "QCOM", "INTC", "AMGN", "INTU", "BKNG",
        "ISRG", "AMAT", "ADP", "GILD", "VRTX", "SBUX", "LRCX", "MU", "ADI", "PANW",
        "REGN", "KLAC", "SNPS", "CRWD", "SHOP", "MELI", "APP", "PLTR", "ARM", "CEG", "WMT"
    ];

    public static readonly DateOnly DefaultFrom = new(2015, 11, 23);
    public static readonly TimeOnly RegularOpen = new(9, 30);

    public static string ToEodhdCode(string ticker)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticker);
        return $"{ticker.Trim().ToUpperInvariant()}.US";
    }

    public static List<string> UniverseFromHeader(IReadOnlyList<string> header, out List<string> skipped)
    {
        ArgumentNullException.ThrowIfNull(header);
        skipped = [];
        var present = new List<string>();
        foreach (var ticker in CandidateTickers)
        {
            var col = $"{ticker.ToLowerInvariant()}.Close";
            var found = false;
            for (int i = 0; i < header.Count; i++)
            {
                if (string.Equals(header[i], col, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }
            if (found)
            {
                present.Add(ToEodhdCode(ticker));
            }
            else
            {
                skipped.Add(ticker);
            }
        }
        return present;
    }

    public static EarningsTiming ClassifyTiming(string? beforeAfterMarket)
    {
        if (string.IsNullOrWhiteSpace(beforeAfterMarket))
        {
            return EarningsTiming.DateOnly;
        }
        var s = beforeAfterMarket.Trim();
        if (s.Equals("BeforeMarket", StringComparison.OrdinalIgnoreCase)
            || s.Equals("BMO", StringComparison.OrdinalIgnoreCase)
            || s.Equals("Before", StringComparison.OrdinalIgnoreCase))
        {
            return EarningsTiming.BeforeMarket;
        }
        if (s.Equals("AfterMarket", StringComparison.OrdinalIgnoreCase)
            || s.Equals("AMC", StringComparison.OrdinalIgnoreCase)
            || s.Equals("After", StringComparison.OrdinalIgnoreCase))
        {
            return EarningsTiming.AfterMarket;
        }
        if (TimeOnly.TryParse(s, CultureInfo.InvariantCulture, out var time))
        {
            if (time < RegularOpen)
            {
                return EarningsTiming.BeforeMarket;
            }
            if (time >= EventCalendar.CashCloseCutoff)
            {
                return EarningsTiming.AfterMarket;
            }
            return EarningsTiming.DateOnly;
        }
        return EarningsTiming.DateOnly;
    }

    public static DateOnly? JoinReportToCashSession(
        DateOnly reportDate,
        EarningsTiming timing,
        IReadOnlyList<DateOnly> cashSessions)
    {
        ArgumentNullException.ThrowIfNull(cashSessions);
        if (timing == EarningsTiming.AfterMarket)
        {
            return EventCalendar.NextCashSession(reportDate, cashSessions);
        }
        return EventCalendar.PriorOrSameCashSession(reportDate, cashSessions);
    }

    public static async Task<EarningsFetchResult> FetchAsync(
        Client client,
        IReadOnlyList<string> symbols,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(symbols);
        var result = new EarningsFetchResult
        {
            From = from,
            To = to
        };
        if (symbols.Count == 0)
        {
            return result;
        }
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await FetchRecursive(client, symbols.ToList(), from, to, result, seen, ct);
        result.Items.Sort((a, b) =>
        {
            var d = string.CompareOrdinal(a.ReportDate, b.ReportDate);
            if (d != 0)
            {
                return d;
            }
            return string.CompareOrdinal(a.Code, b.Code);
        });
        return result;
    }

    static async Task FetchRecursive(
        Client client,
        List<string> symbols,
        DateOnly from,
        DateOnly to,
        EarningsFetchResult result,
        HashSet<string> seen,
        CancellationToken ct)
    {
        if (result.Forbidden || symbols.Count == 0 || from > to)
        {
            return;
        }
        try
        {
            Console.WriteLine($"calendar/earnings symbols={symbols.Count} {from:yyyy-MM-dd}..{to:yyyy-MM-dd}");
            var page = await client.GetCalendarEarningsAsync(from, to, symbols, ct);
            AddDeduped(page, result, seen);
            Console.WriteLine($"calendar/earnings fetched={page.Count} kept={result.Items.Count}");
            return;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            result.Forbidden = true;
            result.StatusNote = "403 Forbidden: Earnings calendar is not on the current EODHD plan (Fundamentals / All-In-One / Corporate Events). Stopped. Did not scrape.";
            Console.WriteLine(result.StatusNote);
            return;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("Earnings calendar API returned HTTP 401 (bad token).", ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            if (symbols.Count == 1)
            {
                result.UnknownSymbols.Add(symbols[0]);
                Console.WriteLine($"calendar/earnings 404 unknown symbol {symbols[0]}; skipped");
                return;
            }
            Console.WriteLine($"calendar/earnings 404 on {symbols.Count} symbols; splitting");
        }
        catch (HttpRequestException ex) when (
            ex.StatusCode == HttpStatusCode.InternalServerError
            || (int?)ex.StatusCode == 422
            || (int?)ex.StatusCode == 414)
        {
            Console.WriteLine($"calendar/earnings HTTP {(int?)ex.StatusCode} on {symbols.Count} symbols {from:yyyy-MM-dd}..{to:yyyy-MM-dd}; splitting");
        }
        catch (Exception ex) when (IsLikelyTooLarge(ex))
        {
            Console.WriteLine($"calendar/earnings request failed ({ex.GetType().Name}); splitting batch");
        }

        if (symbols.Count > 1)
        {
            var mid = symbols.Count / 2;
            await FetchRecursive(client, symbols.GetRange(0, mid), from, to, result, seen, ct);
            if (result.Forbidden)
            {
                return;
            }
            await FetchRecursive(client, symbols.GetRange(mid, symbols.Count - mid), from, to, result, seen, ct);
            return;
        }

        if (from < to)
        {
            var spanDays = to.DayNumber - from.DayNumber;
            if (from.Year < to.Year)
            {
                var yearEnd = new DateOnly(from.Year, 12, 31);
                if (yearEnd >= to)
                {
                    yearEnd = from.AddDays(Math.Max(1, spanDays / 2));
                }
                await FetchRecursive(client, symbols, from, yearEnd, result, seen, ct);
                if (result.Forbidden)
                {
                    return;
                }
                if (yearEnd < to)
                {
                    await FetchRecursive(client, symbols, yearEnd.AddDays(1), to, result, seen, ct);
                }
                return;
            }
            var midDate = from.AddDays(Math.Max(1, spanDays / 2));
            if (midDate >= to)
            {
                midDate = from;
            }
            await FetchRecursive(client, symbols, from, midDate, result, seen, ct);
            if (result.Forbidden || midDate >= to)
            {
                return;
            }
            await FetchRecursive(client, symbols, midDate.AddDays(1), to, result, seen, ct);
            return;
        }

        result.FailedWindows.Add($"{symbols[0]} {from:yyyy-MM-dd}..{to:yyyy-MM-dd}");
        Console.WriteLine($"calendar/earnings giving up on {symbols[0]} {from:yyyy-MM-dd}");
    }

    static bool IsLikelyTooLarge(Exception ex)
    {
        var msg = ex.Message ?? "";
        if (msg.Contains("URI", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("too long", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return ex.InnerException != null && IsLikelyTooLarge(ex.InnerException);
    }

    static void AddDeduped(List<EarningsCalendarItem> page, EarningsFetchResult result, HashSet<string> seen)
    {
        foreach (var item in page)
        {
            if (item == null)
            {
                continue;
            }
            var key = string.Join('\u001f', item.Code ?? "", item.ReportDate ?? "", item.Date ?? "", item.Period ?? "");
            if (seen.Add(key))
            {
                result.Items.Add(item);
            }
        }
    }

    public static void WriteDump(string path, IReadOnlyList<EarningsCalendarItem> items, IReadOnlyCollection<string>? universeCodes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(items);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        HashSet<string>? allow = null;
        if (universeCodes != null)
        {
            allow = new HashSet<string>(universeCodes, StringComparer.OrdinalIgnoreCase);
        }
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        var csv = new CsvWriter(writer);
        csv.WriteRow(
        [
            "symbol",
            "report_date",
            "fiscal_date",
            "before_after_market",
            "actual",
            "estimate",
            "difference",
            "percent",
            "period"
        ]);
        foreach (var item in items)
        {
            var code = item.Code ?? "";
            if (allow != null && (code.Length == 0 || !allow.Contains(code)))
            {
                continue;
            }
            csv.WriteRow(
            [
                code,
                item.ReportDate ?? "",
                item.Date ?? "",
                item.BeforeAfterMarket ?? "",
                EventCalendar.FormatNumber(item.Actual),
                EventCalendar.FormatNumber(item.Estimate),
                EventCalendar.FormatNumber(item.Difference),
                EventCalendar.FormatNumber(item.Percent),
                item.Period ?? ""
            ]);
        }
    }

    public static EarningsJoinResult ApplyFlagsToExtract(
        List<List<string>> grid,
        IReadOnlyList<EarningsCalendarItem> items,
        IReadOnlyCollection<string>? universeCodes = null)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(items);
        if (grid.Count == 0)
        {
            throw new InvalidOperationException("Extract is empty.");
        }

        var header = grid[0];
        ExtractJoin.StripColumns(grid, FlagColumns);
        header = grid[0];

        int dateIdx = ExtractJoin.IndexOf(header, "Date");
        int openIdx = ExtractJoin.IndexOf(header, "qqq.Open");
        int highIdx = ExtractJoin.IndexOf(header, "qqq.High");
        int lowIdx = ExtractJoin.IndexOf(header, "qqq.Low");
        int closeIdx = ExtractJoin.IndexOf(header, "qqq.Close");
        int volIdx = ExtractJoin.IndexOf(header, "qqq.Volume");

        var dates = new List<string>(grid.Count - 1);
        var qqq = new List<(double, double, double, double, double)>(grid.Count - 1);
        for (int r = 1; r < grid.Count; r++)
        {
            var row = grid[r];
            dates.Add(row[dateIdx]);
            qqq.Add((
                ParseDouble(row[openIdx]),
                ParseDouble(row[highIdx]),
                ParseDouble(row[lowIdx]),
                ParseDouble(row[closeIdx]),
                ParseDouble(row[volIdx])));
        }
        var cash = EventCalendar.CashSessionsFromQqq(dates, qqq);

        HashSet<string>? allow = null;
        if (universeCodes != null)
        {
            allow = new HashSet<string>(universeCodes, StringComparer.OrdinalIgnoreCase);
        }

        var namesByDate = new Dictionary<DateOnly, HashSet<string>>();
        var bmoByDate = new Dictionary<DateOnly, bool>();
        var amcByDate = new Dictionary<DateOnly, bool>();
        int mapped = 0;
        int unmapped = 0;

        foreach (var item in items)
        {
            var code = item.Code ?? "";
            if (allow != null && (code.Length == 0 || !allow.Contains(code)))
            {
                continue;
            }
            if (string.IsNullOrWhiteSpace(item.ReportDate)
                || !DateOnly.TryParse(item.ReportDate, CultureInfo.InvariantCulture, out var reportDate))
            {
                unmapped++;
                continue;
            }
            var timing = ClassifyTiming(item.BeforeAfterMarket);
            var join = JoinReportToCashSession(reportDate, timing, cash);
            if (join is null)
            {
                unmapped++;
                continue;
            }
            mapped++;
            if (!namesByDate.TryGetValue(join.Value, out var names))
            {
                names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                namesByDate[join.Value] = names;
            }
            names.Add(code);
            if (timing == EarningsTiming.BeforeMarket)
            {
                bmoByDate[join.Value] = true;
            }
            else if (timing == EarningsTiming.AfterMarket)
            {
                amcByDate[join.Value] = true;
            }
        }

        var nEarn = new double[dates.Count];
        var nBmo = new double[dates.Count];
        var nAmc = new double[dates.Count];
        var nCount = new double[dates.Count];
        int earn1 = 0, bmo1 = 0, amc1 = 0;
        for (int i = 0; i < dates.Count; i++)
        {
            var date = DateOnly.Parse(dates[i], CultureInfo.InvariantCulture);
            var hit = namesByDate.TryGetValue(date, out var names);
            var count = hit ? names!.Count : 0;
            var bmo = bmoByDate.TryGetValue(date, out var b) && b;
            var amc = amcByDate.TryGetValue(date, out var a) && a;
            nEarn[i] = hit ? 1 : 0;
            nBmo[i] = bmo ? 1 : 0;
            nAmc[i] = amc ? 1 : 0;
            nCount[i] = count;
            if (hit) { earn1++; }
            if (bmo) { bmo1++; }
            if (amc) { amc1++; }
        }

        var insertAt = ExtractJoin.InsertIndex(grid[0], EventCalendar.FlagColumns, []);
        ExtractJoin.AddOrReplaceDoubleColumns(
            grid,
            FlagColumns,
            [nEarn, nBmo, nAmc, nCount],
            insertAt);

        var firstDate = DateOnly.Parse(dates[0], CultureInfo.InvariantCulture);
        var lastDate = DateOnly.Parse(dates[^1], CultureInfo.InvariantCulture);
        return new EarningsJoinResult
        {
            DataRows = grid.Count - 1,
            CashSessions = cash.Count,
            EarnFlags = earn1,
            BmoFlags = bmo1,
            AmcFlags = amc1,
            MappedReports = mapped,
            UnmappedReports = unmapped,
            FirstDate = firstDate,
            LastDate = lastDate
        };
    }

    public static async Task<EarningsJoinResult> WriteEarningsFlagsAsync(
        Client client,
        string extractPath,
        string dumpPath,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(extractPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(dumpPath);

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
        var universe = UniverseFromHeader(grid[0], out var skipped);
        Console.WriteLine($"Earnings universe {universe.Count} codes; skipped={string.Join(",", skipped)}");

        var fromDate = from ?? DefaultFrom;
        var lastExtract = DateOnly.Parse(grid[^1][ExtractJoin.IndexOf(grid[0], "Date")], CultureInfo.InvariantCulture);
        var toDate = to ?? lastExtract;
        var fetch = await FetchAsync(client, universe, fromDate, toDate, ct);
        var filtered = FilterUniverse(fetch.Items, universe);
        WriteDump(dumpPath, filtered, universe);
        Console.WriteLine($"Wrote earnings dump {dumpPath} rows={filtered.Count}");

        var join = ApplyFlagsToExtract(grid, filtered, universe);
        if (join.DataRows != before)
        {
            throw new InvalidOperationException($"Extract row count changed: before={before} after={join.DataRows}");
        }
        using (var writer = new StreamWriter(extractPath, false, new UTF8Encoding(false)))
        {
            CsvWriter.WriteAll(grid, writer);
        }
        join.DumpRows = filtered.Count;
        join.DumpPath = dumpPath;
        join.SkippedTickers = skipped;
        join.UnknownSymbols = fetch.UnknownSymbols;
        join.Forbidden = fetch.Forbidden;
        join.StatusNote = fetch.StatusNote;
        join.CalendarFrom = fromDate;
        join.CalendarTo = toDate;
        join.UniverseCount = universe.Count;
        Console.WriteLine($"Wrote earnings flags onto {extractPath}: rows={join.DataRows} evt_earn={join.EarnFlags} bmo={join.BmoFlags} amc={join.AmcFlags}");
        return join;
    }

    public static List<EarningsCalendarItem> FilterUniverse(
        IReadOnlyList<EarningsCalendarItem> items,
        IReadOnlyCollection<string> universeCodes)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(universeCodes);
        var allow = new HashSet<string>(universeCodes, StringComparer.OrdinalIgnoreCase);
        var kept = new List<EarningsCalendarItem>();
        foreach (var item in items)
        {
            var code = item.Code ?? "";
            if (code.Length > 0 && allow.Contains(code))
            {
                kept.Add(item);
            }
        }
        return kept;
    }

    static double ParseDouble(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return double.NaN;
        }
        return double.Parse(s, CultureInfo.InvariantCulture);
    }
}

public enum EarningsTiming
{
    DateOnly,
    BeforeMarket,
    AfterMarket
}

public sealed class EarningsFetchResult
{
    public List<EarningsCalendarItem> Items { get; } = [];
    public List<string> UnknownSymbols { get; } = [];
    public List<string> FailedWindows { get; } = [];
    public bool Forbidden { get; set; }
    public string? StatusNote { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
}

public sealed class EarningsJoinResult
{
    public int DataRows { get; set; }
    public int CashSessions { get; set; }
    public int EarnFlags { get; set; }
    public int BmoFlags { get; set; }
    public int AmcFlags { get; set; }
    public int MappedReports { get; set; }
    public int UnmappedReports { get; set; }
    public int DumpRows { get; set; }
    public string? DumpPath { get; set; }
    public List<string> SkippedTickers { get; set; } = [];
    public List<string> UnknownSymbols { get; set; } = [];
    public bool Forbidden { get; set; }
    public string? StatusNote { get; set; }
    public DateOnly FirstDate { get; set; }
    public DateOnly LastDate { get; set; }
    public DateOnly CalendarFrom { get; set; }
    public DateOnly CalendarTo { get; set; }
    public int UniverseCount { get; set; }
}
