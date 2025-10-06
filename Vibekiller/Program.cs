using System.CommandLine;
using Vibekiller.Utility;

namespace Vibekiller
{
    /// <summary>
    /// Entry point for the Vibekiller CLI interface.
    /// </summary>
    internal sealed class Program
    {
        private const string LOGO = """
            ░█████  ░█████░███░████            ░█████     ░███ ░███ ░███                   
            ░░███   ░░███  ░░░░███              ░███       ░░░ ░███ ░███                   
             ░███    ░███░████░███████  ░██████ ░███ █████████ ░███ ░███ ░██████ ░████████ 
             ░███    ░███ ░███░███ ░███░███░░███░███░░███░░███ ░███ ░███░███░░███░░███░░███
             ░░███   ███  ░███░███ ░███░███████ ░██████░  ░███ ░███ ░███░███████  ░███ ░░░ 
              ░░░█████░   ░███░███ ░███░███░░░  ░███░░███ ░███ ░███ ░███░███░░░   ░███     
                ░░███    ░████████████ ░░██████ ████ ███████████████████░░██████ ░█████    
            """;

        public static async Task<int> Main(string[] args)
        {
            Tracing.InitialiseTelemetry(new Uri("http://localhost:4317/"));
            Console.WriteLine(LOGO + '\n');

            return await Run(args);
        }

        private static async Task<int> Run(string[] args)
        {
            var rootCommand = new RootCommand("Vibekiller CLI");
            var reviewCommand = new Command("review", "Review some code.");

            rootCommand.Add(new ReviewCommand());

            var debugCommand = new Command("debug", "Enter development mode.");
            debugCommand.SetAction(async _ =>
            {
                await rootCommand.Parse(["-h"]).InvokeAsync();

                Console.Write("\nEnter command: ");
                var newLine = Console.ReadLine();
                if (string.IsNullOrEmpty(newLine))
                {
                    return;
                }

                var newArgs = newLine.Split(" ");
                await Run(newArgs);
            });

            rootCommand.Add(debugCommand);

            if (args.Length == 0)
            {
                return await rootCommand.Parse(["-h"]).InvokeAsync();
            }

            return await rootCommand.Parse(args).InvokeAsync();
        }
    }
}
