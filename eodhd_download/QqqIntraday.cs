using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using EodHdExtract;
using TableCsv;

namespace eodhd_download;

/// <summary>
/// QQQ 1-hour EODHD dump and regular-hours first/last hour join onto extract Date.
/// Does not FillIn missing hours. Does not join overnight T close to T+1 open.
/// </summary>
public static class QqqIntraday
{
    public const string Symbol = "QQQ.US";
    public const string Interval = "1h";
    public const string H1Column = "qqq_h1";
    public const string HLastColumn = "qqq_hlast";
    public const string H1VolColumn = "qqq_h1_vol";
    public const string HLastVolColumn = "qqq_hlast_vol";

    public static readonly string[] ExtractColumns =
    [
        H1Column,
        HLastColumn,
        H1VolColumn,
        HLastVolColumn
    ];

    public static readonly DateOnly DefaultFrom = new(2015, 11, 23);
    public static readonly DateOnly HistoryTooShortAfter = new(2022, 12, 31);
    public static readonly TimeOnly RegularOpen = new(9, 30);
    public static readonly TimeOnly RegularClose = new(16, 0);
    public static readonly TimeOnly LastHourOpen = new(15, 0);
    public const int MaxWindowDays = 7200;

    public static DateTime BarUtc(IntradayBar bar)
    {
        ArgumentNullException.ThrowIfNull(bar);
        if (bar.Timestamp != 0)
        {
            return DateTimeOffset.FromUnixTimeSeconds(bar.Timestamp).UtcDateTime;
        }
        if (!string.IsNullOrWhiteSpace(bar.Datetime)
            && DateTime.TryParseExact(
                bar.Datetime.Trim(),
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }
        throw new InvalidOperationException("Intraday bar is missing timestamp and datetime.");
    }

    public static DateTime BarEastern(IntradayBar bar)
    {
        var utc = DateTime.SpecifyKind(BarUtc(bar), DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, EventCalendar.EasternTimeZone);
    }

    public static bool IsRegularHours(TimeOnly etOpen)
    {
        return etOpen >= RegularOpen && etOpen < RegularClose;
    }

    public static bool IsRegularHours(IntradayBar bar)
    {
        var et = BarEastern(bar);
        return IsRegularHours(TimeOnly.FromDateTime(et));
    }

    public static double? HourReturn(IntradayBar bar)
    {
        ArgumentNullException.ThrowIfNull(bar);
        if (bar.Open is null || bar.Close is null || bar.Open.Value == 0
            || double.IsNaN(bar.Open.Value) || double.IsNaN(bar.Close.Value))
        {
            return null;
        }
        return bar.Close.Value / bar.Open.Value - 1.0;
    }

    public static (IntradayBar? First, IntradayBar? Last) FirstAndLastRth(IReadOnlyList<IntradayBar> sessionBars)
    {
        ArgumentNullException.ThrowIfNull(sessionBars);
        IntradayBar? firstPreferred = null;
        IntradayBar? firstEarliest = null;
        DateTime firstEarliestEt = default;
        IntradayBar? lastPreferred = null;
        IntradayBar? lastLatest = null;
        DateTime lastLatestEt = default;

        foreach (var bar in sessionBars)
        {
            if (!IsRegularHours(bar))
            {
                continue;
            }
            var et = BarEastern(bar);
            var time = TimeOnly.FromDateTime(et);
            if (time == RegularOpen)
            {
                firstPreferred = bar;
            }
            if (firstEarliest is null || et < firstEarliestEt)
            {
                firstEarliest = bar;
                firstEarliestEt = et;
            }
            if (time == LastHourOpen)
            {
                lastPreferred = bar;
            }
            if (lastLatest is null || et > lastLatestEt)
            {
                lastLatest = bar;
                lastLatestEt = et;
            }
        }

        return (firstPreferred ?? firstEarliest, lastPreferred ?? lastLatest);
    }

    public static Dictionary<DateOnly, (IntradayBar? First, IntradayBar? Last)> SessionFirstLast(
        IReadOnlyList<IntradayBar> bars)
    {
        ArgumentNullException.ThrowIfNull(bars);
        var byDate = new Dictionary<DateOnly, List<IntradayBar>>();
        foreach (var bar in bars)
        {
            var et = BarEastern(bar);
            var date = DateOnly.FromDateTime(et);
            if (!byDate.TryGetValue(date, out var list))
            {
                list = [];
                byDate[date] = list;
            }
            list.Add(bar);
        }

        var result = new Dictionary<DateOnly, (IntradayBar? First, IntradayBar? Last)>(byDate.Count);
        foreach (var kv in byDate)
        {
            result[kv.Key] = FirstAndLastRth(kv.Value);
        }
        return result;
    }

    public static List<IntradayBar> SortBars(IEnumerable<IntradayBar> bars)
    {
        ArgumentNullException.ThrowIfNull(bars);
        return bars
            .OrderBy(b => b.Timestamp != 0 ? b.Timestamp : new DateTimeOffset(BarUtc(b), TimeSpan.Zero).ToUnixTimeSeconds())
            .ToList();
    }

    public static DateOnly UnixUtcDate(long unix)
    {
        return DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime);
    }

    public static long ToUnixUtc(DateOnly date, TimeOnly time)
    {
        var utc = new DateTimeOffset(date.ToDateTime(time), TimeSpan.Zero);
        return utc.ToUnixTimeSeconds();
    }

    public static async Task<QqqIntradayFetchResult> FetchAsync(
        Client client,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (to < from)
        {
            throw new ArgumentOutOfRangeException(nameof(to), "to must be on or after from.");
        }

        var result = new QqqIntradayFetchResult
        {
            From = from,
            To = to
        };

        List<IntradayBar> bars;
        try
        {
            bars = await FetchWindowsAsync(client, from, to, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            result.Forbidden = true;
            result.StatusNote = "403 Forbidden: Intraday 1h is not on the current EODHD plan. Stopped. Did not join. Did not scrape.";
            return result;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("Intraday API returned HTTP 401 (bad token).", ex);
        }

        bars = SortBars(bars);
        result.Bars = bars;
        result.DumpRows = bars.Count;
        if (bars.Count == 0)
        {
            result.HistoryTooShort = true;
            result.StatusNote = "Intraday 1h returned empty. Treated as too-short. Stopped before join.";
            return result;
        }

        var firstEt = BarEastern(bars[0]);
        var lastEt = BarEastern(bars[^1]);
        result.FirstBarUtc = BarUtc(bars[0]);
        result.FirstBarEt = firstEt;
        result.LastBarUtc = BarUtc(bars[^1]);
        result.LastBarEt = lastEt;
        result.HistoryStartEt = DateOnly.FromDateTime(firstEt);
        if (result.HistoryStartEt > HistoryTooShortAfter)
        {
            result.HistoryTooShort = true;
            result.StatusNote =
                $"History starts {result.HistoryStartEt:yyyy-MM-dd} ET (after 2022-12-31). Dump rows={bars.Count}. Stopped before join.";
        }
        return result;
    }

    public static void WriteDump(string path, IReadOnlyList<IntradayBar> bars)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(bars);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        var csv = new CsvWriter(writer);
        csv.WriteRow(["datetime_utc", "datetime_et", "open", "high", "low", "close", "volume"]);
        foreach (var bar in bars)
        {
            var utc = BarUtc(bar);
            var et = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), EventCalendar.EasternTimeZone);
            csv.WriteRow(
            [
                utc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                et.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                FormatOptional(bar.Open),
                FormatOptional(bar.High),
                FormatOptional(bar.Low),
                FormatOptional(bar.Close),
                FormatOptional(bar.Volume)
            ]);
        }
    }

    public static QqqIntradayJoinResult ApplyToExtract(List<List<string>> grid, IReadOnlyList<IntradayBar> bars)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(bars);
        if (grid.Count < 2)
        {
            throw new InvalidOperationException("Extract has no data rows.");
        }

        var header = grid[0];
        int dateIdx = ExtractJoin.IndexOf(header, "Date");
        var sessions = SessionFirstLast(bars);

        var h1 = new string[grid.Count - 1];
        var hLast = new string[grid.Count - 1];
        var h1Vol = new string[grid.Count - 1];
        var hLastVol = new string[grid.Count - 1];
        int firstNonempty = 0;
        int lastNonempty = 0;

        for (int r = 1; r < grid.Count; r++)
        {
            var i = r - 1;
            var date = DateOnly.Parse(grid[r][dateIdx], CultureInfo.InvariantCulture);
            if (!sessions.TryGetValue(date, out var pair))
            {
                h1[i] = "";
                hLast[i] = "";
                h1Vol[i] = "";
                hLastVol[i] = "";
                continue;
            }

            var firstRet = pair.First is null ? null : HourReturn(pair.First);
            var lastRet = pair.Last is null ? null : HourReturn(pair.Last);
            h1[i] = FormatOptional(firstRet);
            hLast[i] = FormatOptional(lastRet);
            h1Vol[i] = pair.First is null ? "" : FormatOptional(pair.First.Volume);
            hLastVol[i] = pair.Last is null ? "" : FormatOptional(pair.Last.Volume);
            if (h1[i].Length > 0)
            {
                firstNonempty++;
            }
            if (hLast[i].Length > 0)
            {
                lastNonempty++;
            }
        }

        var insertAt = ExtractJoin.InsertIndex(grid[0], ["qqq.Volume"], EventCalendar.FlagColumns);
        ExtractJoin.AddOrReplaceStringColumns(
            grid,
            ExtractColumns,
            [h1, hLast, h1Vol, hLastVol],
            insertAt);

        var firstDate = DateOnly.Parse(grid[1][dateIdx], CultureInfo.InvariantCulture);
        var lastDate = DateOnly.Parse(grid[^1][dateIdx], CultureInfo.InvariantCulture);
        return new QqqIntradayJoinResult
        {
            DataRows = grid.Count - 1,
            FirstHourNonempty = firstNonempty,
            LastHourNonempty = lastNonempty,
            FirstDate = firstDate,
            LastDate = lastDate
        };
    }

    public static async Task<QqqIntradayRunResult> WriteAsync(
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

        var fromDate = from ?? DefaultFrom;
        DateOnly toDate;
        if (to is not null)
        {
            toDate = to.Value;
        }
        else
        {
            toDate = ExtractEndDate(extractPath) ?? DateOnly.FromDateTime(DateTime.UtcNow);
        }

        Console.WriteLine($"Fetching QQQ.US 1h {fromDate:yyyy-MM-dd} .. {toDate:yyyy-MM-dd}");
        var fetch = await FetchAsync(client, fromDate, toDate, ct);
        var run = new QqqIntradayRunResult
        {
            Forbidden = fetch.Forbidden,
            HistoryTooShort = fetch.HistoryTooShort,
            StatusNote = fetch.StatusNote,
            DumpRows = fetch.DumpRows,
            HistoryStartEt = fetch.HistoryStartEt,
            FirstBarUtc = fetch.FirstBarUtc,
            FirstBarEt = fetch.FirstBarEt,
            LastBarUtc = fetch.LastBarUtc,
            LastBarEt = fetch.LastBarEt,
            From = fetch.From,
            To = fetch.To
        };

        if (fetch.Forbidden)
        {
            Console.WriteLine(fetch.StatusNote);
            return run;
        }

        WriteDump(dumpPath, fetch.Bars);
        Console.WriteLine($"Wrote dump {dumpPath} rows={fetch.DumpRows} first_et={fetch.HistoryStartEt:yyyy-MM-dd}");

        if (fetch.HistoryTooShort)
        {
            Console.WriteLine(fetch.StatusNote);
            return run;
        }

        List<List<string>> grid;
        using (var reader = new StreamReader(extractPath))
        {
            grid = CsvReader.ReadAll(reader)
                ?? throw new InvalidOperationException($"Failed to read extract {extractPath}");
        }
        var before = grid.Count - 1;
        var join = ApplyToExtract(grid, fetch.Bars);
        if (join.DataRows != before)
        {
            throw new InvalidOperationException($"Extract row count changed: before={before} after={join.DataRows}");
        }
        using (var writer = new StreamWriter(extractPath, false, new UTF8Encoding(false)))
        {
            CsvWriter.WriteAll(grid, writer);
        }

        run.Joined = true;
        run.ExtractRows = join.DataRows;
        run.FirstHourNonempty = join.FirstHourNonempty;
        run.LastHourNonempty = join.LastHourNonempty;
        Console.WriteLine(
            $"Joined qqq_h1/qqq_hlast onto {extractPath}; rows={join.DataRows} h1={join.FirstHourNonempty} hlast={join.LastHourNonempty}");
        return run;
    }

    public static string FormatOptional(double? value)
    {
        if (value is null || double.IsNaN(value.Value))
        {
            return "";
        }
        return FormatNumber(value.Value);
    }

    public static string FormatNumber(double value)
    {
        return value.ToString("G15", CultureInfo.InvariantCulture);
    }

    public static IntradayBar Bar(DateTime utc, double open, double high, double low, double close, double volume)
    {
        var utcKind = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        var unix = new DateTimeOffset(utcKind).ToUnixTimeSeconds();
        return new IntradayBar
        {
            Timestamp = unix,
            GmtOffset = 0,
            Datetime = utcKind.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = volume
        };
    }

    #region private

    static DateOnly? ExtractEndDate(string extractPath)
    {
        if (!File.Exists(extractPath))
        {
            return null;
        }
        using var reader = new StreamReader(extractPath);
        var grid = CsvReader.ReadAll(reader);
        if (grid is null || grid.Count < 2)
        {
            return null;
        }
        var dateIdx = ExtractJoin.IndexOf(grid[0], "Date");
        return DateOnly.Parse(grid[^1][dateIdx], CultureInfo.InvariantCulture);
    }

    static async Task<List<IntradayBar>> FetchWindowsAsync(
        Client client,
        DateOnly from,
        DateOnly to,
        CancellationToken ct)
    {
        var spanDays = to.DayNumber - from.DayNumber;
        if (spanDays <= MaxWindowDays)
        {
            try
            {
                return await FetchWindowAsync(client, from, to, ct);
            }
            catch (HttpRequestException ex) when (
                ex.StatusCode != HttpStatusCode.Forbidden
                && ex.StatusCode != HttpStatusCode.Unauthorized)
            {
                Console.WriteLine($"Single 1h window failed ({ex.StatusCode}); retrying by year.");
            }
        }

        var all = new List<IntradayBar>();
        for (int year = from.Year; year <= to.Year; year++)
        {
            var windowFrom = year == from.Year ? from : new DateOnly(year, 1, 1);
            var windowTo = year == to.Year ? to : new DateOnly(year, 12, 31);
            var chunk = await FetchWindowAsync(client, windowFrom, windowTo, ct);
            all.AddRange(chunk);
            Console.WriteLine($"Fetched 1h {windowFrom:yyyy-MM-dd}..{windowTo:yyyy-MM-dd} bars={chunk.Count}");
        }
        return all;
    }

    static async Task<List<IntradayBar>> FetchWindowAsync(
        Client client,
        DateOnly from,
        DateOnly to,
        CancellationToken ct)
    {
        var fromUnix = ToUnixUtc(from, TimeOnly.MinValue);
        var toUnix = ToUnixUtc(to, new TimeOnly(23, 59, 59));
        return await client.GetIntradayAsync(Symbol, Interval, fromUnix, toUnix, ct);
    }

    #endregion
}

public sealed class QqqIntradayFetchResult
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public List<IntradayBar> Bars { get; set; } = [];
    public int DumpRows { get; set; }
    public bool Forbidden { get; set; }
    public bool HistoryTooShort { get; set; }
    public string? StatusNote { get; set; }
    public DateOnly? HistoryStartEt { get; set; }
    public DateTime? FirstBarUtc { get; set; }
    public DateTime? FirstBarEt { get; set; }
    public DateTime? LastBarUtc { get; set; }
    public DateTime? LastBarEt { get; set; }
}

public sealed class QqqIntradayJoinResult
{
    public int DataRows { get; set; }
    public int FirstHourNonempty { get; set; }
    public int LastHourNonempty { get; set; }
    public DateOnly FirstDate { get; set; }
    public DateOnly LastDate { get; set; }
}

public sealed class QqqIntradayRunResult
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public bool Forbidden { get; set; }
    public bool HistoryTooShort { get; set; }
    public bool Joined { get; set; }
    public string? StatusNote { get; set; }
    public int DumpRows { get; set; }
    public int ExtractRows { get; set; }
    public int FirstHourNonempty { get; set; }
    public int LastHourNonempty { get; set; }
    public DateOnly? HistoryStartEt { get; set; }
    public DateTime? FirstBarUtc { get; set; }
    public DateTime? FirstBarEt { get; set; }
    public DateTime? LastBarUtc { get; set; }
    public DateTime? LastBarEt { get; set; }
}
