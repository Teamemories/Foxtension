using System;
using System.Diagnostics;
using System.Globalization;

namespace Foxtension.Software.Graphic
{
    public enum PrintOption
    {
        Normal,
        DateTime,
        Logger,
        DateTimeLogger
    }

    public enum PrintFormat
    {
        Message,
        Hint,
        Info,
        Log,
        Warning,
        Error,
        Success
    }

    public sealed class Board
    {
        public static void Print(PrintFormat format, string context, PrintOption options = PrintOption.Normal)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var color = format switch
            {
                PrintFormat.Message => ConsoleColor.White,
                PrintFormat.Hint => ConsoleColor.Cyan,
                PrintFormat.Info => ConsoleColor.Blue,
                PrintFormat.Log => ConsoleColor.Magenta,
                PrintFormat.Warning => ConsoleColor.Yellow,
                PrintFormat.Error => ConsoleColor.Red,
                PrintFormat.Success => ConsoleColor.Green,
                _ => ConsoleColor.White
            };

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            string output = options switch
            {
                PrintOption.Normal => context,
                PrintOption.DateTime => $"[{timestamp}] ~> {context}",
                PrintOption.Logger => context,
                PrintOption.DateTimeLogger => $"[{timestamp}] ~> {context}",
                _ => throw new ArgumentOutOfRangeException(nameof(options), "Invalid PrintOption value.")
            };

            try
            {
                if (options == PrintOption.Logger || options == PrintOption.DateTimeLogger)
                {
                    Debug.WriteLine(output);
                    Console.ForegroundColor = color;
                    Console.WriteLine(output);
                }
                else
                {
                    Console.ForegroundColor = color;
                    Console.WriteLine(output);
                }
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}