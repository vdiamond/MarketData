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
                    totalAffected++;
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
                    totalAffected++;
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
                    totalAffected++;
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

                var prefix = ExtractUtil.ColumnPrefixFromExchangeSymbolId(exchangeSymbolId);

                if (closeOnly)
                {
                    tableBuilder.Include(prefix, tt.columns["Date"], tt.columns["Close"]);
                }
                else
                {
                    if (volume)
                    {
                        tableBuilder.Include(prefix, tt.columns["Date"], tt.columns["Open"],
                            tt.columns["Low"], tt.columns["High"], tt.columns["Close"], tt.columns["Volume"]);

                    }
                    else
                    {
                        tableBuilder.Include(prefix, tt.columns["Date"], tt.columns["Open"],
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

                var prefix = ExtractUtil.ColumnPrefixFromExchangeSymbolId(exchangeSymbolId);

                if (count)
                {
                    tableBuilder.Include(prefix, tt.columns["Date"], tt.columns["Count"], tt.columns["Normalized"]);
                }
                else
                {
                    tableBuilder.Include(prefix, tt.columns["Date"], tt.columns["Normalized"]);
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
            bool DoWriteEventFlags = false;

            var dbPath = EnvOrDefault("MARKETDATA_DB_PATH", @"D:\LiteDb\EodHd.db");
            var extractCsv = EnvOrDefault("MARKETDATA_EXTRACT_CSV", @"D:\predict\index_extract.csv");
            var symbolsCsv = EnvOrDefault("MARKETDATA_SYMBOLS_CSV", @"D:\predict\exchange_symbols.csv");
            var spxSymbolsCsv = EnvOrDefault("MARKETDATA_SPX_SYMBOLS_CSV", @"D:\predict\SPX500_symbols.csv");
            var eventsCsv = EnvOrDefault("MARKETDATA_EVENTS_CSV", @"D:\predict\us_economic_events.csv");
            var eventTypesCsv = EnvOrDefault("MARKETDATA_EVENT_TYPES_CSV", @"D:\predict\us_economic_event_types.csv");

            var writeEventFlagsOnly = args.Any(a => string.Equals(a, "--write-event-flags", StringComparison.OrdinalIgnoreCase));
            if (writeEventFlagsOnly)
            {
                var apiTokenOnly = Environment.GetEnvironmentVariable("EODHD_API_TOKEN");
                if (string.IsNullOrWhiteSpace(apiTokenOnly))
                {
                    throw new InvalidOperationException(
                        "EODHD API token is not set. Set the EODHD_API_TOKEN environment variable before running.");
                }
                var eodHdEvents = new EodNetApi.Client(apiTokenOnly, timeout: 60000);
                await EventCalendar.WriteEventFlagsAsync(eodHdEvents, extractCsv, eventsCsv, eventTypesCsv);
                return;
            }

            if (HasFlag(args, "--write-earnings"))
            {
                var apiTokenEarn = RequireApiToken();
                var earningsCsv = EnvOrDefault("MARKETDATA_EARNINGS_CSV", @"D:\predict\qqq_member_earnings.csv");
                var eodHdEarn = new EodNetApi.Client(apiTokenEarn, timeout: 120000);
                var earnResult = await EarningsCalendar.WriteEarningsFlagsAsync(eodHdEarn, extractCsv, earningsCsv);
                Console.WriteLine(
                    $"Earnings calendar: dump_rows={earnResult.DumpRows} extract_rows={earnResult.DataRows} evt_earn=1 {earnResult.EarnFlags} bmo={earnResult.BmoFlags} amc={earnResult.AmcFlags} skipped={string.Join(",", earnResult.SkippedTickers)} forbidden={earnResult.Forbidden}");
                if (!string.IsNullOrWhiteSpace(earnResult.StatusNote))
                {
                    Console.WriteLine(earnResult.StatusNote);
                }
                return;
            }


            if (HasFlag(args, "--write-qqq-intraday"))
            {
                var apiTokenIntra = RequireApiToken();
                var dumpCsv = EnvOrDefault("MARKETDATA_QQQ_INTRADAY_CSV", @"D:\predict\qqq_intraday_1h.csv");
                var eodHdIntra = new EodNetApi.Client(apiTokenIntra, timeout: 300000);
                var intraResult = await QqqIntraday.WriteAsync(eodHdIntra, extractCsv, dumpCsv);
                Console.WriteLine(
                    $"QQQ 1h: dump_rows={intraResult.DumpRows} extract_rows={intraResult.ExtractRows} h1={intraResult.FirstHourNonempty} hlast={intraResult.LastHourNonempty} forbidden={intraResult.Forbidden} too_short={intraResult.HistoryTooShort} joined={intraResult.Joined}");
                if (!string.IsNullOrWhiteSpace(intraResult.StatusNote))
                {
                    Console.WriteLine(intraResult.StatusNote);
                }
                return;
            }

            var loadPricesOnly = ParseCsvListArg(args, "--load-prices");
            if (loadPricesOnly != null)
            {
                var apiTokenPrices = RequireApiToken();
                using var dbPrices = new LiteDatabase(dbPath);
                var eodHdPrices = new EodNetApi.Client(apiTokenPrices, timeout: 60000);
                var symbols = new List<string>(loadPricesOnly);
                Console.WriteLine($"Loading prices only for {symbols.Count} symbol(s): {string.Join(", ", symbols)}");
                var totalAffected = 0;
                foreach (var symbol in symbols)
                {
                    var outcome = await TryLoadPricesWithUsFallback(dbPrices, eodHdPrices, symbol);
                    totalAffected += outcome.Affected;
                    Console.WriteLine(outcome.Note);
                }
                Console.WriteLine($"Price load complete. Rows upserted: {totalAffected}");
                return;
            }

            var joinPrices = ParseCsvListArg(args, "--join-prices");
            if (joinPrices != null)
            {
                using var dbJoin = new LiteDatabase(dbPath);
                var hspColl = dbJoin.GetCollection<MarketApi.HistoricalStockPrice>("historical_day_prices");
                var rows = ExtractJoin.JoinHistoricalPricesOntoExtract(
                    hspColl,
                    extractCsv,
                    joinPrices,
                    DateTime.Now.AddDays(-10000),
                    afterColumnNames: ["tnx.Volume"],
                    skipEmpty: true);
                Console.WriteLine($"Joined [{string.Join(", ", joinPrices)}] onto {extractCsv}; rows={rows}");
                return;
            }

            if (HasFlag(args, "--join-vix-terms"))
            {
                using var dbJoin = new LiteDatabase(dbPath);
                var hspColl = dbJoin.GetCollection<MarketApi.HistoricalStockPrice>("historical_day_prices");
                var vixTerms = new List<string> { "VIX1Y.INDX", "VIX6M.INDX" };
                var rows = ExtractJoin.JoinHistoricalPricesOntoExtract(
                    hspColl, extractCsv, vixTerms, DateTime.Now.AddDays(-10000));
                Console.WriteLine($"Joined VIX1Y/VIX6M onto {extractCsv}; rows={rows}");
                return;
            }

            if (HasFlag(args, "--load-macro"))
            {
                var apiTokenMacro = RequireApiToken();
                var sidecarCsv = EnvOrDefault("MARKETDATA_MACRO_CSV", @"D:\predict\us_macro_indicators.csv");
                var seriesDir = EnvOrDefault("MARKETDATA_MACRO_DIR", @"D:\predict\macro");
                var eodHdMacro = new EodNetApi.Client(apiTokenMacro, timeout: 60000);
                Console.WriteLine("Fetching USA macro indicators (alpha-3 USA).");
                var fetch = await MacroIndicators.FetchUsaAsync(eodHdMacro);
                if (fetch.Forbidden)
                {
                    Console.WriteLine(fetch.StatusNote);
                    Console.WriteLine($"Forbidden on key={fetch.ForbiddenKey}. Skipping extract macro columns.");
                    return;
                }
                MacroIndicators.WriteSidecarAndSeries(fetch, sidecarCsv, seriesDir);
                Console.WriteLine($"Wrote sidecar {sidecarCsv} and {fetch.Series.Count} series under {seriesDir}");
                foreach (var missing in fetch.MissingKeys)
                {
                    Console.WriteLine($"Indicator not found (404): {missing}");
                }
                foreach (var empty in fetch.EmptyKeys)
                {
                    Console.WriteLine($"Indicator returned empty: {empty}");
                }
                var join = MacroIndicators.JoinOntoExtract(extractCsv, fetch);
                Console.WriteLine($"Joined macro columns [{string.Join(", ", join.ColumnsAdded)}] onto {extractCsv}; rows={join.DataRows}");
                return;
            }

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
                "IRX.INDX",
                "FVX.INDX",
                "TYX.INDX",
                "MOVE.INDX",
                "DXY.INDX",
                "N225.INDX",
                "GDAXI.INDX",
                "XAU.INDX",
                "BCOMCL.INDX",
                "S5TH.INDX",
                "S5TW.INDX",
                "ADVN.INDX",
                "ADVQ.INDX",
                "DECN.INDX",
                "DECQ.INDX",
                "NDTH.INDX",
                "NDTW.INDX",
                "NDFI.INDX",
                "S5FI.INDX",
                "SKEW.INDX",
                "VVIX.INDX",
                "VIX3M.INDX",
                "VIX9D.INDX",
                "VIX1Y.INDX",
                "VIX6M.INDX",
                "MAHN.INDX",
                "MALN.INDX",
                "TRIN.INDX",
                "QQQ.US",
                "HYG.US",
                "LQD.US",
                "TICK.INDX",
                "IWM.US",
                "SMH.US",
                "US2Y.INDX",
                "XLF.US",
                "XLE.US"
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

                // Recode MAHN/MALN vendor 0.01 count floor to 0.0, then fill blanks and write.
                var table = tableBuilder.Table;
                ExtractUtil.RecodeCountFloorToZero(table, ["mahn", "maln"]);
                ExtractUtil.FillInTable<double>(table);
                table.WriteCsv(extractCsv, CsvHeaderType.Header);
            }

            if (DoWriteSymbolsALL)
            {
                // Write out symbol file
                var symbolsColl = db.GetCollection<MarketApi.ExchangeSymbol>("exchange_symbols");
                var results = symbolsColl.FindAll().ToList();
                results.ForEach(x => { x.Country = (string.IsNullOrEmpty(x.Country) ? "" : x.Country); x.Name = (string.IsNullOrEmpty(x.Name) ? "" : x.Name); });
                var table = results.ToTable();
                table.WriteCsv(symbolsCsv, CsvHeaderType.Header);
            }

            if (DoWriteSymbolsSPX)
            {
                // Write out symbol file
                var results = await GetIndexSymbols(db, "GSPC.INDX");
                var table = results.ToTable();
                table.WriteCsv(spxSymbolsCsv, CsvHeaderType.Header);
            }

            if (DoWriteEventFlags)
            {
                await EventCalendar.WriteEventFlagsAsync(eodHdNew, extractCsv, eventsCsv, eventTypesCsv);
            }

            // Clean up
            db.Dispose();
        }

        static string EnvOrDefault(string name, string fallback)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }
            return value;
        }

        static string RequireApiToken()
        {
            var apiToken = Environment.GetEnvironmentVariable("EODHD_API_TOKEN");
            if (string.IsNullOrWhiteSpace(apiToken))
            {
                throw new InvalidOperationException(
                    "EODHD API token is not set. Set the EODHD_API_TOKEN environment variable before running.");
            }
            return apiToken;
        }

        static bool HasFlag(string[] args, string flag)
        {
            return args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));
        }

        static List<string>? ParseCsvListArg(string[] args, string flag)
        {
            var prefix = flag + "=";
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                string? value = null;
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    value = arg[prefix.Length..];
                }
                else if (string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new InvalidOperationException($"{flag} requires a comma-separated symbol list.");
                    }
                    value = args[i + 1];
                }
                if (value == null)
                {
                    continue;
                }
                var symbols = value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
                if (symbols.Count == 0)
                {
                    throw new InvalidOperationException($"{flag} requires at least one symbol.");
                }
                return symbols;
            }
            return null;
        }

        static async Task<(int Affected, string UsedKey, string Note)> TryLoadPricesWithUsFallback(
            LiteDatabase db, EodNetApi.Client eodHd, string symbol)
        {
            var (affected, err) = await TryLoadOneSymbol(db, eodHd, symbol);
            if (err == null && affected > 0)
            {
                return (affected, symbol, $"Loaded {symbol}: {affected} bars");
            }
            err ??= "empty series";

            var alt = AlternateUsCatalogKey(db, symbol);
            if (alt != null)
            {
                Console.WriteLine($"{symbol} failed ({err}); retrying {alt}");
                var (altAffected, altErr) = await TryLoadOneSymbol(db, eodHd, alt);
                if (altErr == null && altAffected > 0)
                {
                    return (altAffected, alt,
                        $"Loaded {alt}: {altAffected} bars (fallback from {symbol}: {err})");
                }
                return (0, symbol,
                    $"Failed {symbol} ({err}) and catalog key {alt} ({altErr ?? "empty series"})");
            }
            return (0, symbol, $"Failed {symbol}: {err}");
        }

        static string? AlternateUsCatalogKey(LiteDatabase db, string exchangeSymbolId)
        {
            // HYG.US / LQD.US / IWM.US / SMH.US / XLF.US / XLE.US -> catalog exchange (NYSE ARCA / NASDAQ). Never map MOVE.INDX to MOVE.NASDAQ or TICK.INDX to a NASDAQ TICK.
            var parts = exchangeSymbolId.Split('.', 2);
            if (parts.Length != 2 || !string.Equals(parts[1], "US", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            var ticker = parts[0];
            var coll = db.GetCollection<MarketApi.ExchangeSymbol>("exchange_symbols");
            foreach (var match in coll.Find(x => x.Symbol == ticker))
            {
                var key = $"{match.Symbol}.{match.Exchange}";
                if (!string.Equals(key, exchangeSymbolId, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(match.Exchange, "US", StringComparison.OrdinalIgnoreCase))
                {
                    return key;
                }
            }
            return null;
        }

        static async Task<(int Affected, string? Error)> TryLoadOneSymbol(
            LiteDatabase db, EodNetApi.Client eodHd, string symbol)
        {
            try
            {
                var affected = await LoadEndOfDayHistoricalStockPrice(
                    db, eodHd, DateTime.Now.AddDays(-10000), DateTime.Now, new List<string> { symbol });
                return (affected, null);
            }
            catch (Exception ex)
            {
                return (0, SummarizeLoadError(ex));
            }
        }

        static string SummarizeLoadError(Exception ex)
        {
            var msg = ex.Message ?? string.Empty;
            if (msg.Contains("api_token", StringComparison.OrdinalIgnoreCase))
            {
                return "request failed (details omitted)";
            }
            if (msg.Length > 240)
            {
                msg = msg[..240];
            }
            return msg;
        }
    }
}
