using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Vibekiller.Utility
{
    public static class Tracing
    {
        private const string APP_NAME = "Vibekiller";
        private static readonly ActivitySource sActivitySource = new(APP_NAME);
        private static TracerProvider? sTracerProvider = null;
        private static bool sDebug = false;

        public static void InitialiseTelemetry(Uri? endpoint = null)
        {
            var tracerBuilder = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(APP_NAME))
                .AddSource(sActivitySource.Name);

            if (endpoint == null)
            {
                tracerBuilder.AddConsoleExporter();
            }
            else
            {
                tracerBuilder.AddOtlpExporter(options =>
                {
                    options.Endpoint = endpoint;
                    options.Protocol = OtlpExportProtocol.Grpc;
                });
            }

            sTracerProvider = tracerBuilder.Build();
        }

        public static Activity Start([CallerMemberName] string memberName = "")
        {
            // We should always have an activity listener, if we've not forgot to start the telemetry
            return sActivitySource.StartActivity(memberName)!;
        }

        public static void Write(object message, LogLevel level)
        {
            using var activity = Start();
            activity.AddTag("debug.message", message);
            activity.AddTag("debug.level", level);

            if (sDebug)
            {
                ConsoleHelpers.PrettyPrint(message, level);
            }
        }

        public static void WriteLine(object message, LogLevel level)
        {
            using var activity = Start();
            activity.AddTag("debug.message", message);
            activity.AddTag("debug.level", level);

            if (sDebug)
            {
                ConsoleHelpers.PrettyPrintLine(message, level);
            }
        }

        public static void SetDebug()
        {
            sDebug = true;
        }

        public static bool Debug
            => sDebug;
    }
}
