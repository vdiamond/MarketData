using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Table;

namespace eodhd_download
{
    static public class ExtractUtil
    {
        static public IDynamicTable CreateDateRangeTable(DateOnly from, DateOnly to, string columnName, params DayOfWeek[] daysOfWeek)
        {
            var ret = new DynamicTable();
            var col = ret.columns.NewColumn(columnName, TypeCode.String);
            var curr = from;
            var time = new TimeOnly(0, 0, 0);   
            while (curr <= to)
            {
                if (daysOfWeek.Length == 0 || daysOfWeek.Contains(curr.DayOfWeek))
                {
                    var row = ret.rows.NewRow();
                    row.SetField<String>(columnName, curr.ToDateTime(time).ToString("yyyy-MM-dd"));
                }
                curr = curr.AddDays(1);
            }
            return ret;
        }

        static public string ColumnPrefixFromExchangeSymbolId(string exchangeSymbolId)
        {
            if (string.IsNullOrWhiteSpace(exchangeSymbolId))
            {
                throw new Exception($"Invalid format of symbol ({exchangeSymbolId}) should be format 'sym.exchng'");
            }
            var splitSym = exchangeSymbolId.Split('.');
            if (splitSym.Length != 2)
            {
                throw new Exception($"Invalid format of symbol ({exchangeSymbolId}) should be format 'sym.exchng'");
            }
            return splitSym[0].ToLower();
        }

        static public void FillIn<T>(IList<T> list) where T : IEquatable<T>
        {
            if (typeof(T) == typeof(double))
            {
                FillInDouble((IList<double>)(object)list);
                return;
            }

            T lastValue = default(T);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(default(T)))
                {
                    list[i] = lastValue;
                }
                lastValue = list[i];
            }
        }

        static void FillInDouble(IList<double> list)
        {
            double lastValue = double.NaN;
            for (int i = 0; i < list.Count; i++)
            {
                if (double.IsNaN(list[i]))
                {
                    list[i] = lastValue;
                }
                else
                {
                    lastValue = list[i];
                }
            }
        }

        static public void FillInTable<T>(IDynamicTable table) where T : IEquatable<T>
        {
            foreach (var col in table.columns)
            {
                if (col.TypeCode == Type.GetTypeCode(typeof(T)))
                {
                    FillIn<T>((IList<T>)col);
                }
             }
        }

        /// <summary>
        /// Recodes a vendor count-index floor (0.01) to 0.0 on matching prefix OHLC columns.
        /// Volume is left unchanged. NaN is left unchanged. Real 0.0 is already the empty count.
        /// </summary>
        static public void RecodeCountFloorToZero(IDynamicTable table, IReadOnlyCollection<string> prefixes, double floor = CountIndexZeroFloor)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(prefixes);

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prefix in prefixes)
            {
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    continue;
                }
                foreach (var field in CountIndexOhlcFields)
                {
                    names.Add($"{prefix}.{field}");
                }
            }

            foreach (var col in table.columns)
            {
                if (col.TypeCode != TypeCode.Double || !names.Contains(col.Name))
                {
                    continue;
                }
                var values = (IList<double>)col;
                for (int i = 0; i < values.Count; i++)
                {
                    if (!double.IsNaN(values[i]) && Math.Abs(values[i] - floor) <= CountIndexFloorTolerance)
                    {
                        values[i] = 0.0;
                    }
                }
            }
        }

        public const double CountIndexZeroFloor = 0.01;

        static readonly string[] CountIndexOhlcFields = ["Open", "High", "Low", "Close"];
        const double CountIndexFloorTolerance = 1e-9;
    }
}