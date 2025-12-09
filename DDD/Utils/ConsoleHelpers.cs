using System;

public static class ConsoleHelpers
{
    // Wrap text at console width
    public static void WrapAndPrint(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine();
            return;
        }

        int width = Console.WindowWidth - 2;
        string[] words = text.Split(' ');

        string line = "";
        foreach (var w in words)
        {
            if ((line + w).Length > width)
            {
                Console.WriteLine(line);
                line = "";
            }
            line += w + " ";
        }
        if (!string.IsNullOrWhiteSpace(line))
            Console.WriteLine(line.TrimEnd());
    }
}
public static class TimeHelpers
{
    public static string ToRelative(DateTime dt)
    {
        var diff = DateTime.UtcNow - dt;

        if (diff.TotalSeconds < 60)
            return $"{(int)diff.TotalSeconds}s ago";

        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";

        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";

        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}d ago";

        return dt.ToString("yyyy-MM-dd");
    }
}