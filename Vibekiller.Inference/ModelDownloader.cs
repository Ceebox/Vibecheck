using Microsoft.Extensions.Configuration;
using Vibekiller.Settings;
using Vibekiller.Utility;

namespace Vibekiller.Inference;

internal sealed class ModelDownloader
{
    private readonly string mDownloadUrl;
    private readonly string mOutputPath;

    public ModelDownloader(string downloadUrl, string outputPath)
    {
        mDownloadUrl = downloadUrl;
        mOutputPath = outputPath;
    }

    public bool ModelDownloaded
        => File.Exists(mOutputPath);

    public async Task Load()
    {
        using var activity = Tracing.Start();
        await this.DownloadModelAsync();
    }

    private async Task DownloadModelAsync(string? token = null)
    {
        using var activity = Tracing.Start();

        try
        {
            // Try without token first
            var success = await this.TryDownloadAsync(null);

            // We'll have to use the token
            if (!success && !string.IsNullOrEmpty(token))
            {
                activity.Log("Authentication required — retrying with token...");

#if DEBUG
                // Retrieve secret debug API key
                // TODO: Make this customisable
                var configurationBuilder = new ConfigurationBuilder();
                var configuration = configurationBuilder.AddUserSecrets<ModelDownloader>().Build();
                token ??= configuration.GetValue<string>("huggingface_api_key");
                token ??= Configuration.Current.InferenceSettings.ModelApiToken;
                if (token == null)
                {
                    activity.AddWarning("No API key found in user secrets.");
                }
#endif

                success = await this.TryDownloadAsync(token);
            }

            if (!success)
            {
                activity.AddError("Download failed due to authentication issues and no valid token.");
            }
        }
        catch (Exception ex)
        {
            activity.Log(ex);
        }
    }

    /// <summary>
    /// Try to download the model from a URL.
    /// </summary>
    /// <param name="token">An optional bearer token to use, as some models require it.</param>
    /// <returns></returns>
    private async Task<bool> TryDownloadAsync(string? token = null)
    {
        using var activity = Tracing.Start();
        using var httpClient = new HttpClient();

        if (!string.IsNullOrEmpty(token))
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        Tracing.WriteLine($"Trying to download model from {mDownloadUrl}...", LogLevel.INFO);
        using var response = await httpClient.GetAsync(mDownloadUrl, HttpCompletionOption.ResponseHeadersRead);

        // We need a (correct) token!
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength;
        using var stream = await response.Content.ReadAsStreamAsync();
        using var fileStream = File.Create(mOutputPath);

        var buffer = new byte[81920];
        var totalRead = 0L;
        var bytesRead = 0;
        var lastReportTime = DateTime.UtcNow;

        activity.Log($"Downloading model to {mOutputPath}...", LogLevel.INFO);

        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            totalRead += bytesRead;

            if (contentLength.HasValue && (DateTime.UtcNow - lastReportTime).TotalSeconds >= 0.5)
            {
                var percent = (double)totalRead / contentLength.Value * 100;
                Console.Write($"\rProgress: {percent:F1}% ({totalRead / 1_000_000.0:F1} MB / {contentLength.Value / 1_000_000.0:F1} MB)");
                lastReportTime = DateTime.UtcNow;
            }
        }

        // This is really long to flush the previous line
        Console.WriteLine("\r\nDownload complete.                                              ");
        return true;
    }
}
