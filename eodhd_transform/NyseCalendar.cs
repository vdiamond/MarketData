using System.Globalization;

namespace eodhd_transform;

/// <summary>
/// NYSE full-session closures. Used to drop EODHD holiday fill rows before session-to-session yields.
/// </summary>
internal static class NyseCalendar
{
    static readonly HashSet<DateOnly> ExtraClosures =
    [
        new DateOnly(2018, 12, 5),
        new DateOnly(2025, 1, 9)
    ];

    public static bool IsFullSessionHoliday(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        if (ExtraClosures.Contains(date))
        {
            return true;
        }

        return HolidaysInYear(date.Year).Contains(date)
            || HolidaysInYear(date.Year + 1).Contains(date);
    }

    public static HashSet<DateOnly> HolidaysInYear(int year)
    {
        var holidays = new HashSet<DateOnly>();
        AddObserved(holidays, year, 1, 1);
        holidays.Add(NthWeekdayOfMonth(year, 1, DayOfWeek.Monday, 3));
        holidays.Add(NthWeekdayOfMonth(year, 2, DayOfWeek.Monday, 3));
        holidays.Add(GoodFriday(year));
        holidays.Add(LastWeekdayOfMonth(year, 5, DayOfWeek.Monday));
        if (year >= 2022)
        {
            AddObserved(holidays, year, 6, 19);
        }
        AddObserved(holidays, year, 7, 4);
        holidays.Add(NthWeekdayOfMonth(year, 9, DayOfWeek.Monday, 1));
        holidays.Add(NthWeekdayOfMonth(year, 11, DayOfWeek.Thursday, 4));
        AddObserved(holidays, year, 12, 25);
        return holidays;
    }

    static void AddObserved(HashSet<DateOnly> holidays, int year, int month, int day)
    {
        var date = new DateOnly(year, month, day);
        if (date.DayOfWeek == DayOfWeek.Saturday)
        {
            date = date.AddDays(-1);
        }
        else if (date.DayOfWeek == DayOfWeek.Sunday)
        {
            date = date.AddDays(1);
        }

        holidays.Add(date);
    }

    static DateOnly NthWeekdayOfMonth(int year, int month, DayOfWeek weekday, int n)
    {
        var date = new DateOnly(year, month, 1);
        while (date.DayOfWeek != weekday)
        {
            date = date.AddDays(1);
        }

        return date.AddDays(7 * (n - 1));
    }

    static DateOnly LastWeekdayOfMonth(int year, int month, DayOfWeek weekday)
    {
        var date = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        while (date.DayOfWeek != weekday)
        {
            date = date.AddDays(-1);
        }

        return date;
    }

    static DateOnly GoodFriday(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateOnly(year, month, day).AddDays(-2);
    }
}
