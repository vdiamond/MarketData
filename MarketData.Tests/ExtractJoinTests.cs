using eodhd_download;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarketData.Tests;

[TestClass]
public class ExtractJoinTests
{
    [TestMethod]
    public void InsertIndex_PrefersLastMatchingAfterName()
    {
        var header = new List<string> { "Date", "vix.Volume", "tnx.Volume", "dxy.Open", "evt_cpi" };
        var at = ExtractJoin.InsertIndex(header, ["vix.Volume", "tnx.Volume"], EventCalendar.FlagColumns);
        Assert.AreEqual(3, at);
    }

    [TestMethod]
    public void InsertIndex_FallsBackToEventFlags()
    {
        var header = new List<string> { "Date", "qqq.Close", "evt_cpi", "evt_fomc" };
        var at = ExtractJoin.InsertIndex(header, ["tnx.Volume"], EventCalendar.FlagColumns);
        Assert.AreEqual(2, at);
    }
}
