// Copied from Tai-Yng/PromptRun (Run version, v0.1.0) on 2026-09-27 and made public
// for cross-assembly use (design D2); guidance message updated for the CmdPal host.

using System.IO;
using System.Net.Http;
using System.Text.Json;
using PromptRun.Library;

namespace PromptRun.Sync;

/// <summary>
/// Orchestrates Push/Pull of prompts.json: config gate, local .bak backup on pull,
/// remote validation before overwrite, and user-facing notifications.
/// </summary>
public sealed class GitHubSyncCoordinator : ISyncCoordinator
{
    private static readonly JsonSerializerOptions DocumentReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly PromptLibrary _library;
    private readonly Func<(string Repo, string Token)> _config;
    private readonly Action<string> _notify;
    private readonly Func<HttpClient> _httpClientFactory;

    public GitHubSyncCoordinator(
        PromptLibrary library,
        Func<(string Repo, string Token)> config,
        Action<string> notify,
        Func<HttpClient>? httpClientFactory = null)
    {
        _library = library;
        _config = config;
        _notify = notify;
        _httpClientFactory = httpClientFactory ?? (() => new HttpClient());
    }

    public void Push() => Run(push: true);

    public void Pull() => Run(push: false);

    private void Run(bool push)
    {
        var (repo, token) = _config();
        if (string.IsNullOrWhiteSpace(repo) || !repo.Contains('/') || string.IsNullOrWhiteSpace(token))
        {
            _notify("Configure repo and PAT in the extension settings first");
            return;
        }

        var direction = push ? "Push" : "Pull";
        try
        {
            var client = new GitHubSyncClient(_httpClientFactory(), repo, token);
            if (push) DoPush(client);
            else DoPull(client);
        }
        catch (Exception ex) when (ex is HttpRequestException or GitHubSyncException or JsonException or FormatException)
        {
            _notify($"{direction} failed: {ex.Message}");
        }
    }

    private void DoPush(GitHubSyncClient client)
    {
        if (!File.Exists(_library.FilePath))
        {
            _notify("Push skipped: prompts.json not found");
            return;
        }

        client.PushAsync(File.ReadAllBytes(_library.FilePath)).GetAwaiter().GetResult();
        _notify("Push complete: prompts.json uploaded to GitHub");
    }

    private void DoPull(GitHubSyncClient client)
    {
        var remote = client.PullAsync().GetAwaiter().GetResult();
        if (!remote.Exists)
        {
            _notify("Pull failed: prompts.json not found in the remote repo");
            return;
        }

        PromptDocument? doc;
        try
        {
            doc = JsonSerializer.Deserialize<PromptDocument>(remote.Content, DocumentReadOptions);
        }
        catch (JsonException)
        {
            _notify("Pull failed: remote file is not valid PromptRun data");
            return;
        }

        if (doc?.Prompts is null)
        {
            _notify("Pull failed: remote file is empty");
            return;
        }

        if (File.Exists(_library.FilePath))
            File.Copy(_library.FilePath, _library.FilePath + ".bak", overwrite: true);

        _library.ReplaceDocument(doc);
        _notify("Pull complete: local prompts.json replaced (backup: prompts.json.bak)");
    }
}
