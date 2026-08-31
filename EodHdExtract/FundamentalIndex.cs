using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace EodHdExtract
{
    public class FundamentalIndex
    {
        [JsonPropertyName("FundamentalIndexId")]
        public string FundamentalIndexId { get; set; }
        [JsonPropertyName("General")]
        public GeneralIndex GeneralIndex { get; set; }
        [JsonPropertyName("Components")]
        public Dictionary<string, ComponentIndex> Components { get; set; }
        [JsonPropertyName("HistoricalTickerComponents")]
        public HistoricalComponentIndex HistoricalComponents { get; set; }
    }
    public class GeneralIndex
    {
        [JsonPropertyName("Code")]
        public string Code { get; set; }
        [JsonPropertyName("Type")]
        public string Type { get; set; }
        [JsonPropertyName("Name")]
        public string Name { get; set; }
        [JsonPropertyName("Exchange")]
        public string Exchange { get; set; }
        [JsonPropertyName("MarketCap")]
        public long MarketCap { get; set; }
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
    }
    public class ComponentIndex
    {
        public string ExchangeSymbolId => $"{Code}.{Exchange}";
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
        [JsonPropertyName("Weight")]
        public double Weight { get; set; }
    }
    public class HistoricalComponentIndex
    {
        [JsonPropertyName("Code")]
        public string Code { get; set; }
        [JsonPropertyName("Name")]
        public string Name { get; set; }
        [JsonPropertyName("StartDate")]
        public string StartDate { get; set; }
        [JsonPropertyName("EndDate")]
        public string EndDate { get; set; }
        [JsonPropertyName("IsActiveNow")]
        public int IsActiveNow { get; set; }
        [JsonPropertyName("IsDelisted")]
        public int IsDelisted { get; set; }
    }
}
