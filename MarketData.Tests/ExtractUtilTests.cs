using eodhd_download;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Table;

namespace MarketData.Tests;

[TestClass]
public class ExtractUtilTests
{
    [TestMethod]
    public void FillIn_KeepsRealZero_FillsNaNFromPrior()
    {
        var values = new List<double> { 1.5, double.NaN, 0.0, double.NaN, 2.0 };
        ExtractUtil.FillIn(values);

        Assert.AreEqual(1.5, values[0]);
        Assert.AreEqual(1.5, values[1]);
        Assert.AreEqual(0.0, values[2]);
        Assert.AreEqual(0.0, values[3]);
        Assert.AreEqual(2.0, values[4]);
    }

    [TestMethod]
    public void FillIn_LeadingMissing_StaysNaN()
    {
        var values = new List<double> { double.NaN, double.NaN, 4.0 };
        ExtractUtil.FillIn(values);

        Assert.IsTrue(double.IsNaN(values[0]));
        Assert.IsTrue(double.IsNaN(values[1]));
        Assert.AreEqual(4.0, values[2]);
    }

    [TestMethod]
    public void FillInTable_OnlyFillsDoubleColumns()
    {
        var table = new DynamicTable();
        table.columns.NewColumn("Date", TypeCode.String);
        table.columns.NewColumn("Px", TypeCode.Double);
        var r0 = table.rows.NewRow();
        r0.SetField("Date", "2026-01-02");
        r0.SetField("Px", 10.0);
        var r1 = table.rows.NewRow();
        r1.SetField("Date", "2026-01-05");
        r1.SetField("Px", double.NaN);
        var r2 = table.rows.NewRow();
        r2.SetField("Date", "2026-01-06");
        r2.SetField("Px", 0.0);

        ExtractUtil.FillInTable<double>(table);

        Assert.AreEqual("2026-01-05", table.rows[1].GetField<string>("Date"));
        Assert.AreEqual(10.0, table.rows[1].GetField<double>("Px"));
        Assert.AreEqual(0.0, table.rows[2].GetField<double>("Px"));
    }

    [TestMethod]
    public void ColumnPrefixFromExchangeSymbolId_ReturnsLowerSymbol()
    {
        Assert.AreEqual("qqq", ExtractUtil.ColumnPrefixFromExchangeSymbolId("QQQ.US"));
    }

    [TestMethod]
    public void ColumnPrefixFromExchangeSymbolId_RejectsBadFormat()
    {
        var ex = Assert.ThrowsException<Exception>(() => ExtractUtil.ColumnPrefixFromExchangeSymbolId("QQQ"));
        StringAssert.Contains(ex.Message, "QQQ");
    }

    [TestMethod]
    public void RecodeCountFloorToZero_ReplacesFloorOnMatchingOhlcOnly()
    {
        var table = new DynamicTable();
        table.columns.NewColumn("mahn.Open", TypeCode.Double);
        table.columns.NewColumn("mahn.Volume", TypeCode.Double);
        table.columns.NewColumn("trin.Close", TypeCode.Double);
        table.columns.NewColumn("maln.Close", TypeCode.Double);
        var r0 = table.rows.NewRow();
        r0.SetField("mahn.Open", 0.01);
        r0.SetField("mahn.Volume", 0.01);
        r0.SetField("trin.Close", 0.01);
        r0.SetField("maln.Close", 0.01);
        var r1 = table.rows.NewRow();
        r1.SetField("mahn.Open", 12.0);
        r1.SetField("mahn.Volume", 0.0);
        r1.SetField("trin.Close", 1.5);
        r1.SetField("maln.Close", 0.0);

        ExtractUtil.RecodeCountFloorToZero(table, ["mahn", "maln"]);

        Assert.AreEqual(0.0, table.rows[0].GetField<double>("mahn.Open"));
        Assert.AreEqual(0.01, table.rows[0].GetField<double>("mahn.Volume"));
        Assert.AreEqual(0.01, table.rows[0].GetField<double>("trin.Close"));
        Assert.AreEqual(0.0, table.rows[0].GetField<double>("maln.Close"));
        Assert.AreEqual(12.0, table.rows[1].GetField<double>("mahn.Open"));
        Assert.AreEqual(0.0, table.rows[1].GetField<double>("maln.Close"));
    }

    [TestMethod]
    public void RecodeCountFloorToZero_LeavesNaNUnchanged()
    {
        var table = new DynamicTable();
        table.columns.NewColumn("mahn.Close", TypeCode.Double);
        var r0 = table.rows.NewRow();
        r0.SetField("mahn.Close", double.NaN);
        var r1 = table.rows.NewRow();
        r1.SetField("mahn.Close", 0.01);

        ExtractUtil.RecodeCountFloorToZero(table, ["mahn"]);

        Assert.IsTrue(double.IsNaN(table.rows[0].GetField<double>("mahn.Close")));
        Assert.AreEqual(0.0, table.rows[1].GetField<double>("mahn.Close"));
    }
}