using LiteDB;
using MarketApi;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using Table;
using TableCsv;
using TableExtension;

namespace eodhd_download
{
    public class DateKeyedTableBuilder
    {
        public DateKeyedTableBuilder(DateTime from, DateTime to, string dateColumnName, params DayOfWeek[] dayOfWeeks)
        {
            _baseTable = ExtractUtil.CreateDateRangeTable(DateOnly.FromDateTime(from), DateOnly.FromDateTime(to), dateColumnName, dayOfWeeks);
            _dict = _baseTable.BuildKey<string>((IColumn<string>)_baseTable.columns[dateColumnName]);
        }
        public void Include(string prefix, IColumn dateCol, params IColumn[] colsToInclude)
        {
            var dc = dateCol as IColumn<string>;
            if (dc == null) { throw new Exception("Date columnmust be of type 'string'"); }
            if (colsToInclude.Length == 0) { throw new Exception("Must have at least one column to include"); }
            IColumn<double>[] cti = new IColumn<double>[colsToInclude.Length];
            for (int i = 0; i < colsToInclude.Length; i++)
            {
                var temp = colsToInclude[i] as IColumn<double>;
                if (temp == null) { throw new Exception($"Column to include ({colsToInclude[i].Name}) must be of type 'double'"); }
                cti[i] = temp;
            }
            _baseTable = _baseTable.LeftJoin(_dict, dc, prefix, cti);
        }
        public IDynamicTable Table { get {  return _baseTable; }  }
        #region private
        private IDynamicTable _baseTable;
        private IDictionary<string, int> _dict;
        #endregion
    }

    internal class Program
    {
        internal static async Task<int> LoadFundamentalIndex(LiteDatabase db, EodNetApi.Client eodHd, params List<string> indices)
        {
            int totalAffected = 0;
            foreach (var exchangeSymbolId in indices)
            {
                try
                {
                    var indicesColl = db.GetCollection<MarketApi.FundamentalIndex>("fundamental_indices");

                    var sw = new Stopwatch();
                    sw.Start();
                    var index = await eodHd.GetFundamentalsIndexAsync(exchangeSymbolId);
                    sw.Stop();

                    index.ExchangeSymbolId = exchangeSymbolId;

                    sw.Restart();
                    db.BeginTrans();
                    indicesColl.Upsert(index);
                    db.Commit();
                    sw.Stop();
                    Console.WriteLine($"Committed changes for FundamentalIndex {index.ExchangeSymbolId} in {sw.ElapsedMilliseconds} ms");
                }
                catch
                {
                    db.Rollback();
                    throw;
                }
            }
            return totalAffected;
        }
        internal static async Task<int> LoadFundamentalEtf(LiteDatabase db, EodNetApi.Client eodHd, params List<string> etfs)
        {
            int totalAffected = 0;
            foreach (var exchangeSymbolId in etfs)
            {
                try
                {
                    var etfsColl = db.GetCollection<MarketApi.FundamentalEtf>("fundamental_etfs");

                    var sw = new Stopwatch();
                    sw.Start();
                    var etf = await eodHd.GetFundamentalsEtfAsync(exchangeSymbolId);
                    sw.Stop();
  
                    etf.ExchangeSymbolId = exchangeSymbolId;
 
                    sw.Restart();
                    db.BeginTrans();
                    etfsColl.Upsert(etf);
                    db.Commit();
                    sw.Stop();
                    Console.WriteLine($"Committed changes for FundamentalEtf {etf.ExchangeSymbolId} in {sw.ElapsedMilliseconds} ms");
                }
                catch
                {
                    db.Rollback();
                    throw;
                }
            }
            return totalAffected;
        }
        internal static async Task<int> LoadFundamentalStock(LiteDatabase db, EodNetApi.Client eodHd, params List<string> stocks)
        {
            int totalAffected = 0;
            foreach (var exchangeSymbolId in stocks)
            {
                try
                {
                    var stocksColl = db.GetCollection<MarketApi.FundamentalStock>("fundamental_stocks");

                    var sw = new Stopwatch();
                    sw.Start();
                    var stock = await eodHd.GetFundamentalsStockAsync(exchangeSymbolId);
                    sw.Stop();

                    stock.ExchangeSymbolId = exchangeSymbolId;

                    sw.Restart();
                    db.BeginTrans();
                    stocksColl.Upsert(stock);
                    db.Commit();
                    sw.Stop();
                    Console.WriteLine($"Committed changes for FundamentalStock {stock.ExchangeSymbolId} in {sw.ElapsedMilliseconds} ms");
                }
                catch
                {
                    db.Rollback();
                    throw;
                }
            }
            return totalAffected;
        }
        internal static async Task<int> LoadExchangeSymbols(LiteDatabase db, EodNetApi.Client eodHd, params List<string> extractionGroups)
        {
            int totalAffected = 0;
            foreach (var group in extractionGroups)
            {
                try
                {
                    var exchangeSymbolsColl = db.GetCollection<MarketApi.ExchangeSymbol>("exchange_symbols");
                    exchangeSymbolsColl.EnsureIndex("Symbol", x => x.Symbol, false);
                    exchangeSymbolsColl.EnsureIndex("Exchange", x => x.Exchange, false);

                    var sw = new Stopwatch();
                    sw.Start();
                    var exchangeSymbols = await eodHd.GetExchangeSymbolsAsync(group);
                    sw.Stop();
                    Console.WriteLine($"Fetched {exchangeSymbols.Count} symbols for group {group} from EODHD in {sw.ElapsedMilliseconds} ms");

                    sw.Restart();
                    db.BeginTrans();
                    exchangeSymbolsColl.Upsert(exchangeSymbols);
                    db.Commit();
                    totalAffected += exchangeSymbols.Count;
                    sw.Stop();
                    Console.WriteLine($"Committed changes for group {group} in {sw.ElapsedMilliseconds} ms");
                }
                catch
                {
                    db.Rollback();
                    throw;
                }
            }
            return totalAffected;
        }
        internal static async Task<int> LoadEndOfDayHistoricalStockPrice(LiteDatabase db, EodNetApi.Client eodHd, DateTime startDate, DateTime endDate, params List<string> exchangeSymbolIds)
        {
            var totalAffected = 0;
            try
            {
                if (exchangeSymbolIds.Count == 0) { return 0; }

                var historicalDayPricesColl = db.GetCollection<MarketApi.HistoricalStockPrice>("historical_day_prices");
                historicalDayPricesColl.EnsureIndex("ExchangeSymbolId", x => x.ExchangeSymbolId, false);

                var sw = new Stopwatch();

                for (int i = 0; i < exchangeSymbolIds.Count; i++)
                {
                    Console.WriteLine($"Processing (Historical Stock Prices) {i + 1}/{exchangeSymbolIds.Count}: {exchangeSymbolIds[i]}");

                    sw.Start();
                    List<MarketApi.HistoricalStockPrice> historicalPricesEodHd = await eodHd.GetEndOfDayHistoricalPriceAsync(exchangeSymbolIds[i], EodNetApi.Client.Period.Day, EodNetApi.Client.Order.Ascending,
                        DateOnly.FromDateTime(startDate), DateOnly.FromDateTime(endDate));
                    historicalPricesEodHd.ForEach(x => x.ExchangeSymbolId = exchangeSymbolIds[i]);
                    sw.Stop();
                    Console.WriteLine($"Fetched {historicalPricesEodHd.Count} historical prices for {exchangeSymbolIds[i]} from EODHD in {sw.ElapsedMilliseconds} ms");

                    sw.Restart();
                    db.BeginTrans();
                    historicalDayPricesColl.Upsert(historicalPricesEodHd);
                    db.Commit();
                    totalAffected += historicalPricesEodHd.Count;
                    sw.Stop();
                    Console.WriteLine($"Committed changes for Symbol {exchangeSymbolIds[i]} in {sw.ElapsedMilliseconds} ms");
                }
            }
            catch
            {
                db.Rollback();
                throw;
            }

            return totalAffected;
        }
        internal static async Task<int> LoadSentimentsData(LiteDatabase db, EodNetApi.Client eodHd, DateTime startDate, DateTime endDate, params List<string> exchangeSymbolIds)
        {
            var totalAffected = 0;
            try
            {
                if (exchangeSymbolIds.Count == 0) { return 0; }

                var sentimentsDataColl = db.GetCollection<MarketApi.SentimentsData>("sentiments_data");
                sentimentsDataColl.EnsureIndex("ExchangeSymbolId", x => x.ExchangeSymbolId, false);

                var sw = new Stopwatch();

                sw.Start();
                var sentimentsDataEodHd = await eodHd.GetSentimentsAsync(exchangeSymbolIds, DateOnly.FromDateTime(startDate), DateOnly.FromDateTime(endDate)); 
                sw.Stop();
                Console.WriteLine($"Processing (Sentiments Data) {exchangeSymbolIds.Count}");
                Console.WriteLine($"Fetched {sentimentsDataEodHd.Count} symbols from EODHD in {sw.ElapsedMilliseconds} ms");

                sw.Restart();
                db.BeginTrans();
                sentimentsDataColl.Upsert(sentimentsDataEodHd);
                db.Commit();
                totalAffected += sentimentsDataEodHd.Count;
                sw.Stop();
                Console.WriteLine($"Committed ({sentimentsDataEodHd.Count}) sentiment changes for {totalAffected} Symbols in {sw.ElapsedMilliseconds} ms");
            }
            catch
            {
                db.Rollback();
                throw;
            }

            return totalAffected;
        }
        internal static async Task<List<string>> GetETFSymbols(LiteDatabase db, string etf)
        {
            // Get ETF record
            var etfsColl = db.GetCollection<MarketApi.FundamentalEtf>("fundamental_etfs");
            var etfData = etfsColl.FindById(etf);
            if (etfData == null) { throw new Exception($"Attempting to load data for Holdings for ETF({etf}) which does not exist in database"); }

             // create an array of ETF Holdings
            List<string> etfParams = etfData.DataEtf.HoldingsEtf.Select(x => x.Key).ToList();

            return etfParams;
        }
        internal static async Task<List<ComponentIndex>> GetIndexSymbols(LiteDatabase db, string index)
        {
            // Get Index record
            var indicesColl = db.GetCollection<MarketApi.FundamentalIndex>("fundamental_indices");
            var indexData = indicesColl.FindById(index);
            if (indexData == null) { throw new Exception($"Attempting to load data for the components of Index({index}) which does not exist in database"); }

            // create an array of Components
            List<ComponentIndex> indicesParams = indexData.Components.Select(x => x.Value).ToList();

            return indicesParams;
        }
        internal static IDynamicTable ExtractHistoricalStockPrices(ILiteCollection<MarketApi.HistoricalStockPrice> coll, string exchangeSymbolId, DateTime fromDate)
        {
            var fromDateA = fromDate.ToString("yyyy-MM-dd");
            var results = coll.Find(Query.And(Query.EQ("ExchangeSymbolId", exchangeSymbolId), Query.GTE("Date", fromDateA))).OrderBy(x => x.Date).ToList();
            var table = results.ToTable();
            return table;
        }
        internal static IDynamicTable ExtractSentimentsData(ILiteCollection<MarketApi.SentimentsData> coll, string exchangeSymbolId, DateTime fromDate)
        {
            var fromDateA = fromDate.ToString("yyyy-MM-dd");
            var results = coll.Find(Query.And(Query.EQ("ExchangeSymbolId", exchangeSymbolId), Query.GTE("Date", fromDateA))).OrderBy(x => x.Date).ToList();
            var table = results.ToTable();
            return table;
        }
        internal static void IncludeHistoricalStockPrices(DateKeyedTableBuilder tableBuilder, ILiteCollection<MarketApi.HistoricalStockPrice> coll, List<string> exchangeSymbolIds, DateTime fromDate, bool closeOnly, bool volume)
        {
            foreach (var exchangeSymbolId in exchangeSymbolIds)
            {
                var tt = ExtractHistoricalStockPrices(coll, exchangeSymbolId, fromDate);

                var splitSym = exchangeSymbolId.Split('.');
                if (splitSym.Length != 2) { throw new Exception($"Invalid format of symbol ({exchangeSymbolId}) should be format 'sym.exchng'"); }

                if (closeOnly)
                {
                    tableBuilder.Include(splitSym[0].ToLower(), tt.columns["Date"], tt.columns["Close"]);
                }
                else
                {
                    if (volume)
                    {
                        tableBuilder.Include(splitSym[0].ToLower(), tt.columns["Date"], tt.columns["Open"],
                            tt.columns["Low"], tt.columns["High"], tt.columns["Close"], tt.columns["Volume"]);

                    }
                    else
                    {
                        tableBuilder.Include(splitSym[0].ToLower(), tt.columns["Date"], tt.columns["Open"],
                             tt.columns["Low"], tt.columns["High"], tt.columns["Close"]);

                    }
                }
            }
        }
        internal static void IncludeSentimentsData(DateKeyedTableBuilder tableBuilder, ILiteCollection<MarketApi.SentimentsData> coll, List<string> exchangeSymbolIds, DateTime fromDate, bool count)
        {
            foreach (var exchangeSymbolId in exchangeSymbolIds)
            {
                var tt = ExtractSentimentsData(coll, exchangeSymbolId, fromDate);

                var splitSym = exchangeSymbolId.Split('.');
                if (splitSym.Length != 2) { throw new Exception($"Invalid format of symbol ({exchangeSymbolId}) should be format 'sym.exchng'"); }

                if (count)
                {
                    tableBuilder.Include(splitSym[0].ToLower(), tt.columns["Date"], tt.columns["Count"], tt.columns["Normalized"]);
                }
                else
                {
                    tableBuilder.Include(splitSym[0].ToLower(), tt.columns["Date"], tt.columns["Normalized"]);
                }
            }
        }
        static async Task Main(string[] args)
        {
            bool DoLoadLookup = true;
            bool DoLoadFundamentalETFs = true;
            bool DoLoadFundamentalIndex = true;
            bool DoLoadPrices = true;
            bool DoLoadSentiment = true;
            bool DoLoadFundamentalStocks = true;
            bool DoWriteExtract = true;
            bool DoWriteSymbolsALL = true;
            bool DoWriteSymbolsSPX = true;

            var dbPath = Environment.GetEnvironmentVariable("MARKETDATA_DB_PATH")
                ?? @"D:\LiteDb\EodHd.db";
            var db = new LiteDatabase(dbPath);

            var apiToken = Environment.GetEnvironmentVariable("EODHD_API_TOKEN");
            if (string.IsNullOrWhiteSpace(apiToken))
            {
                throw new InvalidOperationException(
                    "EODHD API token is not set. Set the EODHD_API_TOKEN environment variable before running.");
            }

            var eodHdNew = new EodNetApi.Client(apiToken);

            if (DoLoadLookup)
            {
                // Refresh / Load exchange symbols
                var loadExchangeSymbolsTask = LoadExchangeSymbols(db, eodHdNew, "INDX", "NYSE", "NASDAQ", "TO");
                loadExchangeSymbolsTask.Wait();
                Console.WriteLine($"ExchangeSymbols loaded: {loadExchangeSymbolsTask.Result}");
            }

            if (DoLoadFundamentalETFs)
            {
                // Load composite symbols
                var loadCompositeSymbolsTask = LoadFundamentalEtf(db, eodHdNew, "QQQ.US");
                loadCompositeSymbolsTask.Wait();
                Console.WriteLine($"CompositeSymbols QQQ.US loaded: {loadCompositeSymbolsTask.Result}");
            }

            if (DoLoadFundamentalIndex)
            {
                // Load composite symbols
                var loadCompositeSymbolsTask = LoadFundamentalIndex(db, eodHdNew, "GSPC.INDX");
                loadCompositeSymbolsTask.Wait();
                Console.WriteLine($"CompositeSymbols GSPC.INDX loaded: {loadCompositeSymbolsTask.Result}");
            }

            // Create an array of Indexes and QQQ components
            string[] indexKeys = new string[]
            {
                "GSPC.INDX",
                "VIX.INDX",
                "TNX.INDX",
                "DXY.INDX",
                "N225.INDX",
                "GDAXI.INDX",
                "XAU.INDX",
                "BCOMCL.INDX",
                "S5TH.INDX",
                "S5TW.INDX",
                "ADVN.INDX",
                "QQQ.US"
            };
            var qqqKeys = await GetETFSymbols(db, "QQQ.US");
            var priceExchangeSymbolList = new List<string>([..indexKeys, ..qqqKeys]);
            var sentExchangeSymbolList = new List<string>(["QQQ.US", .. qqqKeys]);

            if (DoLoadPrices)
            {
                // Load Index and Top 50 QQQ components historical day prices
                Console.WriteLine("\n\n Loading Historical Stock pricess =========================================================");
                var loadHistoricalStockPriceTask = LoadEndOfDayHistoricalStockPrice(db, eodHdNew, DateTime.Now.AddDays(-10000), DateTime.Now, priceExchangeSymbolList);
                loadHistoricalStockPriceTask.Wait();
            }

            if (DoLoadSentiment)
            {
                // Load Top 50 QQQ components Sentiment
                Console.WriteLine("\n\n Loading Sentiment prices for Indexes =========================================================");
                var loadSentimentsDataTask = LoadSentimentsData(db, eodHdNew, DateTime.Now.AddDays(-10000), DateTime.Now, sentExchangeSymbolList);
                loadSentimentsDataTask.Wait();
            }

            if (DoLoadFundamentalStocks)
            {
                // Load Fundamental Stock data for QQQ Members
                Console.WriteLine("\n\n Loading Fundamental Data for QQQ MEmbers =========================================================");
                var loadFundamentalStockTask = LoadFundamentalStock(db, eodHdNew, qqqKeys);
                loadFundamentalStockTask.Wait();
            }

            // Build extract table
            if (DoWriteExtract)
            {
                int daysBack = 4000;
                var startDate = DateTime.Now.AddDays(-daysBack);
                var tableBuilder = new DateKeyedTableBuilder(DateTime.Now.AddDays(-daysBack), DateTime.Now,
                    "Date", DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);

                var hspColl = db.GetCollection<MarketApi.HistoricalStockPrice>("historical_day_prices");
                IncludeHistoricalStockPrices(tableBuilder, hspColl, priceExchangeSymbolList, startDate, false, true);

                var sdColl = db.GetCollection<MarketApi.SentimentsData>("sentiments_data");
                IncludeSentimentsData(tableBuilder, sdColl, sentExchangeSymbolList, startDate, true);

                // Fill in blanks in the table and write it out
                var table = tableBuilder.Table;
                ExtractUtil.FillInTable<double>(table);
                table.WriteCsv(@"D:\predict\index_extract.csv", CsvHeaderType.Header);
            }

            if (DoWriteSymbolsALL)
            {
                // Write out symbol file
                var symbolsColl = db.GetCollection<MarketApi.ExchangeSymbol>("exchange_symbols");
                var results = symbolsColl.FindAll().ToList();
                results.ForEach(x => { x.Country = (string.IsNullOrEmpty(x.Country) ? "" : x.Country); x.Name = (string.IsNullOrEmpty(x.Name) ? "" : x.Name); });
                var table = results.ToTable();
                table.WriteCsv(@"D:\predict\exchange_symbols.csv", CsvHeaderType.Header);
            }

            if (DoWriteSymbolsSPX)
            {
                // Write out symbol file
                var results = await GetIndexSymbols(db, "GSPC.INDX");
                var table = results.ToTable();
                table.WriteCsv(@"D:\predict\SPX500_symbols.csv", CsvHeaderType.Header);
            }

            // Clean up
            db.Dispose();
        }
    }
}
