using System.CommandLine;
using System.Reflection;
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
            var rootCommand = CommandHelpers.CreatRootCommand();
            if (args.Length == 0)
            {
                // We might need help!
                args = ["--help"];
            }

            return await rootCommand.Parse(args).InvokeAsync();
        }
    }
}
