using System;

namespace DDD.Utils
{
    public static class InputHelper
    {
        public static int GetInt(string prompt)
        {
            Console.Write(prompt);
            int result;
            while (!int.TryParse(Console.ReadLine(), out result))
            {
                Console.Write("Invalid input. Please enter a number: ");
            }
            return result;
        }

        public static string GetString(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine() ?? string.Empty;
        }

        public static DateTime GetDateTime(string prompt)
        {
            Console.Write(prompt);
            DateTime result;
            while (!DateTime.TryParse(Console.ReadLine(), out result))
            {
                Console.Write("Invalid date/time format. Try again (e.g. 2025-11-26 14:30): ");
            }
            return result;
        }

        public static void PressEnterToContinue()
        {
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
        }
    }
}
