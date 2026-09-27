// Copied from Tai-Yng/PromptRun (Run version, v0.1.0) on 2026-09-27. Keep in sync only by explicit decision.
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PromptRun.Sync;

public sealed class GitHubFileContent
{
    public bool Exists { get; init; }
    public string? Sha { get; init; }
    public byte[] Content { get; init; } = Array.Empty<byte>();
}

public sealed class GitHubSyncException(string message) : Exception(message);

/// <summary>
/// Single-file sync against the GitHub Contents API. HttpClient is injected so
/// request construction and response parsing are testable with a fake handler.
/// </summary>
public sealed class GitHubSyncClient
{
    private const string CommitMessage = "PromptRUN sync";

    // Relaxed encoder so base64 '+' stays literal in the request body.
    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly HttpClient _http;
    private readonly string _contentsUrl;

    public GitHubSyncClient(HttpClient httpClient, string repo, string token)
    {
        _http = httpClient;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("PromptRun-PowerToys-Plugin");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        _contentsUrl = $"https://api.github.com/repos/{repo.Trim()}/contents/prompts.json";
    }

    /// <summary>Uploads content, fetching the remote sha first when the file exists.</summary>
    public async Task PushAsync(byte[] content, CancellationToken cancellationToken = default)
    {
        var remote = await PullAsync(cancellationToken);
        object payload = remote.Exists
            ? new { message = CommitMessage, content = Convert.ToBase64String(content), sha = remote.Sha }
            : new { message = CommitMessage, content = Convert.ToBase64String(content) };

        var json = JsonSerializer.Serialize(payload, PayloadOptions);
        using var response = await _http.PutAsync(
            _contentsUrl, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new GitHubSyncException($"GitHub returned HTTP {(int)response.StatusCode}");
    }

    public async Task<GitHubFileContent> PullAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(_contentsUrl, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return new GitHubFileContent { Exists = false };
        if (!response.IsSuccessStatusCode)
            throw new GitHubSyncException($"GitHub returned HTTP {(int)response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var sha = doc.RootElement.GetProperty("sha").GetString();
        var base64 = doc.RootElement.GetProperty("content").GetString()?.Replace("\n", string.Empty) ?? string.Empty;
        return new GitHubFileContent { Exists = true, Sha = sha, Content = Convert.FromBase64String(base64) };
    }
}
