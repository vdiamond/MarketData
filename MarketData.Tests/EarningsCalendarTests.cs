using eodhd_download;
using EodHdExtract;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarketData.Tests;

[TestClass]
public class EarningsCalendarTests
{
    static List<DateOnly> SampleCash()
    {
        return
        [
            new(2024, 3, 14),
            new(2024, 3, 15),
            new(2024, 3, 18)
        ];
    }

    [TestMethod]
    public void ClassifyTiming_BmoAmcDateOnlyAndClock()
    {
        Assert.AreEqual(EarningsTiming.DateOnly, EarningsCalendar.ClassifyTiming(null));
        Assert.AreEqual(EarningsTiming.DateOnly, EarningsCalendar.ClassifyTiming(""));
        Assert.AreEqual(EarningsTiming.BeforeMarket, EarningsCalendar.ClassifyTiming("BeforeMarket"));
        Assert.AreEqual(EarningsTiming.AfterMarket, EarningsCalendar.ClassifyTiming("AfterMarket"));
        Assert.AreEqual(EarningsTiming.BeforeMarket, EarningsCalendar.ClassifyTiming("BMO"));
        Assert.AreEqual(EarningsTiming.AfterMarket, EarningsCalendar.ClassifyTiming("amc"));
        Assert.AreEqual(EarningsTiming.BeforeMarket, EarningsCalendar.ClassifyTiming("08:00"));
        Assert.AreEqual(EarningsTiming.BeforeMarket, EarningsCalendar.ClassifyTiming("09:29"));
        Assert.AreEqual(EarningsTiming.DateOnly, EarningsCalendar.ClassifyTiming("09:30"));
        Assert.AreEqual(EarningsTiming.DateOnly, EarningsCalendar.ClassifyTiming("15:59"));
        Assert.AreEqual(EarningsTiming.AfterMarket, EarningsCalendar.ClassifyTiming("16:00"));
        Assert.AreEqual(EarningsTiming.AfterMarket, EarningsCalendar.ClassifyTiming("20:00"));
    }

    [TestMethod]
    public void Join_BmoSameDay_AmcNextSession()
    {
        var cash = SampleCash();
        Assert.AreEqual(
            new DateOnly(2024, 3, 15),
            EarningsCalendar.JoinReportToCashSession(new DateOnly(2024, 3, 15), EarningsTiming.BeforeMarket, cash));
        Assert.AreEqual(
            new DateOnly(2024, 3, 18),
            EarningsCalendar.JoinReportToCashSession(new DateOnly(2024, 3, 15), EarningsTiming.AfterMarket, cash));
        Assert.AreEqual(
            new DateOnly(2024, 3, 15),
            EarningsCalendar.JoinReportToCashSession(new DateOnly(2024, 3, 15), EarningsTiming.DateOnly, cash));
    }

    [TestMethod]
    public void Join_WeekendBmoUsesPriorCash_AmcUsesNext()
    {
        var cash = SampleCash();
        Assert.AreEqual(
            new DateOnly(2024, 3, 15),
            EarningsCalendar.JoinReportToCashSession(new DateOnly(2024, 3, 16), EarningsTiming.BeforeMarket, cash));
        Assert.AreEqual(
            new DateOnly(2024, 3, 18),
            EarningsCalendar.JoinReportToCashSession(new DateOnly(2024, 3, 16), EarningsTiming.AfterMarket, cash));
    }

    [TestMethod]
    public void ApplyFlags_BmoSameDay_AmcNext_DateOnly_NoTPlus1Leak()
    {
        var grid = new List<List<string>>
        {
            new() { "Date", "qqq.Open", "qqq.High", "qqq.Low", "qqq.Close", "qqq.Volume", "aapl.Close", "msft.Close", "nvda.Close", "evt_cpi" },
            new() { "2024-03-14", "1", "1", "1", "1", "1", "1", "1", "1", "0" },
            new() { "2024-03-15", "2", "2", "2", "2", "2", "2", "2", "2", "1" },
            new() { "2024-03-18", "3", "3", "3", "3", "3", "3", "3", "3", "0" },
            new() { "2024-03-19", "3", "3", "3", "3", "3", "3", "3", "3", "0" }
        };
        var reports = new List<EarningsCalendarItem>
        {
            new() { Code = "AAPL.US", ReportDate = "2024-03-15", Date = "2024-03-31", BeforeAfterMarket = "BeforeMarket", Actual = 1.5, Estimate = 1.4 },
            new() { Code = "MSFT.US", ReportDate = "2024-03-15", Date = "2024-03-31", BeforeAfterMarket = "AfterMarket", Actual = 2.1, Estimate = 2.0 },
            new() { Code = "NVDA.US", ReportDate = "2024-03-18", Date = "2024-04-30", BeforeAfterMarket = null },
            new() { Code = "REGN.US", ReportDate = "2024-03-15", Date = "2024-03-31", BeforeAfterMarket = "BeforeMarket" }
        };
        var universe = new[] { "AAPL.US", "MSFT.US", "NVDA.US" };
        var beforeCols = grid[0].Count;
        var result = EarningsCalendar.ApplyFlagsToExtract(grid, reports, universe);

        Assert.AreEqual(4, result.DataRows);
        Assert.AreEqual(3, result.CashSessions);
        Assert.AreEqual(2, result.EarnFlags);
        Assert.AreEqual(1, result.BmoFlags);
        Assert.AreEqual(1, result.AmcFlags);
        Assert.AreEqual(beforeCols + 4, grid[0].Count);
        Assert.AreEqual("evt_cpi", grid[0][9]);

        var header = grid[0];
        int earn = header.IndexOf("evt_earn");
        int bmo = header.IndexOf("evt_earn_bmo");
        int amc = header.IndexOf("evt_earn_amc");
        int n = header.IndexOf("evt_earn_n");

        Assert.AreEqual("0", grid[1][earn]);
        Assert.AreEqual("0", grid[1][bmo]);
        Assert.AreEqual("0", grid[1][amc]);
        Assert.AreEqual("0", grid[1][n]);

        Assert.AreEqual("1", grid[2][earn]);
        Assert.AreEqual("1", grid[2][bmo]);
        Assert.AreEqual("0", grid[2][amc]);
        Assert.AreEqual("1", grid[2][n]);
        Assert.AreEqual("1", grid[2][9]);

        Assert.AreEqual("1", grid[3][earn]);
        Assert.AreEqual("0", grid[3][bmo]);
        Assert.AreEqual("1", grid[3][amc]);
        Assert.AreEqual("2", grid[3][n]);

        Assert.AreEqual("0", grid[4][earn]);
        Assert.AreEqual("0", grid[4][bmo]);
        Assert.AreEqual("0", grid[4][amc]);
        Assert.AreEqual("0", grid[4][n]);
    }

    [TestMethod]
    public void ApplyFlags_AmcFridayDoesNotSetFriday()
    {
        var grid = new List<List<string>>
        {
            new() { "Date", "qqq.Open", "qqq.High", "qqq.Low", "qqq.Close", "qqq.Volume", "msft.Close" },
            new() { "2024-03-15", "1", "1", "1", "1", "1", "1" },
            new() { "2024-03-18", "2", "2", "2", "2", "2", "2" }
        };
        var reports = new List<EarningsCalendarItem>
        {
            new() { Code = "MSFT.US", ReportDate = "2024-03-15", BeforeAfterMarket = "AfterMarket" }
        };
        EarningsCalendar.ApplyFlagsToExtract(grid, reports, ["MSFT.US"]);
        int earn = grid[0].IndexOf("evt_earn");
        int amc = grid[0].IndexOf("evt_earn_amc");
        Assert.AreEqual("0", grid[1][earn]);
        Assert.AreEqual("0", grid[1][amc]);
        Assert.AreEqual("1", grid[2][earn]);
        Assert.AreEqual("1", grid[2][amc]);
    }

    [TestMethod]
    public void UniverseFromHeader_SkipsMissingClose()
    {
        var header = new List<string> { "Date", "aapl.Close", "msft.Close", "qqq.Close" };
        var present = EarningsCalendar.UniverseFromHeader(header, out var skipped);
        CollectionAssert.Contains(present, "AAPL.US");
        CollectionAssert.Contains(present, "MSFT.US");
        CollectionAssert.DoesNotContain(present, "REGN.US");
        CollectionAssert.DoesNotContain(present, "SNPS.US");
        CollectionAssert.Contains(skipped, "REGN");
        CollectionAssert.Contains(skipped, "SNPS");
        CollectionAssert.Contains(skipped, "NVDA");
    }

    [TestMethod]
    public void FilterUniverse_DropsCodesNotOnExtract()
    {
        var items = new List<EarningsCalendarItem>
        {
            new() { Code = "AAPL.US", ReportDate = "2024-03-15" },
            new() { Code = "REGN.US", ReportDate = "2024-03-15" }
        };
        var kept = EarningsCalendar.FilterUniverse(items, ["AAPL.US"]);
        Assert.AreEqual(1, kept.Count);
        Assert.AreEqual("AAPL.US", kept[0].Code);
    }
}
