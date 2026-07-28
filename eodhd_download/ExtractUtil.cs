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

        static public void FillIn<T>(IList<T> list) where T : IEquatable<T>
        {
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
    }
}
