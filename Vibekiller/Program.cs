using System.CommandLine;
using Vibekiller.Utility;
using Vibekiller.Engine;

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

        private const string MODEL_URL = "https://huggingface.co/ibm-granite/granite-4.0-micro-GGUF/resolve/main/granite-4.0-micro-Q4_K_M.gguf";

        public static async Task<int> Main(string[] args)
        {
            Tracing.InitialiseTelemetry(new Uri("http://[::1]:4317/"));
            Console.WriteLine(LOGO);

            var rootCommand = new RootCommand("Vibekiller CLI");
            var reviewCommand = new Command("review", "Review some code.");
            var debugCommand = new Command("debug", "Enter a debug mode.");
            debugCommand.SetAction(async _ =>
            {
                var engine = new VibeEngine(MODEL_URL);
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
