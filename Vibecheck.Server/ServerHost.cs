using Vibecheck.Engine;
using Vibecheck.Git;
using Vibecheck.Settings;
using Vibecheck.Utility;

namespace Vibecheck.Server;

public class ServerHost
{
    public async Task Run(int? port, string[]? args)
    {
        using var activity = Tracing.Start();

        args ??= [];
        port ??= Configuration.Current.ServerSettings.Port;

        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapPost("/api/v1/diff", async context =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var diffText = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(diffText))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Empty diff.");
                return;
            }

            var patchGenerator = new TextPatchSource(diffText);
            if (!patchGenerator.GetPatchInfo().Any())
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("No patches found.");
                return;
            }

            using var engine = new ReviewEngine(patchGenerator);

            var results = new List<ReviewComment>();
            await foreach (var result in engine.Review())
            {
                results.Add(result);
            }

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(results);
        });

        await app.RunAsync($"http://localhost:{port}");
    }
}
