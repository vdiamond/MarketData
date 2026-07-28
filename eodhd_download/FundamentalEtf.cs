using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace MarketApi
{
    public class FundamentalEtf
    {
        [BsonId]
        public string ExchangeSymbolId {  get; set; }
        [JsonPropertyName("General")]
        public GeneralEtf GeneralEtf { get; set; }
        [JsonPropertyName("Technicals")]
        public TechnicalsEtf TechnicalsEtf { get; set; }
        [JsonPropertyName("ETF_Data")]
        public DataEtf DataEtf { get; set; }
    }
    public class GeneralEtf
    {
        [JsonPropertyName("Code")]
        public string Code { get; set; }
        [JsonPropertyName("Type")]
        public string Type { get; set; }
        [JsonPropertyName("Name")]
        public string Name { get; set; }
        [JsonPropertyName("Exchange")]
        public string Exchange { get; set; }
        [JsonPropertyName("CurrencyCode")]
        public string CurrencyCode { get; set; }
        [JsonPropertyName("CurrencyNamee")]
        public string CurrencyName { get; set; }
        [JsonPropertyName("CurrencySymbol")]
        public string CurrencySymbol { get; set; }
        [JsonPropertyName("CountryName")]
        public string CountryName { get; set; }
        [JsonPropertyName("CountryISO")]
        public string CountryISO { get; set; }
        [JsonPropertyName("OpenFigi")]
        public string OpenFigi { get; set; }
        [JsonPropertyName("Description")]
        public string Description { get; set; }
        [JsonPropertyName("Category")]
        public string Category { get; set; }
        [JsonPropertyName("UpdatedAt")]
        public string UpdatedAt { get; set; }
    }
    public class TechnicalsEtf
    {
        [JsonPropertyName("Beta")]
        public double Beta { get; set; }
        [JsonPropertyName("52WeekHigh")]
        public double High52Week { get; set; }
        [JsonPropertyName("52WeekLow")]
        public double Low52Week { get; set; }
        [JsonPropertyName("50DayMA")]
        public double Ma50Day { get; set; }
        [JsonPropertyName("200DayMA")]
        public double Ma200Day { get; set; }
    }
    public class DataEtf
    {
        [JsonPropertyName("ISIN")]
        public string ISIN { get; set; }
        [JsonPropertyName("Company_Name")]
        public string CompanyName { get; set; }
        [JsonPropertyName("Company_URL")]
        public string CompanyURL { get; set; }
        [JsonPropertyName("ETF_URL")]
        public string EtfURL { get; set; }
        [JsonPropertyName("Domicile")]
        public string Domicile { get; set; }
        [JsonPropertyName("Index_Name")]
        public object IndexName { get; set; }
        [JsonPropertyName("Yield")]
        public string Yield { get; set; }
        [JsonPropertyName("Dividend_Paying_Frequency")]
        public string DividendPayingFrequency { get; set; }
        [JsonPropertyName("Inception_Date")]
        public string InceptionDate { get; set; }
        [JsonPropertyName("Max_Annual_Mgmt_Charge")]
        public string MaxAnnualMgmtCharge { get; set; }
        [JsonPropertyName("Ongoing_Charge")]
        public string OngoingCharge { get; set; }
        [JsonPropertyName("Date_Ongoing_Charge")]
        public string DateOngoingCharge { get; set; }
        [JsonPropertyName("NetExpenseRatio")]
        public string NetExpenseRatio { get; set; }
        [JsonPropertyName("AnnualHoldingsTurnover")]
        public string AnnualHoldingsTurnover { get; set; }
        [JsonPropertyName("TotalAssets")]
        public string TotalAssets { get; set; }
        [JsonPropertyName("Average_Mkt_Cap_Mil")]
        public string AverageMktCapMil { get; set; }
        [JsonPropertyName("Market_Capitalisation")]
        public MarketCapitalisationEtf MarketCapitalisationEtf { get; set; }
        [JsonPropertyName("Asset_Allocation")]
        public Dictionary<string, AssetAllocationEtf> AssetAllocationEtf { get; set; }
        [JsonPropertyName("World_Regions")]
        public Dictionary<string, EquitySplitEtf> WorldRegionsEtf { get; set; }
        [JsonPropertyName("Sector_Weights")]
        public Dictionary<string, EquitySplitEtf> SectorWeightsEtf { get; set; }
        [JsonPropertyName("Fixed_Income")]
        public Dictionary<string, FundSplitEtf> FixedIncomeEtf { get; set; }
        [JsonPropertyName("Holdings_Count")]
        public int HoldingsCount { get; set; }
        [JsonPropertyName("Top_10_Holdings")]
        public Dictionary<string, HoldingEtf> Top10Holdings { get; set; }
        [JsonPropertyName("Holdings")]
        public Dictionary<string, HoldingEtf> HoldingsEtf { get; set; }
        [JsonPropertyName("Valuations_Growth")]
        public ValuationsGrowthEtf ValuationsGrowthEtf { get; set; }
        [JsonPropertyName("MorningStar")]
        public MorningstarEtf MorningStarEtf { get; set; }
        [JsonPropertyName("Performance")]
        public PerformanceEtf PerformanceEtf { get; set; }
    }
    public class MarketCapitalisationEtf
    {
        [JsonPropertyName("Mega")]
        public string Mega { get; set; }
        [JsonPropertyName("Big")]
        public string Big { get; set; }
        [JsonPropertyName("Medium")]
        public string Medium { get; set; }
        [JsonPropertyName("Small")]
        public string Small { get; set; }
        [JsonPropertyName("Micro")]
        public string Micro { get; set; }
    }
    public class AssetAllocationEtf
    {
        [JsonPropertyName("Long_%")]
        public string LongPerc { get; set; }
        [JsonPropertyName("Short_%")]
        public string ShortPerc { get; set; }
        [JsonPropertyName("Net_Assets_%")]
        public string NetAssetsPerc { get; set; }
    }
    public class EquitySplitEtf
    {
        [JsonPropertyName("Equity_%")]
        public string EquityPerc { get; set; }
        [JsonPropertyName("Relative_to_Category")]
        public string RelativeToCategory { get; set; }
    }
    public class FundSplitEtf
    {
        [JsonPropertyName("Fund_%")]
        public string FundPerc { get; set; }
        [JsonPropertyName("Relative_to_Category")]
        public string RelativeToCategory { get; set; }
    }
    public class HoldingEtf
    {
        [JsonPropertyName("Code")]
        public string Code { get; set; }
        [JsonPropertyName("Exchange")]
        public string Exchange { get; set; }
        [JsonPropertyName("Name")]
        public string Name { get; set; }
        [JsonPropertyName("Sector")]
        public string Sector { get; set; }
        [JsonPropertyName("Industry")]
        public string Industry { get; set; }
        [JsonPropertyName("Country")]
        public string Country { get; set; }
        [JsonPropertyName("Region")]
        public string Region { get; set; }
        [JsonPropertyName("Assets_%")]
        public double AssetsPerc { get; set; }
    }
    public class ValuationsGrowthEtf
    {
        [JsonPropertyName("Valuations_Rates_Portfolio")]
        public ValuationsRatesEtf ValuationsRatesPortfolio { get; set; }
        [JsonPropertyName("Valuations_Rates_To_Category")]
        public ValuationsRatesEtf ValuationsRatesToCategory { get; set; }
        [JsonPropertyName("Growth_Rates_Portfolio")]
        public GrowthRatesEtf GrowthRatesPortfolio { get; set; }
        [JsonPropertyName("Growth_Rates_To_Category")]
        public GrowthRatesEtf GrowthRatesToCategory { get; set; }
    }
    public class ValuationsRatesEtf
    {
        [JsonPropertyName("Price/Prospective Earnings")]
        public string PriceProspectiveEarnings { get; set; }
        [JsonPropertyName("Price/Book")]
        public string PriceBook { get; set; }
        [JsonPropertyName("Price/Sales")]
        public string PriceSales { get; set; }
        [JsonPropertyName("Price/Cash Flow")]
        public string PriceCashFlow { get; set; }
        [JsonPropertyName("Dividend-Yield Factor")]
        public string DividendYieldFactor { get; set; }
    }
    public class GrowthRatesEtf
    {
        [JsonPropertyName("Long-Term Projected Earnings Growth")]
        public string LongTermProjectedEarningsGrowth { get; set; }
        [JsonPropertyName("Historical Earnings Growth")]
        public string HistoricalEarningsGrowth { get; set; }
        [JsonPropertyName("Sales Growth")]
        public string SalesGrowth { get; set; }
        [JsonPropertyName("Cash-Flow Growth")]
        public string CashFlowGrowth { get; set; }
        [JsonPropertyName("Book-Value Growth")]
        public string BookValueGrowth { get; set; }
    }
    public class MorningstarEtf
    {
        [JsonPropertyName("Ratio")]
        public string Ratio { get; set; }
        [JsonPropertyName("Category_Benchmark")]
        public string CategoryBenchmark { get; set; }
        [JsonPropertyName("Sustainability_Ratio")]
        public string SustainabilityRatio { get; set; }
    }
    public class PerformanceEtf
    {
        [JsonPropertyName("1y_Volatility")]
        public string Volatility1Year { get; set; }
        [JsonPropertyName("3y_Volatility")]
        public string Volatility3Year { get; set; }
        [JsonPropertyName("3y_ExpReturn")]
        public string ExpReturn3Year { get; set; }
        [JsonPropertyName("3y_SharpRatio")]
        public string SharpRatio3Year { get; set; }
        [JsonPropertyName("Returns_YTD")]
        public string ReturnsYTD { get; set; }
        [JsonPropertyName("Returns_1Y")]
        public string Returns1Year { get; set; }
        [JsonPropertyName("Returns_3Y")]
        public string Returns3Year { get; set; }
        [JsonPropertyName("Returns_5Y")]
        public string Returns5Year { get; set; }
        [JsonPropertyName("Returns_10Y")]
        public string Returns10Year { get; set; }
    }
}
