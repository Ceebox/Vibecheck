using System.CommandLine;
using Vibecheck.Git;
using Vibecheck.Inference;
using Vibecheck.Settings;
using Vibecheck.Utility;

namespace Vibecheck;

/// <summary>
/// A smart person would call this the "vectorise" command but some people can't agree on spelling.
/// </summary>
internal sealed class VectorifyCommand : CommandBase
{
    public override Command ToCommand()
    {
        using var activity = Tracing.Start();

        var pathArgument = new Argument<string>("path")
        {
            Description = "The path of the repository to vectorise.",
            DefaultValueFactory = _ => Directory.GetCurrentDirectory(),
        };

        var cmd = new Command("vectorify", "Convert a specified codebase into a vector database to improve AI suggestions.")
        {
            pathArgument,
        };

        cmd.Aliases.Add("vectorise");
        cmd.Aliases.Add("vectorize"); // Darn Americans! (yee haw)

        cmd.SetAction(async parsedArgs =>
        {
            var repositoryPath = parsedArgs.GetValue(pathArgument);
            if (string.IsNullOrEmpty(repositoryPath))
            {
                Tracing.WriteLine("The repository path cannot be empty", LogLevel.ERROR);
                return;
            }

            await RunAsync(repositoryPath);
        });

        return cmd;
    }

    private static async Task RunAsync(
        string repositoryPath
    )
    {
        using var activity = Tracing.Start();

        var codeRoot = RepositoryFinder.Discover(repositoryPath).CodeRoot;
        if (string.IsNullOrEmpty(codeRoot))
        {
            return;
        }

        var modelData = await InferenceEngineFactory.LoadModelDataAsync(Configuration.Current.InferenceSettings.ModelUrl);
        var dbManager = new FolderIndexer(codeRoot, modelData);
        await dbManager.VectoriseAsync();
    }
}
