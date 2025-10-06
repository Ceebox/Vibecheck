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

            var pathOption = new Option<string>("--path")
            {
                Description = "The path of the target git repository.",
                DefaultValueFactory = _ => string.Empty
            };

            var targetOption = new Option<string>("--target")
            {
                Description = "The branch into which the reviewed changes are intented to be merged.",
                DefaultValueFactory = _ => string.Empty
            };

            reviewCommand.Options.Add(pathOption);
            reviewCommand.Options.Add(targetOption);

            reviewCommand.SetAction(async parsedArgs =>
            {
                // If the repo path is empty, we will use the working directory
                var repoPath = parsedArgs.GetValue(pathOption);

                var target = parsedArgs.GetValue(targetOption);
                if (string.IsNullOrEmpty(target))
                {
                    target = Configuration.Current.GitSettings.GitTargetBranch;
                }

                var engine = new ReviewEngine(Configuration.Current.InferenceSettings.ModelUrl);
                await engine.Run();
            });

            rootCommand.Add(reviewCommand);

            if (args.Length == 0)
            {
                return await rootCommand.Parse(["-h"]).InvokeAsync();
            }

            return await rootCommand.Parse(args).InvokeAsync();
        }
    }
}
