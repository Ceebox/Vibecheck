using System.CommandLine;
using Vibecheck.Utility;

namespace Vibecheck
{
    /// <summary>
    /// Entry point for the Vibecheck CLI interface.
    /// </summary>
    internal sealed class Program
    {
        // https://budavariam.github.io/asciiart-text/ (DOS Rebel)
        private const string LOGO = """
 █████   █████  ███  █████                       █████                        █████     
░░███   ░░███  ░░░  ░░███                       ░░███                        ░░███      
 ░███    ░███  ████  ░███████   ██████   ██████  ░███████    ██████   ██████  ░███ █████
 ░███    ░███ ░░███  ░███░░███ ███░░███ ███░░███ ░███░░███  ███░░███ ███░░███ ░███░░███ 
 ░░███   ███   ░███  ░███ ░███░███████ ░███ ░░░  ░███ ░███ ░███████ ░███ ░░░  ░██████░  
  ░░░█████░    ░███  ░███ ░███░███░░░  ░███  ███ ░███ ░███ ░███░░░  ░███  ███ ░███░░███ 
    ░░███      █████ ████████ ░░██████ ░░██████  ████ █████░░██████ ░░██████  ████ █████
     ░░░      ░░░░░ ░░░░░░░░   ░░░░░░   ░░░░░░  ░░░░ ░░░░░  ░░░░░░   ░░░░░░  ░░░░ ░░░░░ 
""";

        public static async Task<int> Main(string[] args)
        {
            Tracing.InitialiseTelemetry(new Uri("http://localhost:4317/"));
            Console.WriteLine(LOGO + '\n');

            return await Run(args);
        }

        public static async Task<int> Run(string[] args)
        {
            var rootCommand = new RootCommand("Vibecheck CLI")
            {
                new ReviewCommand(),
                new ChatCommand(),
                new ServerCommand(),
                new DebugCommand()
            };

            if (args.Length == 0)
            {
                return await rootCommand.Parse(["-h"]).InvokeAsync();
            }

            return await rootCommand.Parse(args).InvokeAsync();
        }
    }
}
