using System.Diagnostics;

namespace Vibekiller.Utility
{
    public static class ActivityExtensions
    {
        public static void Log(this Activity activity, object message)
            => activity.Log(message, LogLevel.NONE);

        public static void Log(this Activity activity, Exception message)
            => activity.Log(message, LogLevel.ERROR);

        public static void Log(this Activity activity, object message, LogLevel logLevel)
        {
            if (message == null || string.IsNullOrEmpty(message.ToString()))
            {
                return;
            }

            var output = message.ToString()!;
            switch (logLevel)
            {
                case LogLevel.WARNING:
                    activity.AddWarning(output);
                    break;

                case LogLevel.ERROR:
                    activity.AddError(output);
                    break;

                case LogLevel.DEBUG:
                case LogLevel.NONE:
                case LogLevel.INFO:
                default:
                    activity.SetStatus(ActivityStatusCode.Ok);
                    activity.AddEvent(new ActivityEvent(output));
                    break;
            }

            PrettyPrint(output, logLevel);
        }

        public static void AddWarning(this Activity activity, object message)
        {
            activity.SetStatus(ActivityStatusCode.Ok);
            activity.SetTag("warning", true);
            activity.AddEvent(new ActivityEvent("Warning: " + message.ToString()));
        }

        public static void AddError(this Activity activity, object message)
        {
            if (message is Exception error)
            {
                activity.AddException(error);
            }
            else
            {
                var output = message.ToString();
                activity.SetStatus(ActivityStatusCode.Error, output);
                activity.SetTag("error", true);
                activity.AddEvent(new ActivityEvent("Error: " + output));
            }
        }

        private static void PrettyPrint(object message, LogLevel logLevel)
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
}
