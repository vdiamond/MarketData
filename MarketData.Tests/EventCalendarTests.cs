using eodhd_download;
using EodHdExtract;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarketData.Tests;

[TestClass]
public class EventCalendarTests
{
    [TestMethod]
    public void ThirdFriday_KnownMonths()
    {
        Assert.AreEqual(new DateOnly(2024, 3, 15), EventCalendar.ThirdFriday(2024, 3));
        Assert.AreEqual(new DateOnly(2025, 4, 18), EventCalendar.ThirdFriday(2025, 4));
        Assert.AreEqual(new DateOnly(2026, 8, 21), EventCalendar.ThirdFriday(2026, 8));
    }

    [TestMethod]
    public void Opex_ThirdFridayWhenCashSession()
    {
        var cash = new List<DateOnly>
        {
            new(2024, 3, 14),
            new(2024, 3, 15),
            new(2024, 3, 18)
        };
        var opex = EventCalendar.MonthlyOpexDates(new DateOnly(2024, 3, 1), new DateOnly(2024, 3, 31), cash);
        CollectionAssert.AreEqual(new List<DateOnly> { new(2024, 3, 15) }, opex);
    }

    [TestMethod]
    public void Opex_HolidayFridayFallsBackToPriorCashSession()
    {
        // 2025-04-18 is the third Friday and Good Friday — not a cash session.
        var cash = new List<DateOnly>
        {
            new(2025, 4, 16),
            new(2025, 4, 17),
            new(2025, 4, 21),
            new(2025, 4, 22)
        };
        var opex = EventCalendar.MonthlyOpexDates(new DateOnly(2025, 4, 1), new DateOnly(2025, 4, 30), cash);
        CollectionAssert.AreEqual(new List<DateOnly> { new(2025, 4, 17) }, opex);
    }

    [TestMethod]
    public void JoinDate_Cutoff1600_SameDayVsNextSession()
    {
        var cash = new List<DateOnly>
        {
            new(2024, 3, 14),
            new(2024, 3, 15),
            new(2024, 3, 18)
        };

        Assert.AreEqual(new DateOnly(2024, 3, 15), EventCalendar.JoinCashDate(new DateTime(2024, 3, 15, 15, 59, 0), cash));
        Assert.AreEqual(new DateOnly(2024, 3, 15), EventCalendar.JoinCashDate(new DateTime(2024, 3, 15, 16, 0, 0), cash));
        Assert.AreEqual(new DateOnly(2024, 3, 18), EventCalendar.JoinCashDate(new DateTime(2024, 3, 15, 16, 0, 1), cash));
        Assert.AreEqual(new DateOnly(2024, 3, 18), EventCalendar.JoinCashDate(new DateTime(2024, 3, 15, 20, 0, 0), cash));
    }

    [TestMethod]
    public void JoinDate_UtcPrintConvertedToEasternCutoff()
    {
        var cash = new List<DateOnly>
        {
            new(2024, 6, 11),
            new(2024, 6, 12),
            new(2024, 6, 13)
        };
        // 20:00 UTC in June is 16:00 EDT — on or before cutoff, same session.
        var atClose = EventCalendar.ParseEventToEastern("2024-06-12 20:00:00");
        Assert.AreEqual(new DateTime(2024, 6, 12, 16, 0, 0), atClose);
        Assert.AreEqual(new DateOnly(2024, 6, 12), EventCalendar.JoinCashDate(atClose, cash));

        var afterClose = EventCalendar.ParseEventToEastern("2024-06-12 20:00:01");
        Assert.AreEqual(new DateOnly(2024, 6, 13), EventCalendar.JoinCashDate(afterClose, cash));
    }

    [TestMethod]
    public void DuplicateQqqRow_IsNotCashSession()
    {
        var dates = new[] { "2026-08-21", "2026-08-24", "2026-08-25" };
        var qqq = new (double, double, double, double, double)[]
        {
            (560, 565, 558, 562, 10_000_000),
            (709.625, 709.79, 702.7, 706.32, 37_094_975),
            (709.625, 709.79, 702.7, 706.32, 37_094_975)
        };
        var cash = EventCalendar.CashSessionsFromQqq(dates, qqq);
        CollectionAssert.AreEqual(
            new List<DateOnly> { new(2026, 8, 21), new(2026, 8, 24) },
            cash);

        // 2026-08-25 10:00 ET is not a cash session; join prior cash 2026-08-24.
        Assert.AreEqual(
            new DateOnly(2026, 8, 24),
            EventCalendar.JoinCashDate(new DateTime(2026, 8, 25, 10, 0, 0), cash));
        // After 16:00 on the duplicate weekday, next cash session is none in this fixture.
        Assert.IsNull(EventCalendar.JoinCashDate(new DateTime(2026, 8, 25, 16, 0, 1), cash));
    }

    [TestMethod]
    public void ApplyFlags_LastEventByUtcWinsSurprise_Pre2020Empty()
    {
        var grid = new List<List<string>>
        {
            new() { "Date", "qqq.Open", "qqq.High", "qqq.Low", "qqq.Close", "qqq.Volume" },
            new() { "2019-12-20", "1", "1", "1", "1", "1" }, // third Friday Dec 2019
            new() { "2020-01-15", "2", "2", "2", "2", "2" },
            new() { "2020-01-16", "3", "3", "3", "3", "3" },
            new() { "2020-01-17", "3", "3", "3", "3", "3" } // duplicate of 01-16, not cash
        };
        var events = new List<EconomicEvent>
        {
            new()
            {
                Type = "CPI",
                Date = "2020-01-15 18:00:00", // 13:00 ET, same-day cash
                Actual = 2.1,
                Estimate = 2.0
            },
            new()
            {
                Type = "Core CPI",
                Date = "2020-01-15 18:30:00", // later same join date
                Actual = 2.4,
                Estimate = 2.2
            },
            new()
            {
                Type = "Fed Interest Rate Decision",
                Date = "2020-01-15 21:01:00", // 16:01 ET -> next cash 2020-01-16
                Actual = 1.75,
                Estimate = 1.50
            }
        };

        var result = EventCalendar.ApplyFlagsToExtract(grid, events);
        Assert.AreEqual(4, result.DataRows);
        Assert.AreEqual(1, result.CpiFlags);
        Assert.AreEqual(1, result.FomcFlags);
        Assert.AreEqual(0, result.Pre2020NonZeroCpiFomc);

        var header = grid[0];
        int cpi = header.IndexOf("evt_cpi");
        int fomc = header.IndexOf("evt_fomc");
        int opex = header.IndexOf("evt_opex");
        int cpiS = header.IndexOf("evt_cpi_surprise");
        int fomcS = header.IndexOf("evt_fomc_surprise");

        // 2019-12-20: OPEX only (third Friday), surprises empty, cpi/fomc 0
        Assert.AreEqual("0", grid[1][cpi]);
        Assert.AreEqual("0", grid[1][fomc]);
        Assert.AreEqual("1", grid[1][opex]);
        Assert.AreEqual("", grid[1][cpiS]);
        Assert.AreEqual("", grid[1][fomcS]);

        // 2020-01-15: CPI last-by-utc surprise 2.4-2.2=0.2; FOMC after cutoff not this day
        Assert.AreEqual("1", grid[2][cpi]);
        Assert.AreEqual("0", grid[2][fomc]);
        Assert.AreEqual(0.2, double.Parse(grid[2][cpiS], System.Globalization.CultureInfo.InvariantCulture), 1e-12);
        Assert.AreEqual("", grid[2][fomcS]);

        // 2020-01-16: FOMC from previous evening. Jan 2020 third Friday is the 17th
        // (not a cash session), so OPEX falls back to this prior cash session.
        Assert.AreEqual("0", grid[3][cpi]);
        Assert.AreEqual("1", grid[3][fomc]);
        Assert.AreEqual("1", grid[3][opex]);
        Assert.AreEqual("0.25", grid[3][fomcS]);

        Assert.AreEqual("0", grid[4][cpi]);
        Assert.AreEqual("0", grid[4][fomc]);
        Assert.AreEqual("0", grid[4][opex]);
        Assert.AreEqual("", grid[4][cpiS]);
        Assert.AreEqual("", grid[4][fomcS]);

        CollectionAssert.AreEquivalent(new[] { "CPI", "Core CPI", "Fed Interest Rate Decision" }, result.KeptTypes);
    }

    [TestMethod]
    public void FamilyOf_MatchesNeedlesCaseInsensitive()
    {
        Assert.AreEqual("cpi", EventCalendar.FamilyOf("United States CPI"));
        Assert.AreEqual("cpi", EventCalendar.FamilyOf("inflation rate YoY"));
        Assert.AreEqual("cpi", EventCalendar.FamilyOf("Core Inflation Rate"));
        Assert.AreEqual("cpi", EventCalendar.FamilyOf("PCE Price Index"));
        Assert.AreEqual("fomc", EventCalendar.FamilyOf("FOMC Statement"));
        Assert.AreEqual("fomc", EventCalendar.FamilyOf("Fed Interest Rate"));
        Assert.AreEqual("fomc", EventCalendar.FamilyOf("Interest Rate Decision"));
        Assert.AreEqual("fomc", EventCalendar.FamilyOf("Federal Funds Rate"));
        Assert.AreEqual("other", EventCalendar.FamilyOf("Nonfarm Payrolls"));
    }
}
