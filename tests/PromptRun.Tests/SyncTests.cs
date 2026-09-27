// Migrated from Tai-Yng/PromptRun (Run version, v0.1.0) on 2026-09-27. The two
// Run-host settings tests were dropped (they test Run-only types); CmdPal-side
// settings wiring is covered by task 5.1.

using System.Net;
using System.Text;
using PromptRun.Library;
using PromptRun.Sync;

namespace PromptRun.Tests;

public sealed class SyncTests : IDisposable
{
    private readonly string _dir;
    private readonly PromptLibrary _library;
    private readonly List<string> _notified = new();

    public SyncTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "promptrun-sync-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _library = new PromptLibrary(_dir);
        _library.Initialize();
    }

    public void Dispose()
    {
        _library.Dispose();
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Body, string? Auth)> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : request.Content.ReadAsStringAsync(cancellationToken).Result;
            Requests.Add((request.Method, body, request.Headers.Authorization?.ToString()));
            return Task.FromResult(responder(request));
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode code, string json) => new(code)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private GitHubSyncCoordinator CreateCoordinator(FakeHandler handler, (string, string) config) =>
        new(_library, () => config, msg => _notified.Add(msg), () => new HttpClient(handler));

    // ---- client level ----

    [Fact]
    public async Task Push_FetchesShaThenPutsLocalContentAsBase64()
    {
        var local = File.ReadAllBytes(_library.FilePath);
        var handler = new FakeHandler(_ => Json(HttpStatusCode.OK,
            """{"sha":"abc123","content":"e30=","encoding":"base64"}"""));
        var client = new GitHubSyncClient(new HttpClient(handler), "me/private-repo", "pat-token");

        await client.PushAsync(local);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(HttpMethod.Put, handler.Requests[1].Method);
        Assert.Contains(Convert.ToBase64String(local), handler.Requests[1].Body);
        Assert.Contains("\"sha\":\"abc123\"", handler.Requests[1].Body);
        Assert.Contains("Bearer pat-token", handler.Requests[0].Auth);
    }

    [Fact]
    public async Task Pull_RemoteExists_ParsesContentAndSha()
    {
        var payload = Encoding.UTF8.GetBytes("""{"version":1,"prompts":[]}""");
        var handler = new FakeHandler(_ => Json(HttpStatusCode.OK,
            $$"""{"sha":"def456","content":"{{Convert.ToBase64String(payload)}}","encoding":"base64"}"""));
        var client = new GitHubSyncClient(new HttpClient(handler), "me/repo", "pat");

        var remote = await client.PullAsync();

        Assert.True(remote.Exists);
        Assert.Equal("def456", remote.Sha);
        Assert.Equal(payload, remote.Content);
    }

    [Fact]
    public async Task Pull_RemoteMissing_ReportsNotExists()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = new GitHubSyncClient(new HttpClient(handler), "me/repo", "pat");

        var remote = await client.PullAsync();

        Assert.False(remote.Exists);
    }

    // ---- coordinator level ----

    [Fact]
    public void Push_WithoutConfig_GuidesUserAndSkipsHttp()
    {
        var handler = new FakeHandler(_ => throw new InvalidOperationException("no HTTP call expected"));
        var coordinator = CreateCoordinator(handler, ("", ""));

        coordinator.Push();

        Assert.Equal("Configure repo and PAT in the extension settings first", Assert.Single(_notified));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public void Push_Success_NotifiesCompletion()
    {
        var handler = new FakeHandler(req => req.Method == HttpMethod.Get
            ? Json(HttpStatusCode.NotFound, "{}")
            : Json(HttpStatusCode.OK, "{}"));
        var coordinator = CreateCoordinator(handler, ("me/private-repo", "pat"));

        coordinator.Push();

        Assert.Equal("Push complete: prompts.json uploaded to GitHub", Assert.Single(_notified));
    }

    [Fact]
    public void Pull_OverwritesLocalAndCreatesBackup()
    {
        var remoteDoc = """{"version":1,"prompts":[{"id":"9","title":"From Remote","content":"cloud copy","tags":[],"favorite":false,"useCount":0,"createdAt":1,"updatedAt":2}]}""";
        var handler = new FakeHandler(_ => Json(HttpStatusCode.OK,
            $$"""{"sha":"x","content":"{{Convert.ToBase64String(Encoding.UTF8.GetBytes(remoteDoc))}}"}"""));
        var coordinator = CreateCoordinator(handler, ("me/private-repo", "pat"));

        coordinator.Pull();

        Assert.Equal("From Remote", _library.Entries[0].Title);
        Assert.True(File.Exists(_library.FilePath + ".bak"), "local backup must exist");
        Assert.Contains("专业翻译助手", File.ReadAllText(_library.FilePath + ".bak"));
        Assert.Contains("Pull complete", Assert.Single(_notified));
    }

    [Fact]
    public void Pull_RemoteMissing_KeepsLocalFileWithoutBackup()
    {
        var before = File.ReadAllText(_library.FilePath);
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var coordinator = CreateCoordinator(handler, ("me/private-repo", "pat"));

        coordinator.Pull();

        Assert.Equal(before, File.ReadAllText(_library.FilePath));
        Assert.False(File.Exists(_library.FilePath + ".bak"));
        Assert.Contains("not found in the remote repo", Assert.Single(_notified));
    }

    [Fact]
    public void Pull_InvalidRemotePayload_NotifyAndKeepLocal()
    {
        var before = File.ReadAllText(_library.FilePath);
        var handler = new FakeHandler(_ => Json(HttpStatusCode.OK,
            $$"""{"sha":"x","content":"{{Convert.ToBase64String("not json at all"u8.ToArray())}}"}"""));
        var coordinator = CreateCoordinator(handler, ("me/private-repo", "pat"));

        coordinator.Pull();

        Assert.Equal(before, File.ReadAllText(_library.FilePath));
        Assert.Contains("not valid PromptRun data", Assert.Single(_notified));
    }

    [Fact]
    public void Pull_NetworkError_NotifyFailure()
    {
        var handler = new FakeHandler(_ => throw new HttpRequestException("connection refused"));
        var coordinator = CreateCoordinator(handler, ("me/private-repo", "pat"));

        coordinator.Pull();

        Assert.Contains("Pull failed", Assert.Single(_notified));
    }
}
