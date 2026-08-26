using eodhd_download;
using MarketApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarketData.Tests;

[TestClass]
public class QqqIntradayTests
{
    static IntradayBar UtcBar(string utc, double open, double close, double volume = 1000)
    {
        var dt = DateTime.ParseExact(utc, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        var high = Math.Max(open, close);
        var low = Math.Min(open, close);
        return QqqIntraday.Bar(dt, open, high, low, close, volume);
    }

    [TestMethod]
    public void Dst_WinterUtcIsEst_SummerUtcIsEdt()
    {
        var winter = UtcBar("2024-01-02 14:30:00", 100, 101);
        var winterEt = QqqIntraday.BarEastern(winter);
        Assert.AreEqual(new DateTime(2024, 1, 2, 9, 30, 0), winterEt);
        Assert.IsTrue(QqqIntraday.IsRegularHours(winter));

        var summer = UtcBar("2024-07-02 13:30:00", 100, 101);
        var summerEt = QqqIntraday.BarEastern(summer);
        Assert.AreEqual(new DateTime(2024, 7, 2, 9, 30, 0), summerEt);
        Assert.IsTrue(QqqIntraday.IsRegularHours(summer));

        var spring = UtcBar("2024-03-11 13:30:00", 100, 101);
        Assert.AreEqual(new DateTime(2024, 3, 11, 9, 30, 0), QqqIntraday.BarEastern(spring));

        var fall = UtcBar("2024-11-04 14:30:00", 100, 101);
        Assert.AreEqual(new DateTime(2024, 11, 4, 9, 30, 0), QqqIntraday.BarEastern(fall));
    }

    [TestMethod]
    public void RthFilter_PremarketAndAfterHoursExcluded()
    {
        var bars = new List<IntradayBar>
        {
            UtcBar("2024-07-02 12:00:00", 99, 99.5, 10),   // 08:00 EDT premarket
            UtcBar("2024-07-02 13:30:00", 100, 101, 500),  // 09:30 first
            UtcBar("2024-07-02 19:00:00", 110, 111, 400),  // 15:00 last preferred
            UtcBar("2024-07-02 20:00:00", 111, 112, 300),  // 16:00 not in [09:30, 16:00)
            UtcBar("2024-07-02 21:00:00", 112, 113, 200)   // 17:00 after hours
        };
        var (first, last) = QqqIntraday.FirstAndLastRth(bars);
        Assert.IsNotNull(first);
        Assert.IsNotNull(last);
        Assert.AreEqual(100, first.Open);
        Assert.AreEqual(500, first.Volume);
        Assert.AreEqual(110, last.Open);
        Assert.AreEqual(400, last.Volume);
        Assert.AreEqual(0.01, QqqIntraday.HourReturn(first)!.Value, 1e-12);
        Assert.AreEqual(111.0 / 110.0 - 1.0, QqqIntraday.HourReturn(last)!.Value, 1e-12);
    }

    [TestMethod]
    public void MissingPreferredHour_UsesEarliestAndLatestInSession()
    {
        var bars = new List<IntradayBar>
        {
            UtcBar("2024-07-02 14:30:00", 102, 103, 50),  // 10:30 EDT
            UtcBar("2024-07-02 18:30:00", 108, 107, 60)   // 14:30 EDT
        };
        var (first, last) = QqqIntraday.FirstAndLastRth(bars);
        Assert.IsNotNull(first);
        Assert.IsNotNull(last);
        Assert.AreEqual(102, first.Open);
        Assert.AreEqual(108, last.Open);
    }

    [TestMethod]
    public void MissingInSession_EmptyFirstAndLast()
    {
        var bars = new List<IntradayBar>
        {
            UtcBar("2024-07-02 12:00:00", 99, 100, 1),  // 08:00 pre
            UtcBar("2024-07-02 20:30:00", 112, 113, 2)  // 16:30 after
        };
        var (first, last) = QqqIntraday.FirstAndLastRth(bars);
        Assert.IsNull(first);
        Assert.IsNull(last);
    }

    [TestMethod]
    public void NoOvernightLeak_TCloseNotJoinedToTPlus1Open()
    {
        var tLast = UtcBar("2024-07-01 19:30:00", 200, 202, 800);   // 15:30 EDT Jul 1
        var tAh = UtcBar("2024-07-01 21:00:00", 202, 199, 50);      // 17:00 after hours Jul 1
        var t1Pre = UtcBar("2024-07-02 08:00:00", 198, 199, 40);    // 04:00 pre Jul 2
        var t1First = UtcBar("2024-07-02 13:30:00", 210, 211, 900); // 09:30 Jul 2
        var t1Last = UtcBar("2024-07-02 19:00:00", 212, 213, 700);  // 15:00 Jul 2
        var sessions = QqqIntraday.SessionFirstLast([tLast, tAh, t1Pre, t1First, t1Last]);

        var d1 = new DateOnly(2024, 7, 1);
        var d2 = new DateOnly(2024, 7, 2);
        Assert.IsTrue(sessions.ContainsKey(d1));
        Assert.IsTrue(sessions.ContainsKey(d2));
        Assert.AreEqual(200, sessions[d1].Last!.Open);
        Assert.AreEqual(800, sessions[d1].Last!.Volume);
        Assert.AreEqual(210, sessions[d2].First!.Open);
        Assert.AreEqual(900, sessions[d2].First!.Volume);

        var overnight = t1First.Open!.Value / tLast.Close!.Value - 1.0;
        var h1 = QqqIntraday.HourReturn(sessions[d2].First!)!.Value;
        Assert.AreNotEqual(overnight, h1, 1e-12);
        Assert.AreEqual(211.0 / 210.0 - 1.0, h1, 1e-12);
    }

    [TestMethod]
    public void ApplyToExtract_EmptyIfMissingHour_PreservesRowsAndEarnColumns()
    {
        var grid = new List<List<string>>
        {
            new() { "Date", "qqq.Open", "qqq.High", "qqq.Low", "qqq.Close", "qqq.Volume", "evt_cpi", "evt_earn" },
            new() { "2024-07-01", "1", "1", "1", "1", "1", "0", "1" },
            new() { "2024-07-02", "2", "2", "2", "2", "2", "0", "0" },
            new() { "2024-07-03", "3", "3", "3", "3", "3", "1", "0" }
        };
        var bars = new List<IntradayBar>
        {
            UtcBar("2024-07-01 13:30:00", 100, 101, 10),
            UtcBar("2024-07-01 19:00:00", 110, 111, 20),
            UtcBar("2024-07-02 12:00:00", 90, 91, 5)
        };
        var beforeRows = grid.Count - 1;
        var beforeEarn = grid[1][7];
        var result = QqqIntraday.ApplyToExtract(grid, bars);

        Assert.AreEqual(3, result.DataRows);
        Assert.AreEqual(beforeRows, grid.Count - 1);
        Assert.AreEqual(1, result.FirstHourNonempty);
        Assert.AreEqual(1, result.LastHourNonempty);

        var header = grid[0];
        int h1 = header.IndexOf("qqq_h1");
        int hLast = header.IndexOf("qqq_hlast");
        int h1Vol = header.IndexOf("qqq_h1_vol");
        int hLastVol = header.IndexOf("qqq_hlast_vol");
        int earn = header.IndexOf("evt_earn");
        int vol = header.IndexOf("qqq.Volume");
        Assert.IsTrue(h1 > vol);
        Assert.IsTrue(earn > hLastVol);
        Assert.AreEqual("evt_earn", grid[0][earn]);
        Assert.AreEqual(beforeEarn, grid[1][earn]);

        Assert.AreEqual((101.0 / 100.0 - 1.0).ToString("G15", System.Globalization.CultureInfo.InvariantCulture), grid[1][h1]);
        Assert.AreEqual((111.0 / 110.0 - 1.0).ToString("G15", System.Globalization.CultureInfo.InvariantCulture), grid[1][hLast]);
        Assert.AreEqual("10", grid[1][h1Vol]);
        Assert.AreEqual("20", grid[1][hLastVol]);

        Assert.AreEqual("", grid[2][h1]);
        Assert.AreEqual("", grid[2][hLast]);
        Assert.AreEqual("", grid[2][h1Vol]);
        Assert.AreEqual("", grid[2][hLastVol]);
        Assert.AreEqual("", grid[3][h1]);
        Assert.AreEqual("", grid[3][hLast]);
    }

    [TestMethod]
    public void PreferredFifteenHundredBeatsLaterInSessionBar()
    {
        var bars = new List<IntradayBar>
        {
            UtcBar("2024-07-02 13:30:00", 100, 101, 1),
            UtcBar("2024-07-02 19:00:00", 110, 109, 2),  // 15:00
            UtcBar("2024-07-02 19:30:00", 109, 108, 3)   // 15:30 still in session
        };
        var (first, last) = QqqIntraday.FirstAndLastRth(bars);
        Assert.AreEqual(100, first!.Open);
        Assert.AreEqual(110, last!.Open);
        Assert.AreEqual(2, last.Volume);
    }
}
