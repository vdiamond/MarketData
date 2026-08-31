using System.Globalization;
using System.Text;
using EodHdExtract;
using TableCsv;

namespace eodhd_download;

/// <summary>
/// US economic-event dump, cash-session join (16:00 ET cutoff), and monthly OPEX flags.
/// Cash sessions are defined from extract QQQ OHLCV, not NyseCalendar.
/// </summary>
public static class EventCalendar
{
    public const string CpiFlagColumn = "evt_cpi";
    public const string FomcFlagColumn = "evt_fomc";
    public const string OpexFlagColumn = "evt_opex";
    public const string CpiSurpriseColumn = "evt_cpi_surprise";
    public const string FomcSurpriseColumn = "evt_fomc_surprise";

    public static readonly string[] FlagColumns =
    [
        CpiFlagColumn,
        FomcFlagColumn,
        OpexFlagColumn,
        CpiSurpriseColumn,
        FomcSurpriseColumn
    ];

    public static readonly string[] CpiTypeNeedles =
    [
        "CPI",
        "Inflation Rate",
        "Core Inflation",
        "PCE Price"
    ];

    public static readonly string[] FomcTypeNeedles =
    [
        "FOMC",
        "Fed Interest Rate",
        "Interest Rate Decision",
        "Federal Funds"
    ];

    public static readonly TimeOnly CashCloseCutoff = new(16, 0);

    public static TimeZoneInfo EasternTimeZone { get; } = ResolveEastern();

    static TimeZoneInfo ResolveEastern()
    {
        foreach (var id in new[] { "America/New_York", "Eastern Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }
        throw new InvalidOperationException("America/New_York timezone is not available.");
    }

    public static bool IsCpiFamily(string? type)
    {
        return ContainsAny(type, CpiTypeNeedles);
    }

    public static bool IsFomcFamily(string? type)
    {
        return ContainsAny(type, FomcTypeNeedles);
    }

    public static string FamilyOf(string? type)
    {
        if (IsCpiFamily(type))
        {
            return "cpi";
        }
        if (IsFomcFamily(type))
        {
            return "fomc";
        }
        return "other";
    }

    static bool ContainsAny(string? type, string[] needles)
    {
        if (string.IsNullOrEmpty(type))
        {
            return false;
        }
        foreach (var needle in needles)
        {
            if (type.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public static DateTime ParseEventToEastern(string date)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(date);
        var trimmed = date.Trim();
        DateTime utc;
        if (HasExplicitZone(trimmed))
        {
            var dto = DateTimeOffset.Parse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            utc = dto.UtcDateTime;
        }
        else if (DateTime.TryParseExact(trimmed, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                 || DateTime.TryParseExact(trimmed, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            utc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }
        else
        {
            utc = DateTime.SpecifyKind(
                DateTime.Parse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
                DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTimeFromUtc(utc, EasternTimeZone);
    }

    static bool HasExplicitZone(string date)
    {
        if (date.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        // ISO offset at the end: +HH:MM / -HH:MM / +HHMM
        var space = date.LastIndexOf(' ');
        var t = date.LastIndexOf('T');
        var start = Math.Max(space, t);
        if (start < 0)
        {
            return false;
        }
        var rest = date.AsSpan(start + 1);
        for (int i = 0; i < rest.Length; i++)
        {
            if (rest[i] is '+' or '-')
            {
                return i > 0;
            }
        }
        return false;
    }

    public static DateOnly ThirdFriday(int year, int month)
    {
        var date = new DateOnly(year, month, 1);
        while (date.DayOfWeek != DayOfWeek.Friday)
        {
            date = date.AddDays(1);
        }
        return date.AddDays(14);
    }

    public static List<DateOnly> CashSessionsFromQqq(
        IReadOnlyList<string> dates,
        IReadOnlyList<(double Open, double High, double Low, double Close, double Volume)> qqq)
    {
        if (dates.Count != qqq.Count)
        {
            throw new ArgumentException("Date and QQQ series must be the same length.");
        }
        var sessions = new List<DateOnly>(dates.Count);
        for (int i = 0; i < dates.Count; i++)
        {
            var dup = i > 0
                && qqq[i].Open == qqq[i - 1].Open
                && qqq[i].High == qqq[i - 1].High
                && qqq[i].Low == qqq[i - 1].Low
                && qqq[i].Close == qqq[i - 1].Close
                && qqq[i].Volume == qqq[i - 1].Volume;
            if (!dup)
            {
                sessions.Add(DateOnly.Parse(dates[i], CultureInfo.InvariantCulture));
            }
        }
        return sessions;
    }

    public static DateOnly? JoinCashDate(DateTime eastern, IReadOnlyList<DateOnly> cashSessions)
    {
        var calendarDate = DateOnly.FromDateTime(eastern);
        var time = TimeOnly.FromDateTime(eastern);
        if (time <= CashCloseCutoff)
        {
            return PriorOrSameCashSession(calendarDate, cashSessions);
        }
        return NextCashSession(calendarDate, cashSessions);
    }

    public static DateOnly? PriorOrSameCashSession(DateOnly date, IReadOnlyList<DateOnly> cashSessions)
    {
        for (int i = cashSessions.Count - 1; i >= 0; i--)
        {
            if (cashSessions[i] <= date)
            {
                return cashSessions[i];
            }
        }
        return null;
    }

    public static DateOnly? NextCashSession(DateOnly date, IReadOnlyList<DateOnly> cashSessions)
    {
        for (int i = 0; i < cashSessions.Count; i++)
        {
            if (cashSessions[i] > date)
            {
                return cashSessions[i];
            }
        }
        return null;
    }

    public static List<DateOnly> MonthlyOpexDates(DateOnly windowFrom, DateOnly windowTo, IReadOnlyList<DateOnly> cashSessions)
    {
        var dates = new List<DateOnly>();
        var cursor = new DateOnly(windowFrom.Year, windowFrom.Month, 1);
        var lastMonth = new DateOnly(windowTo.Year, windowTo.Month, 1);
        while (cursor <= lastMonth)
        {
            var friday = ThirdFriday(cursor.Year, cursor.Month);
            var join = PriorOrSameCashSession(friday, cashSessions);
            if (join != null)
            {
                dates.Add(join.Value);
            }
            cursor = cursor.AddMonths(1);
        }
        return dates;
    }

    public static double? Surprise(double? actual, double? estimate)
    {
        if (actual is null || estimate is null)
        {
            return null;
        }
        return actual.Value - estimate.Value;
    }

    public static string EventDedupKey(EconomicEvent ev)
    {
        return string.Join('\u001f',
            ev.Type ?? "",
            ev.Date ?? "",
            ev.Comparison ?? "",
            ev.Period ?? "",
            ev.Country ?? "",
            FormatNumber(ev.Actual),
            FormatNumber(ev.Estimate));
    }

    public static async Task<List<EconomicEvent>> FetchAllUsEventsAsync(
        Client client,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        var all = new List<EconomicEvent>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var windowStart = from;
        while (windowStart <= to)
        {
            var qEndMonth = ((windowStart.Month - 1) / 3) * 3 + 3;
            var windowEnd = new DateOnly(windowStart.Year, qEndMonth, 1).AddMonths(1).AddDays(-1);
            if (windowEnd > to)
            {
                windowEnd = to;
            }
            await FetchWindowRecursive(client, windowStart, windowEnd, all, seen, ct);
            windowStart = windowEnd.AddDays(1);
        }
        all.Sort((a, b) => string.CompareOrdinal(a.Date, b.Date));
        return all;
    }

    static async Task FetchWindowRecursive(
        Client client,
        DateOnly from,
        DateOnly to,
        List<EconomicEvent> all,
        HashSet<string> seen,
        CancellationToken ct)
    {
        var page0 = await client.GetEconomicEventsAsync("US", from, to, 1000, 0, ct);
        if (page0.Count < 1000)
        {
            AddDeduped(page0, all, seen);
            Console.WriteLine($"economic-events {from:yyyy-MM-dd}..{to:yyyy-MM-dd} offset=0 count={page0.Count}");
            return;
        }

        var page1 = await client.GetEconomicEventsAsync("US", from, to, 1000, 1000, ct);
        if (page1.Count < 1000)
        {
            AddDeduped(page0, all, seen);
            AddDeduped(page1, all, seen);
            Console.WriteLine($"economic-events {from:yyyy-MM-dd}..{to:yyyy-MM-dd} offset=0/1000 count={page0.Count + page1.Count}");
            return;
        }

        if (from >= to)
        {
            AddDeduped(page0, all, seen);
            AddDeduped(page1, all, seen);
            Console.WriteLine($"economic-events {from:yyyy-MM-dd}..{to:yyyy-MM-dd} hit offset cap on a single-day window; kept {page0.Count + page1.Count}");
            return;
        }

        Console.WriteLine($"economic-events {from:yyyy-MM-dd}..{to:yyyy-MM-dd} full at offset 0 and 1000; splitting window");
        var spanDays = to.DayNumber - from.DayNumber;
        var mid = from.AddDays(spanDays / 2);
        if (mid <= from)
        {
            mid = from;
        }
        await FetchWindowRecursive(client, from, mid, all, seen, ct);
        if (mid < to)
        {
            await FetchWindowRecursive(client, mid.AddDays(1), to, all, seen, ct);
        }
    }

    static void AddDeduped(List<EconomicEvent> page, List<EconomicEvent> all, HashSet<string> seen)
    {
        foreach (var ev in page)
        {
            if (seen.Add(EventDedupKey(ev)))
            {
                all.Add(ev);
            }
        }
    }

    public static void WriteEventDump(string eventsPath, string typesPath, IReadOnlyList<EconomicEvent> events)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(eventsPath))!);
        using (var writer = new StreamWriter(eventsPath, false, new UTF8Encoding(false)))
        {
            var csv = new CsvWriter(writer);
            csv.WriteRow(
            [
                "event_type",
                "comparison",
                "period",
                "country",
                "event_utc",
                "event_et_date",
                "event_et_time",
                "actual",
                "previous",
                "estimate",
                "change",
                "change_percentage"
            ]);
            foreach (var ev in events)
            {
                DateTime et;
                try
                {
                    et = ParseEventToEastern(ev.Date ?? "");
                }
                catch
                {
                    et = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified);
                }
                csv.WriteRow(
                [
                    ev.Type ?? "",
                    ev.Comparison ?? "",
                    ev.Period ?? "",
                    ev.Country ?? "",
                    ev.Date ?? "",
                    et == DateTime.MinValue ? "" : et.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    et == DateTime.MinValue ? "" : et.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                    FormatNumber(ev.Actual),
                    FormatNumber(ev.Previous),
                    FormatNumber(ev.Estimate),
                    FormatNumber(ev.Change),
                    FormatNumber(ev.ChangePercentage)
                ]);
            }
        }

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var ev in events)
        {
            var t = ev.Type ?? "";
            counts[t] = counts.TryGetValue(t, out var n) ? n + 1 : 1;
        }
        using (var writer = new StreamWriter(typesPath, false, new UTF8Encoding(false)))
        {
            var csv = new CsvWriter(writer);
            csv.WriteRow(["event_type", "count", "family"]);
            foreach (var kv in counts.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.Ordinal))
            {
                csv.WriteRow([kv.Key, kv.Value.ToString(CultureInfo.InvariantCulture), FamilyOf(kv.Key)]);
            }
        }
    }

    public static EventJoinResult ApplyFlagsToExtract(
        List<List<string>> grid,
        IReadOnlyList<EconomicEvent> events)
    {
        if (grid.Count == 0)
        {
            throw new InvalidOperationException("Extract is empty.");
        }
        var header = grid[0];
        StripExistingFlagColumns(grid);
        header = grid[0];

        int dateIdx = IndexOf(header, "Date");
        int openIdx = IndexOf(header, "qqq.Open");
        int highIdx = IndexOf(header, "qqq.High");
        int lowIdx = IndexOf(header, "qqq.Low");
        int closeIdx = IndexOf(header, "qqq.Close");
        int volIdx = IndexOf(header, "qqq.Volume");

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

        var cash = CashSessionsFromQqq(dates, qqq);
        var firstDate = DateOnly.Parse(dates[0], CultureInfo.InvariantCulture);
        var lastDate = DateOnly.Parse(dates[^1], CultureInfo.InvariantCulture);
        var opex = new HashSet<DateOnly>(MonthlyOpexDates(firstDate, lastDate, cash));

        var cpiByDate = new Dictionary<DateOnly, (string Utc, double? Surprise)>();
        var fomcByDate = new Dictionary<DateOnly, (string Utc, double? Surprise)>();
        var keptTypes = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var ev in events)
        {
            var family = FamilyOf(ev.Type);
            if (family == "other")
            {
                continue;
            }
            keptTypes.Add(ev.Type ?? "");
            if (string.IsNullOrWhiteSpace(ev.Date))
            {
                continue;
            }
            var et = ParseEventToEastern(ev.Date);
            var join = JoinCashDate(et, cash);
            if (join is null)
            {
                continue;
            }
            var surprise = Surprise(ev.Actual, ev.Estimate);
            var utc = ev.Date ?? "";
            var dict = family == "cpi" ? cpiByDate : fomcByDate;
            if (!dict.TryGetValue(join.Value, out var existing) || string.CompareOrdinal(utc, existing.Utc) >= 0)
            {
                dict[join.Value] = (utc, surprise);
            }
        }

        header.AddRange(FlagColumns);
        int cpi1 = 0, fomc1 = 0, opex1 = 0, pre2020 = 0, pre2020NonZero = 0;
        var cutoff2020 = new DateOnly(2020, 1, 1);

        for (int r = 1; r < grid.Count; r++)
        {
            var row = grid[r];
            var date = DateOnly.Parse(row[dateIdx], CultureInfo.InvariantCulture);
            var cpi = cpiByDate.TryGetValue(date, out var cpiHit);
            var fomc = fomcByDate.TryGetValue(date, out var fomcHit);
            var isOpex = opex.Contains(date);
            row.Add(cpi ? "1" : "0");
            row.Add(fomc ? "1" : "0");
            row.Add(isOpex ? "1" : "0");
            row.Add(cpi ? FormatNumber(cpiHit.Surprise) : "");
            row.Add(fomc ? FormatNumber(fomcHit.Surprise) : "");
            if (cpi) { cpi1++; }
            if (fomc) { fomc1++; }
            if (isOpex) { opex1++; }
            if (date < cutoff2020)
            {
                pre2020++;
                if (cpi || fomc || row[^2] != "" || row[^1] != "")
                {
                    pre2020NonZero++;
                }
            }
        }

        return new EventJoinResult
        {
            DataRows = grid.Count - 1,
            CashSessions = cash.Count,
            CpiFlags = cpi1,
            FomcFlags = fomc1,
            OpexFlags = opex1,
            Pre2020Rows = pre2020,
            Pre2020NonZeroCpiFomc = pre2020NonZero,
            FirstDate = firstDate,
            LastDate = lastDate,
            KeptTypes = keptTypes.ToList()
        };
    }

    public static async Task<EventJoinResult> WriteEventFlagsAsync(
        Client client,
        string extractPath,
        string eventsPath,
        string typesPath,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken ct = default)
    {
        var fromDate = from ?? new DateOnly(2020, 1, 1);
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        Console.WriteLine($"Fetching US economic events {fromDate:yyyy-MM-dd} .. {toDate:yyyy-MM-dd}");
        var events = await FetchAllUsEventsAsync(client, fromDate, toDate, ct);
        Console.WriteLine($"Fetched {events.Count} US economic events (deduped).");
        WriteEventDump(eventsPath, typesPath, events);

        List<List<string>> grid;
        using (var reader = new StreamReader(extractPath))
        {
            grid = CsvReader.ReadAll(reader)
                ?? throw new InvalidOperationException($"Failed to read extract {extractPath}");
        }
        var before = grid.Count - 1;
        var result = ApplyFlagsToExtract(grid, events);
        if (result.DataRows != before)
        {
            throw new InvalidOperationException($"Extract row count changed: before={before} after={result.DataRows}");
        }
        using (var writer = new StreamWriter(extractPath, false, new UTF8Encoding(false)))
        {
            CsvWriter.WriteAll(grid, writer);
        }
        result.EventCount = events.Count;
        result.DistinctTypes = events.Select(e => e.Type ?? "").Where(t => t.Length > 0).Distinct(StringComparer.Ordinal).Count();
        Console.WriteLine($"Wrote flags onto {extractPath}: rows={result.DataRows} cpi={result.CpiFlags} fomc={result.FomcFlags} opex={result.OpexFlags}");
        return result;
    }

    static void StripExistingFlagColumns(List<List<string>> grid)
    {
        var header = grid[0];
        var drop = new HashSet<int>();
        for (int i = 0; i < header.Count; i++)
        {
            if (FlagColumns.Contains(header[i], StringComparer.OrdinalIgnoreCase))
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

    static int IndexOf(List<string> header, string name)
    {
        var i = header.FindIndex(h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase));
        if (i < 0)
        {
            throw new InvalidOperationException($"Extract is missing column {name}");
        }
        return i;
    }

    static double ParseDouble(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return double.NaN;
        }
        return double.Parse(s, CultureInfo.InvariantCulture);
    }

    public static string FormatNumber(double? value)
    {
        if (value is null)
        {
            return "";
        }
        return value.Value.ToString("G15", CultureInfo.InvariantCulture);
    }
}

public sealed class EventJoinResult
{
    public int DataRows { get; set; }
    public int CashSessions { get; set; }
    public int CpiFlags { get; set; }
    public int FomcFlags { get; set; }
    public int OpexFlags { get; set; }
    public int Pre2020Rows { get; set; }
    public int Pre2020NonZeroCpiFomc { get; set; }
    public DateOnly FirstDate { get; set; }
    public DateOnly LastDate { get; set; }
    public List<string> KeptTypes { get; set; } = [];
    public int EventCount { get; set; }
    public int DistinctTypes { get; set; }
}
