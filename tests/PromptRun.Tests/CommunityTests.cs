// Community search (awesome-chatgpt-prompts dataset) + library save, new for the
// "external prompt resources" feature.

using System.Net;
using System.Text;
using PromptRun.Community;
using PromptRun.Library;

namespace PromptRun.Tests;

public sealed class CommunityTests : IDisposable
{
    private readonly string _dir;
    private readonly PromptLibrary _library;

    public CommunityTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "promptrun-community-" + Guid.NewGuid().ToString("N"));
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
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(responder(request));
        }
    }

    private static HttpResponseMessage Text(HttpStatusCode code, string body) => new(code)
    {
        Content = new StringContent(body, Encoding.UTF8, "text/csv"),
    };

    private const string CsvHeader = "act,prompt\n";

    // ---- CsvParser ----

    [Fact]
    public void Parse_HandlesQuotedFieldsEscapesAndEmbeddedNewlines()
    {
        var rows = CsvParser.Parse(
            "act,prompt\n\"Shell, \"\"advanced\"\"\",\"line one\nline two\"\nLinux Expert,\"a \"\"quoted\"\" word\"\n");

        Assert.Equal(3, rows.Count); // header + 2 records
        Assert.Equal("Shell, \"advanced\"", rows[1][0]);
        Assert.Contains("\n", rows[1][1]);
        Assert.Equal("Linux Expert", rows[2][0]);
        Assert.Contains("\"quoted\"", rows[2][1]);
    }

    // ---- dataset parsing ----

    [Fact]
    public void ParseDataset_SkipsHeaderAndEmptyPrompts_Categorizes()
    {
        var csv = CsvHeader
            + "\"SQL Expert\",\"Optimize this query\"\n"
            + "Midjourney Artist,\"a [subject] portrait\"\n"
            + "\"Ghost\",\"\"\n"
            + ",\"\n";

        var items = CommunityPrompts.ParseDataset(csv);

        Assert.Equal(2, items.Count);
        Assert.Equal("SQL Expert", items[0].Title);
        Assert.Equal("code", items[0].Category);
        Assert.Equal("image", items[1].Category);
    }

    // ---- network chain ----

    [Fact]
    public async Task GetAsync_CdnFirst_ThenFallsBackToGitHub()
    {
        var handler = new FakeHandler(req =>
            req.RequestUri!.Host.Contains("jsdelivr")
                ? Text(HttpStatusCode.InternalServerError, "boom")
                : Text(HttpStatusCode.OK, CsvHeader + "\"Translator\",\"Translate text\"\n"));
        var community = new CommunityPrompts(new HttpClient(handler), _dir);

        var items = await community.GetAsync();

        Assert.Equal(2, handler.Calls);
        Assert.Single(items);
        Assert.Equal("github", community.Source);
        Assert.True(File.Exists(Path.Combine(_dir, "community-cache.json")), "disk cache must be written");
    }

    [Fact]
    public async Task GetAsync_NetworkDown_UsesStaleDiskCache()
    {
        // seed the disk cache via a successful first load
        var okHandler = new FakeHandler(_ => Text(HttpStatusCode.OK, CsvHeader + "\"Seeded\",\"seed content\"\n"));
        var community = new CommunityPrompts(new HttpClient(okHandler), _dir);
        await community.GetAsync();

        var offline = new CommunityPrompts(new HttpClient(
            new FakeHandler(_ => throw new HttpRequestException("offline"))), _dir);

        var items = await offline.GetAsync();

        Assert.Single(items);
        Assert.Equal("Seeded", items[0].Title);
        Assert.Equal("cache", offline.Source);
    }

    [Fact]
    public async Task SearchAsync_FiltersByTitleOrContent_EmptyQueryYieldsNothing()
    {
        var handler = new FakeHandler(_ => Text(HttpStatusCode.OK, CsvHeader
            + "\"Translator\",\"Translate the text to Chinese\"\n"
            + "\"SQL Expert\",\"Optimize queries\"\n"));
        var community = new CommunityPrompts(new HttpClient(handler), _dir);

        Assert.Empty(await community.SearchAsync(""));

        var hits = await community.SearchAsync("translate");
        Assert.Single(hits);
        Assert.Equal("Translator", hits[0].Title);
    }

    // ---- save to library ----

    [Fact]
    public void LibraryAdd_AppendsAndRejectsDuplicates()
    {
        var before = _library.Entries.Count; // template entries from Initialize

        Assert.True(_library.Add(new PromptEntry { Title = "From Community", Content = "hello" }));
        Assert.False(_library.Add(new PromptEntry { Title = "From Community", Content = "hello" }));
        Assert.True(_library.Add(new PromptEntry { Title = "From Community", Content = "different body", Tags = ["community", "network"] }));

        var entries = _library.Entries;
        Assert.Equal(before + 2, entries.Count);
        Assert.Contains("community", entries[^1].Tags);
        Assert.Equal(2, entries.Count(e => e.Title == "From Community"));
    }
}
