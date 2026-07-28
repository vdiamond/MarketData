using RestClient;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EodNetApi
{
    public class Client
    {
        public Client(string apiKey, int timeout = 10000)
        {
            _restClient = new RestClientApi(_root_url, new EodFormatter(), timeout);
            _restClient.DefaultQueryParameters.Add("api_token", apiKey);
            _restClient.DefaultQueryParameters.Add("fmt", "json");
        }
        public async Task<List<MarketApi.Exchange>> GetExchangeGroupsAsync(CancellationToken ct = default)
        {
            var apiResponse = await _restClient.CallApi<string>(_get_exchanges, ct, false);
            if (!apiResponse.Ok)
            {
                throw new Exception($"{apiResponse.ErrorMessage}/{apiResponse.Content}");
            }
            var ret = JsonSerializer.Deserialize<List<MarketApi.Exchange>>($"{apiResponse.Content}");
            if (ret == null)
            {
                throw new Exception("Deserialization of Exchange list failed, null returned");
            }
            return ret;
        }
        public async Task<List<MarketApi.ExchangeSymbol>> GetExchangeSymbolsAsync(string exchangeGroup, CancellationToken ct = default)
        {
            var apiResponse = await _restClient.CallApi<string>($@"{_get_exchange_symbols}/{exchangeGroup}", ct, false);
            if (!apiResponse.Ok)
            {
                throw new Exception($"{apiResponse.ErrorMessage}/{apiResponse.Content}");
            }
            var ret = JsonSerializer.Deserialize<List<MarketApi.ExchangeSymbol>>($"{apiResponse.Content}");
            if (ret == null) 
            {
                throw new Exception("Deserialization of ExchangeSymbol list failed, null returned");
            }
            return ret;
        }
        /*
        public async Task<BsonDocument> GetExchangeDetailAsync(string exchangeGroup, CancellationToken ct = default)
        {
            var apiResponse = await _restClient.CallApi<string>($@"{_get_exchange_details}/{exchangeGroup}", ct, false);
            if (!apiResponse.Ok)
            {
                throw new Exception($"{apiResponse.ErrorMessage}/{apiResponse.Content}");
            }
            var ret = BsonDocument.Parse($"{{ {_bson_enclosing_document} : {apiResponse.Content} }}");
            ret = ret[_bson_enclosing_document].AsBsonDocument;
            return ret;
        }
        public async Task<BsonDocument> GetFundamentalsAsync(string symbolGroup, CancellationToken ct = default)
        {
            var apiResponse = await _restClient.CallApi<string>($@"{_get_fundamentals}/{symbolGroup}", ct, false);
            if (!apiResponse.Ok)
            {
                throw new Exception($"Resp. from Service({apiResponse.ErrorMessage}/{apiResponse.Content})");
            }
            var ret = BsonDocument.Parse($"{{ {_bson_enclosing_document} : {apiResponse.Content} }}");
            ret = ret[_bson_enclosing_document].AsBsonDocument;
            return ret;
        }

        */

        public async Task<MarketApi.FundamentalStock> GetFundamentalsStockAsync(string exchangeSymbolId, CancellationToken ct = default)
        {
            // To performenace tune, put a filter on to only get Stock Data
            var apiResponse = await _restClient.CallApi<string>($@"{_get_fundamentals}/{exchangeSymbolId}", ct, false);
            if (!apiResponse.Ok)
            {
                throw new Exception($"Resp. from Service({apiResponse.ErrorMessage}/{apiResponse.Content})");
            }
            //debug ===============================================
            // File.WriteAllText(@"d:\xxx.json", apiResponse.Content);
            //=====================================================
            var ret = JsonSerializer.Deserialize<MarketApi.FundamentalStock>($"{apiResponse.Content}");
            if (ret == null)
            {
                throw new Exception("Error in deserializing FundamentalStock, response was null");
            }
            ret.ExchangeSymbolId = exchangeSymbolId;
            return ret;
        }
        public async Task<MarketApi.FundamentalEtf> GetFundamentalsEtfAsync(string exchangeSymbolId, CancellationToken ct = default)
        {
            // To performenace tune, put a filter on to only get ETF Data
            var apiResponse = await _restClient.CallApi<string>($@"{_get_fundamentals}/{exchangeSymbolId}", ct, false);
            if (!apiResponse.Ok)
            {
                throw new Exception($"Resp. from Service({apiResponse.ErrorMessage}/{apiResponse.Content})");
            }

            var ret = JsonSerializer.Deserialize<MarketApi.FundamentalEtf>($"{apiResponse.Content}");
            if (ret == null)
            {
                throw new Exception("Error in deserializing FundamentalEtf, response was null");
            }
            ret.ExchangeSymbolId = exchangeSymbolId;
            return ret;
        }
        public async Task<MarketApi.FundamentalIndex> GetFundamentalsIndexAsync(string exchangeSymbolId, CancellationToken ct = default)
        {
            // To performenace tune, put a filter on to only get Index Data
            var apiResponse = await _restClient.CallApi<string>($@"{_get_fundamentals}/{exchangeSymbolId}", ct, false);
            if (!apiResponse.Ok)
            {
                throw new Exception($"Resp. from Service({apiResponse.ErrorMessage}/{apiResponse.Content})");
            }

            var ret = JsonSerializer.Deserialize<MarketApi.FundamentalIndex>($"{apiResponse.Content}");
            if (ret == null)
            {
                throw new Exception("Error in deserializing FundamentalIndex, response was null");
            }
            ret.ExchangeSymbolId = exchangeSymbolId;
            return ret;
        }
        public enum Period { Day, Week, Month  }
        public enum Order {  Ascending, Descending }
        public async Task<List<MarketApi.HistoricalStockPrice>> GetEndOfDayHistoricalPriceAsync(string exchangeSymbolId, Period period = Period.Day, Order order = Order.Ascending, DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
        {
            _restClient.QueryParameters.Add<Client.Period>("period", period);
            _restClient.QueryParameters.Add<Client.Order>("order", order);
            if (from != null)
            {
                _restClient.QueryParameters.Add<DateOnly>("from", from.Value);
            }
            if (to != null)
            {
                _restClient.QueryParameters.Add<DateOnly>("to", to.Value);
            }
            var apiResponse = await _restClient.CallApi<string>($@"{_get_eod}/{exchangeSymbolId}", ct, false);
            if (!apiResponse.Ok)
            {
                throw new Exception($"{apiResponse.ErrorMessage}/{apiResponse.Content}");
            }
            var ret = JsonSerializer.Deserialize<List<MarketApi.HistoricalStockPrice>>($"{apiResponse.Content}");
            if ( ret == null )
            {
                throw new Exception("Error in deserializing HistoricalStockPrice - value of null returned");
            }
            return ret;
        }
        /*
        public async Task<List<BsonDocument>> GetEndOfDayBulk(string exchange, DateOnly? date= null, CancellationToken ct = default)
        {
            if (date != null)
            {
                _restClient.QueryParameters.Add<DateOnly>("date", date.Value);
            }
            var apiResponse = await _restClient.CallApi<string>($@"{_get_eod_bulk_last_day}/{exchange}", ct, false);
            if (!apiResponse.Ok)
            {
                throw new Exception($"{apiResponse.ErrorMessage}/{apiResponse.Content}");
            }
            var document = BsonDocument.Parse($"{{ {_bson_enclosing_document} : {apiResponse.Content} }}");
            var ret = _toListEod(document[_bson_enclosing_document].AsBsonArray);
            return ret;
        }
        public async Task<List<BsonDocument>> GetEarnings(DateOnly? fromDate, DateOnly? toDate = null, List<string>? codes = null, CancellationToken ct = default)
        {
            if (fromDate != null)
            {
                _restClient.QueryParameters.Add<DateOnly>("from", fromDate.Value);
            }
            if (toDate != null)
            {
                _restClient.QueryParameters.Add<DateOnly>("to", toDate.Value);
            }
            if (codes != null)
            {
                _restClient.QueryParameters.Add("symbols", string.Join(',', codes));
            }
            var apiResponse = await _restClient.CallApi<string>($@"{_get_earnings}", ct, false);
            if (!apiResponse.Ok)
            {
                throw new Exception($"{apiResponse.ErrorMessage}/{apiResponse.Content}");
            }
            return _toList(BsonDocument.Parse($"{apiResponse.Content}")["earnings"].AsBsonArray);
        }
*/
        public async Task<List<MarketApi.SentimentsData>> GetSentimentsAsync(List<string> exchangeSymbolIds, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
        {
            _restClient.QueryParameters.Add<DateOnly>("from", fromDate);
            _restClient.QueryParameters.Add<DateOnly>("to", toDate);
            _restClient.QueryParameters.Add("s", string.Join(',', exchangeSymbolIds));
            var apiResponse = await _restClient.CallApi<string>($@"{_get_sentiments}", ct, false);
            if (!apiResponse.Ok)
            {
                throw new Exception($"{apiResponse.ErrorMessage}/{apiResponse.Content}");
            }
            if (apiResponse.Content == "[]")
            {
                return new List<MarketApi.SentimentsData>();
            }
            var sentDict = JsonSerializer.Deserialize<Dictionary<string, List<MarketApi.SentimentsData>>>($"{apiResponse.Content}");
            if (sentDict == null) { throw new Exception("Deserialization of SentimentsData failed. Return value is null"); }

            var ret = new List<MarketApi.SentimentsData>();
            foreach(var sent in sentDict)
            {
                foreach (var item in sent.Value)
                {
                    ret.Add(new MarketApi.SentimentsData() { ExchangeSymbolId = sent.Key, Count = item.Count, Date = item.Date, Normalized = item.Normalized });
                }
            }

            return ret;
        }
        
/*
        public async Task<BsonDocument> GetUser(CancellationToken ct = default)
        {
            var apiResponse = await _restClient.CallApi<string>($@"{_get_user}", ct, false);
            if (!apiResponse.Ok)
            {
                throw new Exception($"{apiResponse.ErrorMessage}/{apiResponse.Content}");
            }
            var ret = BsonDocument.Parse($"{apiResponse.ErrorMessage}/{apiResponse.Content}");
            return ret;
        }

        */
        #region private
        /*
        private List<BsonDocument> _toList(BsonArray bsonArray)
        {
            var ret = new List<BsonDocument>();
            foreach (var item in bsonArray)
            {
                ret.Add(item.AsBsonDocument);
            }
            return ret;
        }
        private List<BsonDocument> _toListEod(BsonArray bsonArray, string symbolGroup = "")
        {
            var ret = new List<BsonDocument>();
            foreach (var item in bsonArray)
            {
                var doc = item.AsBsonDocument;
                if (symbolGroup == "")
                {
                    var sg = $"{doc["code"]}.{doc["exchange_short_name"]}";
                    doc.Add("SymbolGroup", sg);
                    doc.Remove("code");
                    doc.Remove("exchange_short_name");
                }
                else 
                {
                    doc.Add("SymbolGroup", symbolGroup);
                }
                ret.Add(doc);
            }
            return ret;
        }
        private List<BsonDocument> _toListSentiment(BsonDocument bsonDocument, List<BsonDocument> list = null)
        {
            if (list == null) { list = new List<BsonDocument>(); }
            foreach (var item in bsonDocument)
            {
                var symbolGroup = item.Name;
                var itemArray = item.Value.AsBsonArray;

                foreach (var sent in itemArray)
                {
                    list.Add(
                        new BsonDocument {
                            { "SymbolGroup", symbolGroup },
                            { "Date", sent["date"] },
                            { "Count", sent["count"] },
                            { "Normalized", sent["normalized"] }
                        }
                        );
                }
            }
            return list;
        }
        */
        private RestClientApi _restClient;
        private const string _root_url = @"https://eodhd.com/api/";
        private const string _bson_enclosing_document = "Elements";
        private const string _get_exchanges = @"exchanges-list";
        private const string _get_exchange_symbols = @"exchange-symbol-list";
        private const string _get_exchange_details = @"exchange-details";
        private const string _get_fundamentals = @"fundamentals";
        private const string _get_eod = @"eod";
        private const string _get_eod_bulk_last_day = @"eod-bulk-last-day";
        private const string _get_earnings = @"calendar/earnings";
        private const string _get_user = @"user";
        private const string _get_sentiments = @"sentiments";
        #endregion
    }
    /*
        internal class EodFormatter : IFormatter
        {
            public string ToString<T>(T value, int option = 0)
            {
                if (typeof(T) == typeof(Client.Period))
                {
                    Client.Period p = (Client.Period)(object)value; 
                    if (p == Client.Period.Day) { return "d"; }
                    if (p == Client.Period.Week) { return "w"; }
                    return "m";
                }
                if (typeof(T) == typeof(Client.Order))
                {
                    Client.Order p = (Client.Order)(object)value;
                    if (p == Client.Order.Ascending) { return "a"; }
                    return "d";
                }
                if (typeof(T) == typeof(DateOnly))
                {
                    DateOnly p = (DateOnly)(object)value;
                    return p.ToString("yyyy-MM-dd");
                }
                return value.ToString(); ;
            }
        }
    */
    
        internal class EodFormatter : IFormatter
        {
            public string ToString<T>(T value, int option = 0)
            {
//                if (typeof(T) == typeof(Client.Period))
//                {
//                    Client.Period p = (Client.Period)(object)value; 
//                    if (p == Client.Period.Day) { return "d"; }
//                    if (p == Client.Period.Week) { return "w"; }
//                    return "m";
//                }
//                if (typeof(T) == typeof(Client.Order))
//                {
//                    Client.Order p = (Client.Order)(object)value;
//                    if (p == Client.Order.Ascending) { return "a"; }
//                    return "d";
//                }
                if (typeof(T) == typeof(DateOnly))
                {
                    DateOnly p = (DateOnly)(object)value;
                    return p.ToString("yyyy-MM-dd");
                }
                return value.ToString(); 
            }
        }

}
