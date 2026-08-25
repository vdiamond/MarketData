using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarketData.Tests;

[TestClass]
public class TransformContractTests
{
    [TestMethod]
    public void OutputColumns_MatchQqqFeatureContract()
    {
        string[] expected =
        [
            "Date",
            "qqq_open",
            "qqq_high",
            "qqq_low",
            "qqq_close",
            "qqq_vol",
            "qqq_ema9",
            "qqq_ema20",
            "qqq_ema50",
            "qqq_slope9",
            "qqq_slope20",
            "qqq_slope50",
            "qqq_rsi14",
            "qqq_adx14",
            "qqq_volatility20",
            "qqq_macd_hist26_12_9",
            "qqq_macd_signal26_12_9",
            "qqq_bollinger_width20_2",
            "qqq_bollinger_pos20_2",
            "qqq_support",
            "qqq_support_strength",
            "qqq_resistance",
            "qqq_resistance_strength",
            "qqq_yield",
            "qqq_yield_la1"
        ];

        CollectionAssert.AreEqual(expected, eodhd_transform.Program.OutputColumns);
    }
}
