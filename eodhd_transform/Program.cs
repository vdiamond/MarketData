using System.Globalization;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;
using Table;
using TableCsv;
using TableExtension;


using TALib;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace eodhd_transform
{
    internal class Program
    {
        static IDynamicTable LoadDateDoublesCsv(string filePath, string dateFieldName = "Date", TypeCode dateFieldType = TypeCode.String, TypeCode defaultFieldType = TypeCode.Double)
        {
            var attr = new DefaultFieldAttributes();
            attr.SetDefault(defaultFieldType);
            attr.Add(new Field(dateFieldName, dateFieldType, true));
            IDynamicTable table = new DynamicTable();
            table.LoadCsv(filePath, CsvHeaderType.Header, null, attr);
            return table;
        }

        static DateTime toDateTime(string dateString, int hours = 16, int minutes = 0, int seconds = 0)
        {
            var date = DateTime.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            DateTime result = new DateTime(date.Year, date.Month, date.Day, hours, minutes, seconds);
            return result;
        }

        static void Main(string[] args)
        {

            //
            // Load Csv - convert all fields to the correct types
            var extractTable = LoadDateDoublesCsv(@"d:\predict\index_extract.csv");

            //
            // Source field definitions
            var Date = extractTable.AsList<string>("Date");
            var qqqOpen = extractTable.AsList<double>("qqq.Open");
            var qqqLow = extractTable.AsList<double>("qqq.Low");
            var qqqHigh = extractTable.AsList<double>("qqq.High");
            var qqq = extractTable.AsList<double>("qqq.Close");
            var qqqVolume = extractTable.AsList<double>("qqq.Volume");

            //
            // Target field definitions
            var qqq_open = extractTable.columns.NewColumAsList<double>("qqq_open");
            var qqq_low = extractTable.columns.NewColumAsList<double>("qqq_low");
            var qqq_high = extractTable.columns.NewColumAsList<double>("qqq_high");
            var qqq_close = extractTable.columns.NewColumAsList<double>("qqq_close");
            var qqq_vol = extractTable.columns.NewColumAsList<double>("qqq_vol");

            var qqq_ema9 = extractTable.columns.NewColumAsList<double>("qqq_ema9");
            var qqq_ema20 = extractTable.columns.NewColumAsList<double>("qqq_ema20");
            var qqq_ema50 = extractTable.columns.NewColumAsList<double>("qqq_ema50");

            var qqq_slope9 = extractTable.columns.NewColumAsList<double>("qqq_slope9");
            var qqq_slope20 = extractTable.columns.NewColumAsList<double>("qqq_slope20");
            var qqq_slope50 = extractTable.columns.NewColumAsList<double>("qqq_slope50");

            var qqq_rsi14 = extractTable.columns.NewColumAsList<double>("qqq_rsi14");
            var qqq_adx14 = extractTable.columns.NewColumAsList<double>("qqq_adx14");

            var qqq_volatility20 = extractTable.columns.NewColumAsList<double>("qqq_volatility20");
 
            var qqq_macd_hist26_12_9 = extractTable.columns.NewColumAsList<double>("qqq_macd_hist26_12_9");
            var qqq_macd_signal26_12_9 = extractTable.columns.NewColumAsList<double>("qqq_macd_signal26_12_9");

            var qqq_bollinger_width20_2 = extractTable.columns.NewColumAsList<double>("qqq_bollinger_width20_2");
            var qqq_bollinger_pos20_2 = extractTable.columns.NewColumAsList<double>("qqq_bollinger_pos20_2");

            var qqq_support = extractTable.columns.NewColumAsList<double>("qqq_support");
            var qqq_support_strength = extractTable.columns.NewColumAsList<double>("qqq_support_strength");
 
            var qqq_resistance = extractTable.columns.NewColumAsList<double>("qqq_resistance");
            var qqq_resistance_strength = extractTable.columns.NewColumAsList<double>("qqq_resistance_strength");

            var qqq_yield = extractTable.columns.NewColumAsList<double>("qqq_yield");   
            var qqq_yield_la1 = extractTable.columns.NewColumAsList<double>("qqq_yield_la1");

            // populate extracted fields
            qqqOpen.nop(qqq_open);
            qqqLow.nop(qqq_low);
            qqqHigh.nop(qqq_high);
            qqq.nop(qqq_close);
            qqqVolume.nop(qqq_vol);

            qqq.ema(9, qqq_ema9);
            qqq.ema(20, qqq_ema20); 
            qqq.ema(50, qqq_ema50);

            qqq.slope(9, qqq_slope9);
            qqq.slope(20, qqq_slope20);
            qqq.slope(50, qqq_slope50);

            qqq.wilders_rsi(14, qqq_rsi14);
            qqq.wilders_adx(qqqLow, qqqHigh, 14, qqq_adx14);

            qqq.volatility(20, qqq_volatility20);

            qqq.macd_hist(26, 12, 9, qqq_macd_hist26_12_9);
            qqq.macd_signal(26, 12, 9, qqq_macd_signal26_12_9);

            qqq.bollinger_bands_width(20, 2, qqq_bollinger_width20_2);
            qqq.bollinger_bands_pctb(20, 2, qqq_bollinger_pos20_2);

            var priceLevel = new PriceLevels(1.0, 1);
            for (int i = 0; i < Date.Count; i++)
            {
                var dt = toDateTime(Date[i]);
                priceLevel.LogBar(dt, qqqOpen[i], qqqHigh[i], qqqLow[i], qqq[i], qqqVolume[i]);

                qqq_resistance[i] = 0.0;
                qqq_resistance_strength[i] = 0.0;
                if (priceLevel.GetResistance(dt, qqq_close[i], 90, out double resistance_strength, out double resistance))
                {
                    qqq_resistance[i] = resistance;
                    qqq_resistance_strength[i] = resistance_strength;
                }

                qqq_support[i] = 0.0;
                qqq_support_strength[i] = 0.0;
                if (priceLevel.GetSupport(dt, qqq_close[i], 90, out double support_strength, out double support))
                {
                    qqq_support[i] = support;
                    qqq_support_strength[i] = support_strength;
                }
            }

            qqq.yield(1).mul(100.0, qqq_yield);
            qqq_yield.offset(-1, qqq_yield_la1);

            //
            // take out initial 50 rows
            extractTable.rows.RemoveRows(0, 50);

            //
            // Remove extract columns
            extractTable.columns.TrimToInclude(
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
            );

            extractTable.WriteCsv(@"d:\predict\index_extract_ta.csv", CsvHeaderType.Header);
         }


    }
}
