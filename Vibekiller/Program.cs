using System.CommandLine;
using Vibekiller.Utility;
using Vibekiller.Engine;
using Vibekiller.Settings;

namespace Vibekiller
{
    /// <summary>
    /// Entry point for the Vibekiller CLI interface.
    /// </summary>
    internal sealed class Program
    {
        private const string LOGO = """
             █████   █████ ████████             █████      ███████ ████                    
            ░░███   ░░███ ░░░░░███             ░░███      ░░░░░███░░███                    
             ░███    ░███ ████░███████   ██████ ░███ █████████░███ ░███   ██████  ████████ 
             ░███    ░███░░███░███░░███ ███░░███░███░░███░░███░███ ░███  ███░░███░░███░░███
             ░░███   ███  ░███░███ ░███░███████ ░██████░  ░███░███ ░███ ░███████  ░███ ░░░ 
              ░░░█████░   ░███░███ ░███░███░░░  ░███░░███ ░███░███ ░███ ░███░░░   ░███     
                ░░███     ████████████ ░░██████ ████ ███████████████████░░██████  █████    
                 ░░░     ░░░░░░░░░░░░   ░░░░░░ ░░░░ ░░░░░░░░░░░░░░░░░░░  ░░░░░░  ░░░░░                                                                       
            """;

        public static async Task<int> Main(string[] args)
        {
            Tracing.InitialiseTelemetry(new Uri("http://[::1]:4317/"));
            Console.WriteLine(LOGO);

            var rootCommand = new RootCommand("Vibekiller CLI");
            var reviewCommand = new Command("review", "Review some code.");
            var debugCommand = new Command("debug", "Enter a debug mode.");
            debugCommand.SetAction(async _ =>
            {
                var engine = new ReviewEngine(Configuration.Current.InferenceSettings.ModelUrl);
                await engine.Run();
            });

            rootCommand.Add(reviewCommand);
            rootCommand.Add(debugCommand);

            if (args.Length == 0)
            {
                return await rootCommand.Parse(["-h"]).InvokeAsync();
            }

            return await rootCommand.Parse(args).InvokeAsync();
        }
    }
}
