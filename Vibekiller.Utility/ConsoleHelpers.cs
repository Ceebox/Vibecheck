namespace Vibekiller.Utility;
internal static class ConsoleHelpers
{
    public static void PrettyPrint(object message, LogLevel logLevel)
    {
        var originalColor = Console.ForegroundColor;

        Console.ForegroundColor = logLevel switch
        {
            LogLevel.DEBUG => ConsoleColor.Gray,
            LogLevel.NONE => ConsoleColor.White,
            LogLevel.INFO => ConsoleColor.Blue,
            LogLevel.SUCCESS => ConsoleColor.Green,
            LogLevel.WARNING => ConsoleColor.Yellow,
            LogLevel.ERROR => ConsoleColor.Red,
            _ => ConsoleColor.White,
        };

        Console.Write(message);
        Console.ForegroundColor = originalColor;
    }

    public static void PrettyPrintLine(object message, LogLevel logLevel)
    {
        var originalColor = Console.ForegroundColor;

        Console.ForegroundColor = logLevel switch
        {
            LogLevel.DEBUG => ConsoleColor.Gray,
            LogLevel.NONE => ConsoleColor.White,
            LogLevel.INFO => ConsoleColor.Blue,
            LogLevel.SUCCESS => ConsoleColor.Green,
            LogLevel.WARNING => ConsoleColor.Yellow,
            LogLevel.ERROR => ConsoleColor.Red,
            _ => ConsoleColor.White,
        };

        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }
}
