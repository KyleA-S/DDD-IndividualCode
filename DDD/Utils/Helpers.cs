using System;
using System.Collections.Generic;
using System.Text;

namespace DDD.Utils
{
    public static class ConsoleHelpers
    {
        // wrap and print text to console at a given width
        public static void WrapAndPrint(string text, int width = 80)
        {
            if (string.IsNullOrEmpty(text))
            {
                Console.WriteLine();
                return;
            }

            var words = text.Split(' ');
            var line = new StringBuilder();
            foreach (var w in words)
            {
                if (line.Length + w.Length + 1 > width)
                {
                    Console.WriteLine(line.ToString().TrimEnd());
                    line.Clear();
                }
                line.Append(w).Append(' ');
            }
            if (line.Length > 0) Console.WriteLine(line.ToString().TrimEnd());
        }
    }

    public static class TimeHelpers
    {
        public static string ToRelative(DateTime dt)
        {
            var ts = DateTime.UtcNow - dt.ToUniversalTime();
            if (ts.TotalSeconds < 60) return $"{Math.Floor(ts.TotalSeconds)}s ago";
            if (ts.TotalMinutes < 60) return $"{Math.Floor(ts.TotalMinutes)}m ago";
            if (ts.TotalHours < 24) return $"{Math.Floor(ts.TotalHours)}h ago";
            if (ts.TotalDays < 7) return $"{Math.Floor(ts.TotalDays)}d ago";
            if (ts.TotalDays < 30) return $"{Math.Floor(ts.TotalDays / 7)}w ago";
            if (ts.TotalDays < 365) return $"{Math.Floor(ts.TotalDays / 30)}mo ago";
            return $"{Math.Floor(ts.TotalDays / 365)}y ago";
        }
    }
}
