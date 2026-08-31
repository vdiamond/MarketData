using System.Reflection;
using System.Text.Json.Serialization;
using EodHdExtract;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarketData.Tests;

[TestClass]
public class EodHdExtractContractTests
{
    [TestMethod]
    public void Assembly_DoesNotReferenceLiteDB()
    {
        var referenced = typeof(Client).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        CollectionAssert.DoesNotContain(referenced, "LiteDB");
    }

    [TestMethod]
    public void ClientReturnTypes_HaveClassNameIdAndJsonName()
    {
        AssertClassNameId(new Exchange { Code = "US" }, "US");
        AssertClassNameId(new ExchangeSymbol { Symbol = "QQQ", Exchange = "US" }, "QQQ.US");
        AssertClassNameId(new FundamentalStock { FundamentalStockId = "AAPL.US" }, "AAPL.US");
        AssertClassNameId(new FundamentalEtf { FundamentalEtfId = "QQQ.US" }, "QQQ.US");
        AssertClassNameId(new FundamentalIndex { FundamentalIndexId = "GSPC.INDX" }, "GSPC.INDX");
        AssertClassNameId(
            new HistoricalStockPrice { ExchangeSymbolId = "QQQ.US", Date = "2026-08-31" },
            "QQQ.US.2026-08-31");
        AssertClassNameId(
            new SentimentsData { ExchangeSymbolId = "QQQ.US", Date = "2026-08-31" },
            "QQQ.US.2026-08-31");
        AssertClassNameId(
            new MacroIndicator { CountryCode = "USA", Indicator = "real_interest_rate", Date = "2024-12-31" },
            "USA.real_interest_rate.2024-12-31");
        AssertClassNameId(
            new EconomicEvent { Country = "US", Type = "CPI", Date = "2026-01-15 13:30:00", Comparison = "YoY", Period = "Dec" },
            "US.CPI.2026-01-15 13:30:00.YoY.Dec");
        AssertClassNameId(
            new EarningsCalendarResponse { From = "2026-01-01", To = "2026-01-31", Symbols = "AAPL.US" },
            "2026-01-01.2026-01-31.AAPL.US");
        AssertClassNameId(
            new EarningsCalendarItem { Code = "AAPL.US", ReportDate = "2026-01-30", Date = "2026-03-31", Period = "Q1" },
            "AAPL.US.2026-01-30.2026-03-31.Q1");
        AssertClassNameId(new IntradayBar { Timestamp = 1700000000 }, "1700000000");
    }

    #region private

    static void AssertClassNameId<T>(T instance, string expected)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var type = typeof(T);
        var name = $"{type.Name}Id";
        var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(prop, $"{type.Name} is missing public {name}.");
        var json = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
        Assert.IsNotNull(json, $"{type.Name}.{name} is missing [JsonPropertyName].");
        Assert.AreEqual(name, json.Name);
        Assert.AreEqual(expected, prop.GetValue(instance));
    }

    #endregion
}
