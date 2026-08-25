
using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace MarketApi
{
    public enum DataTypeEnum { INDEX, FUND, BOND, Common_Stock, ETF, Mutual_Fund, Notes, Preferred_Stock, Warrant, Unit }
    public enum CountryCodeISO3Enum
    {
        USA, CAN, GBR, DEU, FRA, ITA, ESP, JPN, CHN, IND, AUS, BRA, RUS, KOR, MEX, NLD, SWE,
        CHE, BEL, AUT, NOR, DNK, FIN, IRL, PRT, GRC, POL, CZE, HUN
    }
    public enum CurrencyCodeEnum
    {
        USD, CAD, GBP, EUR, FRF, ITL, ESP, JPY, CNY, INR, AUD, BRL, RUB, KRW, MXN, NLG, SEK,
        CHF, BEF, ATS, NOR, DKK, FIN, IEP, PTE, GRD, PLN, CZK, HUF, SGD, RON, ZAR, TRY, HKD,
        TWD, MYR, NZD, THB, IDR, VND, ARS, CLP, COP, PEN, UYU, DZD, HRK, BGN, LKR, EGP, NGN,
        KZT, QAR, KWD, OMR, BHD, LYD, MAD, TND, JOD, BIF, MUR, AED, AFN, ALL, AMD, ANG, AOA,
        AWG, AZN, BAM, BBD, BDT, BMD, BND, BSD, PKR, SAR, NOK, ISK, PHP, ILS, ILA, Unknown
    }
    public class Exchange
    {
        [BsonId]
        public string ExchangeId => Code;
        [JsonPropertyName("Name")]
        public string Name { get; set; }
        [JsonPropertyName("Code")]
        public string Code { get; set; }
        [JsonPropertyName("OperatingMIC")]
        public string OperatingMIC { get; set; }
        [JsonPropertyName("Country")]
        public string Country { get; set; }
        [JsonPropertyName("Currency")]
        public string Currency { get; set; }
        [JsonPropertyName("CountryISO2")]
        public string CountryISO2 { get; set; }
        [JsonPropertyName("CountryISO3")]
        public string CountryISO3 { get; set; }
    }
    public class ExchangeSymbol
    {
        [BsonId]
        public string ExchangeSymbolId => $"{Symbol}.{Exchange}";
        [JsonPropertyName("Code")]
        public string Symbol { get; set; }
        [JsonPropertyName("Name")]
        public string Name { get; set; }
        [JsonPropertyName("Country")]
        public string Country { get; set; }
        [JsonPropertyName("Exchange")]
        public string Exchange { get; set; }
        [JsonPropertyName("Currency")]
        public string Currency { get; set; }
        [JsonPropertyName("Type")]
        public string Type { get; set; }
        [JsonPropertyName("Isin")]
        public object Isin { get; set; }
    }
    public class HistoricalStockPrice
    {
        [BsonId]
        public string ExchangeSymbolIdDate => $"{ExchangeSymbolId}.{Date:yyyy-MM-dd}";
        [JsonPropertyName("exchangeSymbolId")]
        public string ExchangeSymbolId { get; set; }
        [JsonPropertyName("date")]
        public string Date { get; set; }
        [JsonPropertyName("open")]
        public double Open { get; set; }
        [JsonPropertyName("high")]
        public double High { get; set; }
        [JsonPropertyName("low")]
        public double Low { get; set; }
        [JsonPropertyName("close")]
        public double Close { get; set; }
        [JsonPropertyName("adjusted_close")]
        public double AdjustedClose { get; set; }
        [JsonPropertyName("volume")]
        public double Volume { get; set; }
    }
    public class SentimentsData
    {
        [BsonId]
        public string ExchangeSymbolIdDate => $"{ExchangeSymbolId}.{Date:yyyy-MM-dd}";

        [JsonPropertyName("exchangeSymbolId")]
        public string ExchangeSymbolId { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("count")]
        public double Count { get; set; }

        [JsonPropertyName("normalized")]
        public double Normalized { get; set; }
    }



    public class MacroIndicator
    {
        [JsonPropertyName("CountryCode")]
        public string CountryCode { get; set; }
        [JsonPropertyName("CountryName")]
        public string CountryName { get; set; }
        [JsonPropertyName("Indicator")]
        public string Indicator { get; set; }
        [JsonPropertyName("Date")]
        public string Date { get; set; }
        [JsonPropertyName("Period")]
        public string Period { get; set; }
        [JsonPropertyName("Value")]
        public double? Value { get; set; }
    }

    public class EconomicEvent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("comparison")]
        public string Comparison { get; set; }
        [JsonPropertyName("period")]
        public string Period { get; set; }
        [JsonPropertyName("country")]
        public string Country { get; set; }
        [JsonPropertyName("date")]
        public string Date { get; set; }
        [JsonPropertyName("actual")]
        public double? Actual { get; set; }
        [JsonPropertyName("previous")]
        public double? Previous { get; set; }
        [JsonPropertyName("estimate")]
        public double? Estimate { get; set; }
        [JsonPropertyName("change")]
        public double? Change { get; set; }
        [JsonPropertyName("change_percentage")]
        public double? ChangePercentage { get; set; }
    }

}
