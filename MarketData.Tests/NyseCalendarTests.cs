using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarketData.Tests;

[TestClass]
public class NyseCalendarTests
{
    [TestMethod]
    public void KnownHolidays_AreDetected()
    {
        Assert.IsTrue(eodhd_transform.NyseCalendar.IsFullSessionHoliday(new DateOnly(2024, 1, 1)));
        Assert.IsTrue(eodhd_transform.NyseCalendar.IsFullSessionHoliday(new DateOnly(2024, 3, 29)));
        Assert.IsTrue(eodhd_transform.NyseCalendar.IsFullSessionHoliday(new DateOnly(2024, 5, 27)));
        Assert.IsTrue(eodhd_transform.NyseCalendar.IsFullSessionHoliday(new DateOnly(2024, 6, 19)));
        Assert.IsTrue(eodhd_transform.NyseCalendar.IsFullSessionHoliday(new DateOnly(2024, 7, 4)));
        Assert.IsTrue(eodhd_transform.NyseCalendar.IsFullSessionHoliday(new DateOnly(2024, 9, 2)));
        Assert.IsTrue(eodhd_transform.NyseCalendar.IsFullSessionHoliday(new DateOnly(2024, 11, 28)));
        Assert.IsTrue(eodhd_transform.NyseCalendar.IsFullSessionHoliday(new DateOnly(2024, 12, 25)));
        Assert.IsTrue(eodhd_transform.NyseCalendar.IsFullSessionHoliday(new DateOnly(2023, 1, 2)));
        Assert.IsTrue(eodhd_transform.NyseCalendar.IsFullSessionHoliday(new DateOnly(2022, 6, 20)));
        Assert.IsTrue(eodhd_transform.NyseCalendar.IsFullSessionHoliday(new DateOnly(2021, 12, 31)));
        Assert.IsTrue(eodhd_transform.NyseCalendar.IsFullSessionHoliday(new DateOnly(2018, 12, 5)));
        Assert.IsTrue(eodhd_transform.NyseCalendar.IsFullSessionHoliday(new DateOnly(2025, 1, 9)));
    }

    [TestMethod]
    public void RegularWeekday_IsNotHoliday()
    {
        Assert.IsFalse(eodhd_transform.NyseCalendar.IsFullSessionHoliday(new DateOnly(2024, 7, 3)));
        Assert.IsFalse(eodhd_transform.NyseCalendar.IsFullSessionHoliday(new DateOnly(2021, 6, 18)));
    }
}
