using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace MarketApi
{
    public class FundamentalStock
    {
        [BsonId]
        public string ExchangeSymbolId { get; set; }
        [JsonPropertyName("General")]
        public GeneralStock GeneralStock { get; set; }
        [JsonPropertyName("Highlights")]
        public HighlightsStock HighlightsStock { get; set; }
        [JsonPropertyName("Valuation")]
        public ValuationStock ValuationStock { get; set; }
        [JsonPropertyName("SharesStats")]
        public SharesstatsStock SharesStatsStock { get; set; }
        [JsonPropertyName("Technicals")]
        public TechnicalsStock TechnicalsStock { get; set; }
        [JsonPropertyName("SplitsDividends")]
        public SplitsDividendsStock SplitsDividendsStock { get; set; }
        [JsonPropertyName("AnalystRatings")]
        public AnalystRatingsStock AnalystRatingsStock { get; set; }
        [JsonPropertyName("Holders")]
        public HoldersStock HoldersStock { get; set; }
        [JsonPropertyName("InsiderTransactions")]
        public Dictionary<string, InsiderTransactionStock> InsiderTransactions { get; set; }
        [JsonPropertyName("ESGScores")]
        public ESGScoresStock ESGScoresStock { get; set; }
        [JsonPropertyName("outstandingShares")]
        public OutstandingSharesStock outstandingSharesStock { get; set; }
        [JsonPropertyName("Earnings")]
        public EarningsStock EarningsStock { get; set; }
        [JsonPropertyName("Financials")]
        public FinancialsStock FinancialsStock { get; set; }
    }
    public class GeneralStock
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
        [JsonPropertyName("CurrencyName")]
        public string CurrencyName { get; set; }
        [JsonPropertyName("CurrencySymbol")]
        public string CurrencySymbol { get; set; }
        [JsonPropertyName("CountryName")]
        public string CountryName { get; set; }
        [JsonPropertyName("CountryISO")]
        public string CountryISO { get; set; }
        [JsonPropertyName("OpenFigi")]
        public string OpenFigi { get; set; }
        [JsonPropertyName("ISIN")]
        public string ISIN { get; set; }
        [JsonPropertyName("LEI")]
        public string LEI { get; set; }
        [JsonPropertyName("PrimaryTicker")]
        public string PrimaryTicker { get; set; }
        [JsonPropertyName("CUSIP")]
        public string CUSIP { get; set; }
        [JsonPropertyName("CIK")]
        public string CIK { get; set; }
        [JsonPropertyName("EmployerIdNumber")]
        public string EmployerIdNumber { get; set; }
        [JsonPropertyName("FiscalYearEnd")]
        public string FiscalYearEnd { get; set; }
        [JsonPropertyName("IPODate")]
        public string IPODate { get; set; }
        [JsonPropertyName("InternationalDomestic")]
        public string InternationalDomestic { get; set; }
        [JsonPropertyName("Sector")]
        public string Sector { get; set; }
        [JsonPropertyName("Industry")]
        public string Industry { get; set; }
        [JsonPropertyName("GicSector")]
        public string GicSector { get; set; }
        [JsonPropertyName("GicGroup")]
        public string GicGroup { get; set; }
        [JsonPropertyName("GicIndustry")]
        public string GicIndustry { get; set; }
        [JsonPropertyName("GicSubIndustry")]
        public string GicSubIndustry { get; set; }
        [JsonPropertyName("HomeCategory")]
        public string HomeCategory { get; set; }
        [JsonPropertyName("IsDelisted")]
        public bool IsDelisted { get; set; }
        [JsonPropertyName("Description")]
        public string Description { get; set; }
        [JsonPropertyName("Address")]
        public string Address { get; set; }
        [JsonPropertyName("AddressData")]
        public AddressDataStock AddressDataStock { get; set; }
        [JsonPropertyName("Listings")]
        public Dictionary<string, ListingStock> Listings { get; set; }
        [JsonPropertyName("Officers")]
        public Dictionary<string, OfficerStock> OfficersStock { get; set; }
        [JsonPropertyName("Phone")]
        public string Phone { get; set; }
        [JsonPropertyName("WebURL")]
        public string WebURL { get; set; }
        [JsonPropertyName("LogoURL")]
        public string LogoURL { get; set; }
        [JsonPropertyName("FullTimeEmployees")]
        public int FullTimeEmployees { get; set; }
        [JsonPropertyName("UpdatedAt")]
        public string UpdatedAt { get; set; }
    }
    public class AddressDataStock
    {
        [JsonPropertyName("Street")]
        public string Street { get; set; }
        [JsonPropertyName("City")]
        public string City { get; set; }
        [JsonPropertyName("State")]
        public string State { get; set; }
        [JsonPropertyName("Country")]
        public string Country { get; set; }
        [JsonPropertyName("ZIP")]
        public string ZIP { get; set; }
    }
    public class ListingStock
    {
        [JsonPropertyName("Code")]
        public string Code { get; set; }
        [JsonPropertyName("Exchange")]
        public string Exchange { get; set; }
        [JsonPropertyName("Name")]
        public string Name { get; set; }
    }
    public class OfficerStock
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; }
        [JsonPropertyName("Title")]
        public string Title { get; set; }
        [JsonPropertyName("YearBorn")]
        public string YearBorn { get; set; }
    }
    public class HighlightsStock
    {
        [JsonPropertyName("MarketCapitalization")]
        public long? MarketCapitalization { get; set; }
        [JsonPropertyName("MarketCapitalizationMln")]
        public double? MarketCapitalizationMln { get; set; }
        [JsonPropertyName("EBITDA")]
        public long? EBITDA { get; set; }
        [JsonPropertyName("PERatio")]
        public double? PERatio { get; set; }
        [JsonPropertyName("PEGRatio")]
        public double? PEGRatio { get; set; }
        [JsonPropertyName("WallStreetTargetPrice")]
        public double? WallStreetTargetPrice { get; set; }
        [JsonPropertyName("BookValue")]
        public double? BookValue { get; set; }
        [JsonPropertyName("DividendShare")]
        public double? DividendShare { get; set; }
        [JsonPropertyName("DividendYield")]
        public double? DividendYield { get; set; }
        [JsonPropertyName("EarningsShare")]
        public double? EarningsShare { get; set; }
        [JsonPropertyName("EPSEstimateCurrentYear")]
        public double? EPSEstimateCurrentYear { get; set; }
        [JsonPropertyName("EPSEstimateNextYear")]
        public double? EPSEstimateNextYear { get; set; }
        [JsonPropertyName("EPSEstimateNextQuarter")]
        public double? EPSEstimateNextQuarter { get; set; }
        [JsonPropertyName("EPSEstimateCurrentQuarter")]
        public double? EPSEstimateCurrentQuarter { get; set; }
        [JsonPropertyName("MostRecentQuarter")]
        public string? MostRecentQuarter { get; set; }
        [JsonPropertyName("ProfitMargin")]
        public double? ProfitMargin { get; set; }
        [JsonPropertyName("OperatingMarginTTM")]
        public double? OperatingMarginTTM { get; set; }
        [JsonPropertyName("ReturnOnAssetsTTM")]
        public double? ReturnOnAssetsTTM { get; set; }
        [JsonPropertyName("ReturnOnEquityTTM")]
        public double? ReturnOnEquityTTM { get; set; }
        [JsonPropertyName("RevenueTTM")]
        public long? RevenueTTM { get; set; }
        [JsonPropertyName("RevenuePerShareTTM")]
        public double? RevenuePerShareTTM { get; set; }
        [JsonPropertyName("QuarterlyRevenueGrowthYOY")]
        public double? QuarterlyRevenueGrowthYOY { get; set; }
        [JsonPropertyName("GrossProfitTTM")]
        public long? GrossProfitTTM { get; set; }
        [JsonPropertyName("DilutedEpsTTM")]
        public double? DilutedEpsTTM { get; set; }
        [JsonPropertyName("QuarterlyEarningsGrowthYOY")]
        public double? QuarterlyEarningsGrowthYOY { get; set; }
    }
    public class ValuationStock
    {
        [JsonPropertyName("TrailingPE")]
        public double TrailingPE { get; set; }
        [JsonPropertyName("PriceSalesTTM")]
        public double PriceSalesTTM { get; set; }
        [JsonPropertyName("PriceBookMRQ")]
        public double PriceBookMRQ { get; set; }
        [JsonPropertyName("EnterpriseValue")]
        public long EnterpriseValue { get; set; }
        [JsonPropertyName("EnterpriseValueRevenue")]
        public double EnterpriseValueRevenue { get; set; }
        [JsonPropertyName("EnterpriseValueEbitda")]
        public double EnterpriseValueEbitda { get; set; }
    }
    public class SharesstatsStock
    {
        public long SharesOutstanding { get; set; }
        public long SharesFloat { get; set; }
        public double PercentInsiders { get; set; }
        public double PercentInstitutions { get; set; }
        public double? SharesShort { get; set; }
        public double? SharesShortPriorMonth { get; set; }
        public double? ShortRatio { get; set; }
        public double? ShortPercentOutstanding { get; set; }
        public double ShortPercentFloat { get; set; }
    }
    public class TechnicalsStock
    {
        [JsonPropertyName("Beta")]
        public double? Beta { get; set; }
        [JsonPropertyName("52WeekHigh")]
        public double High52Week { get; set; }
        [JsonPropertyName("52WeekLow")]
        public double Low52Week { get; set; }
        [JsonPropertyName("50DayMA")]
        public double Ma50Day { get; set; }
        [JsonPropertyName("200DayMA")]
        public double Ma200Day { get; set; }
        [JsonPropertyName("SharesShort")]
        public int SharesShort { get; set; }
        [JsonPropertyName("SharesShortPriorMonth")]
        public int SharesShortPriorMonth { get; set; }
        [JsonPropertyName("ShortRatio")]
        public double ShortRatio { get; set; }
        [JsonPropertyName("ShortPercent")]
        public double ShortPercent { get; set; }
    }
    public class SplitsDividendsStock
    {
        [JsonPropertyName("ForwardAnnualDividendRate")]
        public double ForwardAnnualDividendRate { get; set; }
        [JsonPropertyName("ForwardAnnualDividendYield")]
        public double ForwardAnnualDividendYield { get; set; }
        [JsonPropertyName("PayoutRatio")]
        public double PayoutRatio { get; set; }
        [JsonPropertyName("DividendDate")]
        public string DividendDate { get; set; }
        [JsonPropertyName("ExDividendDate")]
        public string ExDividendDate { get; set; }
        [JsonPropertyName("LastSplitFactor")]
        public string LastSplitFactor { get; set; }
        [JsonPropertyName("LastSplitDate")]
        public string LastSplitDate { get; set; }
        [JsonPropertyName("NumberDividendsByYear")]
        public Dictionary<string, NumberDividendsByYearStock> NumberDividendsByYearStock { get; set; }
    }
    public class NumberDividendsByYearStock
    {
        [JsonPropertyName("Year")]
        public int Year { get; set; }
        [JsonPropertyName("Count")]
        public int Count { get; set; }
    }
    public class AnalystRatingsStock
    {
        [JsonPropertyName("Rating")]
        public double Rating { get; set; }
        [JsonPropertyName("TargetPrice")]
        public double TargetPrice { get; set; }
        [JsonPropertyName("StrongBuy")]
        public int StrongBuy { get; set; }
        [JsonPropertyName("Buy")]
        public int Buy { get; set; }
        [JsonPropertyName("Hold")]
        public int Hold { get; set; }
        [JsonPropertyName("Sell")]
        public int Sell { get; set; }
        [JsonPropertyName("StringSell")]
        public int StrongSell { get; set; }
    }
    public class HoldersStock
    {
        [JsonPropertyName("Institutions")]
        public Dictionary<string, HolderStock> Institutions { get; set; }
        [JsonPropertyName("Funds")]
        public Dictionary<string, HolderStock> Funds { get; set; }
    }
    public class HolderStock
    {
        [JsonPropertyName("name")]
        public string name { get; set; }
        [JsonPropertyName("date")]
        public string date { get; set; }
        [JsonPropertyName("totalShares")]
        public double totalShares { get; set; }
        [JsonPropertyName("totalAssets")]
        public double totalAssets { get; set; }
        [JsonPropertyName("currentShares")]
        public long currentShares { get; set; }
        [JsonPropertyName("change")]
        public int change { get; set; }
        [JsonPropertyName("change_p")]
        public double changePerc { get; set; }
    }
    public class InsiderTransactionStock
    {
        [JsonPropertyName("date")]
        public string date { get; set; }
        [JsonPropertyName("ownerCik")]
        public double? ownerCik { get; set; }
        [JsonPropertyName("ownerName")]
        public string ownerName { get; set; }
        [JsonPropertyName("transactionDate")]
        public string transactionDate { get; set; }
        [JsonPropertyName("transactionCode")]
        public string transactionCode { get; set; }
        [JsonPropertyName("transactionAmount")]
        public double? transactionAmount { get; set; }
        [JsonPropertyName("transactionPrice")]
        public double transactionPrice { get; set; }
        [JsonPropertyName("transactionAcquiredDisposed")]
        public string transactionAcquiredDisposed { get; set; }
        [JsonPropertyName("postTransactionAmount")]
        public double? postTransactionAmount { get; set; }
        [JsonPropertyName("secLink")]
        public string? secLink { get; set; }
    }
    public class ESGScoresStock
    {
        [JsonPropertyName("Disclaimer")]
        public string Disclaimer { get; set; }
        [JsonPropertyName("RatingDate")]
        public string RatingDate { get; set; }
        [JsonPropertyName("TotalEsg")]
        public float TotalEsg { get; set; }
        [JsonPropertyName("TotalEsgPercentile")]
        public float TotalEsgPercentile { get; set; }
        [JsonPropertyName("EnvironmentScore")]
        public float EnvironmentScore { get; set; }
        [JsonPropertyName("EnvironmentScorePercentile")]
        public int EnvironmentScorePercentile { get; set; }
        [JsonPropertyName("SocialScore")]
        public float SocialScore { get; set; }
        [JsonPropertyName("SocialScorePercentile")]
        public int SocialScorePercentile { get; set; }
        [JsonPropertyName("GovernanceScore")]
        public float GovernanceScore { get; set; }
        [JsonPropertyName("GovernanceScorePercentile")]
        public int GovernanceScorePercentile { get; set; }
        [JsonPropertyName("ControversyLevel")]
        public int ControversyLevel { get; set; }
        [JsonPropertyName("ActivitiesInvolvement")]
        public Dictionary<string, ActivitiesInvolvementStock> ActivitiesInvolvements { get; set; }
    }
    public class ActivitiesInvolvementStock
    {
        [JsonPropertyName("Activity")]
        public string Activity { get; set; }
        [JsonPropertyName("Involvement")]
        public string Involvement { get; set; }
    }
    public class OutstandingSharesStock
    {
        [JsonPropertyName("annual")]
        public Dictionary<string, OutstandingShareStock> annual { get; set; }
        [JsonPropertyName("quarterly")]
        public Dictionary<string, OutstandingShareStock> quarterly { get; set; }
    }
    public class OutstandingShareStock
    {
        [JsonPropertyName("date")]
        public string date { get; set; }
        [JsonPropertyName("dateFormatted")]
        public string dateFormatted { get; set; }
        [JsonPropertyName("sharesMln")]
        public string sharesMln { get; set; }
        [JsonPropertyName("shares")]
        public double shares { get; set; }
    }
    public class EarningsStock
    {
        [JsonPropertyName("History")]
        public Dictionary<string, HistoryStock> History { get; set; }

        [JsonPropertyName("Trend")]
        public TrendStock TrendStock { get; set; }

        [JsonPropertyName("Annual")]
        public Dictionary<string, Annual2Stock> Annual { get; set; }
    }
    public class HistoryStock
    {
        [JsonPropertyName("reportDate")]
        public string reportDate { get; set; }
        [JsonPropertyName("date")]
        public string date { get; set; }
        [JsonPropertyName("beforeAfterMarket")]
        public string beforeAfterMarket { get; set; }
        [JsonPropertyName("currency")]
        public string currency { get; set; }
        [JsonPropertyName("epsActual")]
        public double? epsActual { get; set; }
        [JsonPropertyName("epsEstimate")]
        public double? epsEstimate { get; set; }
        [JsonPropertyName("epsDifference")]
        public double epsDifference { get; set; }
        [JsonPropertyName("surprisePercent")]
        public double? surprisePercent { get; set; }
    }
    public class TrendStock
    {
        [JsonPropertyName("Quarterly")]
        public Dictionary<string, Quarterly1Stock> Quarterly { get; set; }
        [JsonPropertyName("Annual")]
        public Dictionary<string, Annual1Stock> Annual { get; set; }
    }
    public class Quarterly1Stock
    {
        [JsonPropertyName("date")]
        public string date { get; set; }
        [JsonPropertyName("period")]
        public string period { get; set; }
        [JsonPropertyName("growth")]
        public string growth { get; set; }
        [JsonPropertyName("earningsEstimateAvg")]
        public string earningsEstimateAvg { get; set; }
        [JsonPropertyName("earningsEstimateLow")]
        public string earningsEstimateLow { get; set; }
        [JsonPropertyName("earningsEstimateHigh")]
        public string earningsEstimateHigh { get; set; }
        [JsonPropertyName("earningsEstimateYearAgoEps")]
        public string earningsEstimateYearAgoEps { get; set; }
        [JsonPropertyName("earningsEstimateNumberOfAnalysts")]
        public string earningsEstimateNumberOfAnalysts { get; set; }
        [JsonPropertyName("earningsEstimateGrowth")]
        public string earningsEstimateGrowth { get; set; }
        [JsonPropertyName("revenueEstimateAvg")]
        public string revenueEstimateAvg { get; set; }
        [JsonPropertyName("revenueEstimateLow")]
        public string revenueEstimateLow { get; set; }
        [JsonPropertyName("revenueEstimateHigh")]
        public string revenueEstimateHigh { get; set; }
        [JsonPropertyName("revenueEstimateYearAgoEps")]
        public string revenueEstimateYearAgoEps { get; set; }
        [JsonPropertyName("revenueEstimateNumberOfAnalysts")]
        public string revenueEstimateNumberOfAnalysts { get; set; }
        [JsonPropertyName("revenueEstimateGrowth")]
        public string revenueEstimateGrowth { get; set; }
        [JsonPropertyName("epsTrendCurrent")]
        public string epsTrendCurrent { get; set; }
        [JsonPropertyName("epsTrend7daysAgo")]
        public string epsTrend7daysAgo { get; set; }
        [JsonPropertyName("epsTrend30daysAgo")]
        public string epsTrend30daysAgo { get; set; }
        [JsonPropertyName("epsTrend60daysAgo")]
        public string epsTrend60daysAgo { get; set; }
        [JsonPropertyName("epsTrend90daysAgo")]
        public string epsTrend90daysAgo { get; set; }
        [JsonPropertyName("epsRevisionsUpLast7days")]
        public string epsRevisionsUpLast7days { get; set; }
        [JsonPropertyName("epsRevisionsUpLast30days")]
        public string epsRevisionsUpLast30days { get; set; }
        [JsonPropertyName("epsRevisionsDownLast7days")]
        public string epsRevisionsDownLast7days { get; set; }
        [JsonPropertyName("epsRevisionsDownLast30days")]
        public string epsRevisionsDownLast30days { get; set; }
        [JsonPropertyName("type")]
        public string type { get; set; }
        [JsonPropertyName("fiscalQuarter")]
        public string fiscalQuarter { get; set; }
    }
    public class Annual1Stock
    {
        [JsonPropertyName("date")]
        public string date { get; set; }
        [JsonPropertyName("period")]
        public string period { get; set; }
        [JsonPropertyName("growth")]
        public string growth { get; set; }
        [JsonPropertyName("earningsEstimateAvg")]
        public string earningsEstimateAvg { get; set; }
        [JsonPropertyName("earningsEstimateLow")]
        public string earningsEstimateLow { get; set; }
        [JsonPropertyName("earningsEstimateHigh")]
        public string earningsEstimateHigh { get; set; }
        [JsonPropertyName("earningsEstimateYearAgoEps")]
        public string earningsEstimateYearAgoEps { get; set; }
        [JsonPropertyName("earningsEstimateNumberOfAnalysts")]
        public string earningsEstimateNumberOfAnalysts { get; set; }
        [JsonPropertyName("earningsEstimateGrowth")]
        public string earningsEstimateGrowth { get; set; }
        [JsonPropertyName("revenueEstimateAvg")]
        public string revenueEstimateAvg { get; set; }
        [JsonPropertyName("revenueEstimateLow")]
        public string revenueEstimateLow { get; set; }
        [JsonPropertyName("revenueEstimateHigh")]
        public string revenueEstimateHigh { get; set; }
        [JsonPropertyName("revenueEstimateYearAgoEps")]
        public object revenueEstimateYearAgoEps { get; set; }
        [JsonPropertyName("revenueEstimateNumberOfAnalysts")]
        public string revenueEstimateNumberOfAnalysts { get; set; }
        [JsonPropertyName("revenueEstimateGrowth")]
        public string revenueEstimateGrowth { get; set; }
        [JsonPropertyName("epsTrendCurrent")]
        public string epsTrendCurrent { get; set; }
        [JsonPropertyName("epsTrend7daysAgo")]
        public string epsTrend7daysAgo { get; set; }
        [JsonPropertyName("epsTrend30daysAgo")]
        public string epsTrend30daysAgo { get; set; }
        [JsonPropertyName("epsTrend60daysAgo")]
        public string epsTrend60daysAgo { get; set; }
        [JsonPropertyName("epsTrend90daysAgo")]
        public string epsTrend90daysAgo { get; set; }
        [JsonPropertyName("epsRevisionsUpLast7days")]
        public string epsRevisionsUpLast7days { get; set; }
        [JsonPropertyName("epsRevisionsUpLast30days")]
        public string epsRevisionsUpLast30days { get; set; }
        [JsonPropertyName("epsRevisionsDownLast7days")]
        public object epsRevisionsDownLast7days { get; set; }
        [JsonPropertyName("epsRevisionsDownLast30days")]
        public string epsRevisionsDownLast30days { get; set; }
        [JsonPropertyName("type")]
        public string type { get; set; }
    }
    public class Annual2Stock
    {
        [JsonPropertyName("date")]
        public string date { get; set; }
        [JsonPropertyName("epsActaul")]
        public double epsActual { get; set; }
    }
    public class FinancialsStock
    {
        [JsonPropertyName("Balance_Sheet")]
        public BalanceSheetStock BalanceSheetStock { get; set; }
        [JsonPropertyName("Cash_Flow")]
        public CashFlowStock CashFlowStock { get; set; }
        [JsonPropertyName("Income_Statement")]
        public IncomeStatementStock IncomeStatementStock { get; set; }
    }
    public class BalanceSheetStock
    {
        [JsonPropertyName("currency_symbol")]
        public string CurrencySymbol { get; set; }
        [JsonPropertyName("quarterly")]
        public Dictionary<string, BalanceSheetInstance> quarterly { get; set; }
        [JsonPropertyName("yearly")]
        public Dictionary<string, BalanceSheetInstance> yearly { get; set; }
    }
    public class BalanceSheetInstance
    {
        [JsonPropertyName("date")]
        public string date { get; set; }
        [JsonPropertyName("filing_date")]
        public string filing_date { get; set; }
        [JsonPropertyName("currency_symbol")]
        public string currency_symbol { get; set; }
        [JsonPropertyName("totalAssets")]
        public string totalAssets { get; set; }
        [JsonPropertyName("intangibleAssets")]
        public string intangibleAssets { get; set; }
        [JsonPropertyName("earningAssets")]
        public string earningAssets { get; set; }
        [JsonPropertyName("otherCurrentAssets")]
        public string otherCurrentAssets { get; set; }
        [JsonPropertyName("totalLiab")]
        public string totalLiab { get; set; }
        [JsonPropertyName("totalStockholderEquity")]
        public string totalStockholderEquity { get; set; }
        [JsonPropertyName("deferredLongTermLiab")]
        public string deferredLongTermLiab { get; set; }
        [JsonPropertyName("otherCurrentLiab")]
        public string otherCurrentLiab { get; set; }
        [JsonPropertyName("commonStock")]
        public string commonStock { get; set; }
        [JsonPropertyName("capitalStock ")]
        public string capitalStock { get; set; }
        [JsonPropertyName("retainedEarnings")]
        public string retainedEarnings { get; set; }
        [JsonPropertyName("otherLiab")]
        public string otherLiab { get; set; }
        [JsonPropertyName("goodWill")]
        public string goodWill { get; set; }
        [JsonPropertyName("otherAssets")]
        public string otherAssets { get; set; }
        [JsonPropertyName("cash")]
        public string cash { get; set; }
        [JsonPropertyName("cashAndEquivalents")]
        public string cashAndEquivalents { get; set; }
        [JsonPropertyName("totalCurrentLiabilities")]
        public string totalCurrentLiabilities { get; set; }
        [JsonPropertyName("currentDeferredRevenue")]
        public string currentDeferredRevenue { get; set; }
        [JsonPropertyName("netDebt")]
        public string netDebt { get; set; }
        [JsonPropertyName("shortTermDebt")]
        public string shortTermDebt { get; set; }
        [JsonPropertyName("shortLongTermDebt")]
        public string shortLongTermDebt { get; set; }
        [JsonPropertyName("shortLongTermDebtTotal")]
        public string shortLongTermDebtTotal { get; set; }
        [JsonPropertyName("otherStockholderEquity")]
        public string otherStockholderEquity { get; set; }
        [JsonPropertyName("propertyPlantEquipment")]
        public string propertyPlantEquipment { get; set; }
        [JsonPropertyName("totalCurrentAssets")]
        public string totalCurrentAssets { get; set; }
        [JsonPropertyName("longTermInvestments")]
        public string longTermInvestments { get; set; }
        [JsonPropertyName("netTangibleAssets")]
        public string netTangibleAssets { get; set; }
        [JsonPropertyName("shortTermInvestments")]
        public string shortTermInvestments { get; set; }
        [JsonPropertyName("netReceivables")]
        public string netReceivables { get; set; }
        [JsonPropertyName("longTermDebt")]
        public string longTermDebt { get; set; }
        [JsonPropertyName("inventory")]
        public string inventory { get; set; }
        [JsonPropertyName("accountsPayable")]
        public string accountsPayable { get; set; }
        [JsonPropertyName("totalPermanentEquity")]
        public string totalPermanentEquity { get; set; }
        [JsonPropertyName("noncontrollingInterestInConsolidatedEntity")]
        public string noncontrollingInterestInConsolidatedEntity { get; set; }
        [JsonPropertyName("temporaryEquityRedeemableNoncontrollingInterests")]
        public string temporaryEquityRedeemableNoncontrollingInterests { get; set; }
        [JsonPropertyName("accumulatedOtherComprehensiveIncome")]
        public string accumulatedOtherComprehensiveIncome { get; set; }
        [JsonPropertyName("additionalPaidInCapital")]
        public string additionalPaidInCapital { get; set; }
        [JsonPropertyName("commonStockTotalEquity")]
        public string commonStockTotalEquity { get; set; }
        [JsonPropertyName("preferredStockTotalEquity")]
        public string preferredStockTotalEquity { get; set; }
        [JsonPropertyName("retainedEarningsTotalEquity")]
        public string retainedEarningsTotalEquity { get; set; }
        [JsonPropertyName("treasuryStock")]
        public string treasuryStock { get; set; }
        [JsonPropertyName("accumulatedAmortization")]
        public string accumulatedAmortization { get; set; }
        [JsonPropertyName("nonCurrrentAssetsOther")]
        public string nonCurrrentAssetsOther { get; set; }
        [JsonPropertyName("deferredLongTermAssetCharges")]
        public string deferredLongTermAssetCharges { get; set; }
        [JsonPropertyName("nonCurrentAssetsTotal")]
        public string nonCurrentAssetsTotal { get; set; }
        [JsonPropertyName("capitalLeaseObligations")]
        public string capitalLeaseObligations { get; set; }
        [JsonPropertyName("longTermDebtTotal")]
        public string longTermDebtTotal { get; set; }
        [JsonPropertyName("nonCurrentLiabilitiesOther")]
        public string nonCurrentLiabilitiesOther { get; set; }
        [JsonPropertyName("nonCurrentLiabilitiesTotal")]
        public string nonCurrentLiabilitiesTotal { get; set; }
        [JsonPropertyName("negativeGoodwill")]
        public string negativeGoodwill { get; set; }
        [JsonPropertyName("warrants")]
        public string warrants { get; set; }
        [JsonPropertyName("preferredStockRedeemable")]
        public string preferredStockRedeemable { get; set; }
        [JsonPropertyName("capitalSurpluse")]
        public string capitalSurpluse { get; set; }
        [JsonPropertyName("liabilitiesAndStockholdersEquity")]
        public string liabilitiesAndStockholdersEquity { get; set; }
        [JsonPropertyName("cashAndShortTermInvestments")]
        public string cashAndShortTermInvestments { get; set; }
        [JsonPropertyName("propertyPlantAndEquipmentGross")]
        public string propertyPlantAndEquipmentGross { get; set; }
        [JsonPropertyName("propertyPlantAndEquipmentNet")]
        public string propertyPlantAndEquipmentNet { get; set; }
        [JsonPropertyName("accumulatedDepreciation")]
        public string accumulatedDepreciation { get; set; }
        [JsonPropertyName("netWorkingCapital")]
        public string netWorkingCapital { get; set; }
        [JsonPropertyName("netInvestedCapital")]
        public string netInvestedCapital { get; set; }
        [JsonPropertyName("commonStockSharesOutstanding")]
        public string commonStockSharesOutstanding { get; set; }
    }
    public class CashFlowStock
    {
        [JsonPropertyName("currency_symbol")]
        public string CurrencySymbol { get; set; }
        [JsonPropertyName("quarterly")]
        public Dictionary<string, CashFlowInstance> quarterly { get; set; }
        [JsonPropertyName("yearly")]
        public Dictionary<string, CashFlowInstance> yearly { get; set; }
    }
    public class CashFlowInstance
    {
        [JsonPropertyName("date")]
        public string date { get; set; }
        [JsonPropertyName("filing_date")]
        public string filing_date { get; set; }
        [JsonPropertyName("currency_symbol")]
        public string currency_symbol { get; set; }
        [JsonPropertyName("investments")]
        public string investments { get; set; }
        [JsonPropertyName("changeToLiabilities")]
        public string changeToLiabilities { get; set; }
        [JsonPropertyName("totalCashflowsFromInvestingActivities")]
        public string totalCashflowsFromInvestingActivities { get; set; }
        [JsonPropertyName("netBorrowings")]
        public string netBorrowings { get; set; }
        [JsonPropertyName("totalCashFromFinancingActivities")]
        public string totalCashFromFinancingActivities { get; set; }
        [JsonPropertyName("changeToOperatingActivities")]
        public string changeToOperatingActivities { get; set; }
        [JsonPropertyName("netIncome")]
        public string netIncome { get; set; }
        [JsonPropertyName("changeInCash")]
        public string changeInCash { get; set; }
        [JsonPropertyName("beginPeriodCashFlow")]
        public string beginPeriodCashFlow { get; set; }
        [JsonPropertyName("endPeriodCashFlow")]
        public string endPeriodCashFlow { get; set; }
        [JsonPropertyName("totalCashFromOperatingActivities")]
        public string totalCashFromOperatingActivities { get; set; }
        [JsonPropertyName("issuanceOfCapitalStock")]
        public string issuanceOfCapitalStock { get; set; }
        [JsonPropertyName("depreciation")]
        public string depreciation { get; set; }
        [JsonPropertyName("otherCashflowsFromInvestingActivities")]
        public string otherCashflowsFromInvestingActivities { get; set; }
        [JsonPropertyName("dividendsPaid")]
        public string dividendsPaid { get; set; }
        [JsonPropertyName("changeToInventory")]
        public string changeToInventory { get; set; }
        [JsonPropertyName("changeToAccountReceivables")]
        public string changeToAccountReceivables { get; set; }
        [JsonPropertyName("salePurchaseOfStock")]
        public string salePurchaseOfStock { get; set; }
        [JsonPropertyName("otherCashflowsFromFinancingActivities")]
        public string otherCashflowsFromFinancingActivities { get; set; }
        [JsonPropertyName("changeToNetincome")]
        public string changeToNetincome { get; set; }
        [JsonPropertyName("capitalExpenditures")]
        public string capitalExpenditures { get; set; }
        [JsonPropertyName("changeReceivables")]
        public string changeReceivables { get; set; }
        [JsonPropertyName("cashFlowsOtherOperating")]
        public string cashFlowsOtherOperating { get; set; }
        [JsonPropertyName("exchangeRateChanges")]
        public string exchangeRateChanges { get; set; }
        [JsonPropertyName("cashAndCashEquivalentsChanges")]
        public string cashAndCashEquivalentsChanges { get; set; }
        [JsonPropertyName("changeInWorkingCapital")]
        public string changeInWorkingCapital { get; set; }
        [JsonPropertyName("stockBasedCompensation")]
        public string stockBasedCompensation { get; set; }
        [JsonPropertyName("otherNonCashItems")]
        public string otherNonCashItems { get; set; }
        [JsonPropertyName("freeCashFlow")]
        public string freeCashFlow { get; set; }
    }
    public class IncomeStatementStock
    {
        [JsonPropertyName("currency_symbol")]
        public string CurrencySymbol { get; set; }
        [JsonPropertyName("quarterly")]
        public Dictionary<string, IncomeStatementInstance> quarterly { get; set; }
        [JsonPropertyName("yearly")]
        public Dictionary<string, IncomeStatementInstance> yearly { get; set; }
    }
    public class IncomeStatementInstance
    {
        [JsonPropertyName("date")]
        public string date { get; set; }
        [JsonPropertyName("filing_date")]
        public string filing_date { get; set; }
        [JsonPropertyName("currency_symbol")]
        public string currency_symbol { get; set; }
        [JsonPropertyName("researchDevelopment")]
        public string researchDevelopment { get; set; }
        [JsonPropertyName("effectOfAccountingCharges")]
        public string effectOfAccountingCharges { get; set; }
        [JsonPropertyName("incomeBeforeTax")]
        public string incomeBeforeTax { get; set; }
        [JsonPropertyName("minorityInterest")]
        public string minorityInterest { get; set; }
        [JsonPropertyName("netIncome")]
        public string netIncome { get; set; }
        [JsonPropertyName("sellingGeneralAdministrative")]
        public string sellingGeneralAdministrative { get; set; }
        [JsonPropertyName("sellingAndMarketingExpenses")]
        public string sellingAndMarketingExpenses { get; set; }
        [JsonPropertyName("grossProfit")]
        public string grossProfit { get; set; }
        [JsonPropertyName("reconciledDepreciation")]
        public string reconciledDepreciation { get; set; }
        [JsonPropertyName("ebit")]
        public string ebit { get; set; }
        [JsonPropertyName("ebitda")]
        public string ebitda { get; set; }
        [JsonPropertyName("depreciationAndAmortization")]
        public string depreciationAndAmortization { get; set; }
        [JsonPropertyName("nonOperatingIncomeNetOther")]
        public string nonOperatingIncomeNetOther { get; set; }
        [JsonPropertyName("operatingIncome")]
        public string operatingIncome { get; set; }
        [JsonPropertyName("otherOperatingExpenses")]
        public string otherOperatingExpenses { get; set; }
        [JsonPropertyName("interestExpense")]
        public string interestExpense { get; set; }
        [JsonPropertyName("taxProvision")]
        public string taxProvision { get; set; }
        [JsonPropertyName("interestIncome")]
        public string interestIncome { get; set; }
        [JsonPropertyName("netInterestIncome")]
        public string netInterestIncome { get; set; }
        [JsonPropertyName("extraordinaryItems")]
        public string extraordinaryItems { get; set; }
        [JsonPropertyName("nonRecurring")]
        public string nonRecurring { get; set; }
        [JsonPropertyName("otherItems")]
        public string otherItems { get; set; }
        [JsonPropertyName("incomeTaxExpense")]
        public string incomeTaxExpense { get; set; }
        [JsonPropertyName("totalRevenue")]
        public string totalRevenue { get; set; }
        [JsonPropertyName("totalOperatingExpenses")]
        public string totalOperatingExpenses { get; set; }
        [JsonPropertyName("costOfRevenue")]
        public string costOfRevenue { get; set; }
        [JsonPropertyName("totalOtherIncomeExpenseNet")]
        public string totalOtherIncomeExpenseNet { get; set; }
        [JsonPropertyName("discontinuedOperations")]
        public string discontinuedOperations { get; set; }
        [JsonPropertyName("netIncomeFromContinuingOps")]
        public string netIncomeFromContinuingOps { get; set; }
        [JsonPropertyName("netIncomeApplicableToCommonShares")]
        public string netIncomeApplicableToCommonShares { get; set; }
        [JsonPropertyName("preferredStockAndOtherAdjustments")]
        public string preferredStockAndOtherAdjustments { get; set; }
    }
}
