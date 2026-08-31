using EodHdExtract;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestClient;

namespace MarketData.Tests;

[TestClass]
public class EodFormatterTests
{
    [TestMethod]
    public void ToString_MapsPeriodOrderAndDateOnly()
    {
        IFormatter formatter = new EodFormatter();

        Assert.AreEqual("d", formatter.ToString(Client.Period.Day));
        Assert.AreEqual("w", formatter.ToString(Client.Period.Week));
        Assert.AreEqual("m", formatter.ToString(Client.Period.Month));
        Assert.AreEqual("a", formatter.ToString(Client.Order.Ascending));
        Assert.AreEqual("d", formatter.ToString(Client.Order.Descending));
        Assert.AreEqual("2026-08-25", formatter.ToString(new DateOnly(2026, 8, 25)));
    }
}
