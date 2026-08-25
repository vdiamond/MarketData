using eodhd_download;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarketData.Tests;

[TestClass]
public class MacroIndicatorsTests
{
    [TestMethod]
    public void PeriodKnownFrom_Dec31_IsNextJanuary15()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 15), MacroIndicators.PeriodKnownFrom(new DateOnly(2024, 12, 31)));
        Assert.AreEqual(new DateOnly(2024, 1, 15), MacroIndicators.PeriodKnownFrom(new DateOnly(2023, 12, 31)));
    }

    [TestMethod]
    public void KnownAsOf_Dec31_UsesJan15WhenCashSession()
    {
        var cash = new List<DateOnly>
        {
            new(2025, 1, 14),
            new(2025, 1, 15),
            new(2025, 1, 16)
        };
        Assert.AreEqual(
            new DateOnly(2025, 1, 15),
            MacroIndicators.KnownAsOfCashSession(new DateOnly(2024, 12, 31), cash));
    }

    [TestMethod]
    public void KnownAsOf_Jan15NotCash_UsesNextCashSession()
    {
        // 2023-01-15 was Sunday; first cash on/after is Monday 2023-01-16.
        var cash = new List<DateOnly>
        {
            new(2023, 1, 13),
            new(2023, 1, 16),
            new(2023, 1, 17)
        };
        Assert.AreEqual(
            new DateOnly(2023, 1, 16),
            MacroIndicators.KnownAsOfCashSession(new DateOnly(2022, 12, 31), cash));
    }

    [TestMethod]
    public void JoinAnnualPrints_EmptyUntilKnownThenForwardFill()
    {
        var extractDates = new List<DateOnly>
        {
            new(2024, 12, 31),
            new(2025, 1, 14),
            new(2025, 1, 15),
            new(2025, 1, 16),
            new(2025, 1, 17)
        };
        var cash = new List<DateOnly>
        {
            new(2024, 12, 31),
            new(2025, 1, 14),
            new(2025, 1, 15),
            new(2025, 1, 16)
            // 2025-01-17 is a duplicate extract row, not cash
        };
        var prints = new List<(DateOnly, double)>
        {
            (new DateOnly(2023, 12, 31), 3.2),
            (new DateOnly(2024, 12, 31), 2.9)
        };

        var values = MacroIndicators.JoinAnnualPrints(extractDates, cash, prints);

        Assert.AreEqual(3.2, values[0]);
        Assert.AreEqual(3.2, values[1]);
        Assert.AreEqual(2.9, values[2]);
        Assert.AreEqual(2.9, values[3]);
        Assert.AreEqual(2.9, values[4]);
    }

    [TestMethod]
    public void JoinAnnualPrints_LeadingEmptyBeforeFirstKnown()
    {
        var extractDates = new List<DateOnly>
        {
            new(2010, 1, 14),
            new(2010, 1, 15),
            new(2010, 1, 18)
        };
        var cash = new List<DateOnly>
        {
            new(2010, 1, 14),
            new(2010, 1, 15),
            new(2010, 1, 18)
        };
        var prints = new List<(DateOnly, double)>
        {
            (new DateOnly(2009, 12, 31), 2.7)
        };

        var values = MacroIndicators.JoinAnnualPrints(extractDates, cash, prints);

        Assert.IsTrue(double.IsNaN(values[0]));
        Assert.AreEqual(2.7, values[1]);
        Assert.AreEqual(2.7, values[2]);
    }

    [TestMethod]
    public void JoinAnnualPrints_DoesNotLeakOnHolidayJan15()
    {
        // 2024-01-15 MLK — weekday extract copy, not a cash session.
        // First cash on/after 2024-01-15 is 2024-01-16.
        var extractDates = new List<DateOnly>
        {
            new(2024, 1, 12),
            new(2024, 1, 15),
            new(2024, 1, 16)
        };
        var cash = new List<DateOnly>
        {
            new(2024, 1, 12),
            new(2024, 1, 16)
        };
        var prints = new List<(DateOnly, double)>
        {
            (new DateOnly(2022, 12, 31), 8.0),
            (new DateOnly(2023, 12, 31), 4.1)
        };

        var values = MacroIndicators.JoinAnnualPrints(extractDates, cash, prints);

        Assert.AreEqual(8.0, values[0]);
        Assert.AreEqual(8.0, values[1]);
        Assert.AreEqual(4.1, values[2]);
    }
}
