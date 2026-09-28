// Copyright (c) Tai-Yng. MIT license.
//
// Community prompt search over the public f/awesome-chatgpt-prompts dataset
// (act,prompt CSV). Source chain mirrors the community convention: CDN → GitHub
// raw → stale disk cache. Independent implementation, no shared code/data with
// any other local project.

using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PromptRun.Community;

public sealed record CommunityPrompt(string Id, string Title, string Content, string Category);

public sealed class CommunityPrompts
{
    private static readonly string[] Sources =
    [
        "https://cdn.jsdelivr.net/gh/f/awesome-chatgpt-prompts@main/prompts.csv",
        "https://raw.githubusercontent.com/f/awesome-chatgpt-prompts/main/prompts.csv",
    ];

    private const int FetchTimeoutSeconds = 10;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions CacheOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
    };

    private sealed record DiskCache(long At, List<CommunityPrompt> Items);

    private readonly HttpClient _http;
    private readonly string _cacheFilePath;
    private List<CommunityPrompt>? _memory;
    private DateTimeOffset _memoryAt;
    private string _source = "";

    /// <summary>'jsdelivr' | 'github' | 'cache' — where the current items came from.</summary>
    public string Source => _source;

    public CommunityPrompts(HttpClient httpClient, string cacheDirectory)
    {
        _http = httpClient;
        _http.Timeout = TimeSpan.FromSeconds(FetchTimeoutSeconds);
        Directory.CreateDirectory(cacheDirectory);
        _cacheFilePath = Path.Combine(cacheDirectory, "community-cache.json");
    }

    /// <summary>Loads the dataset (memory → fresh disk cache → network → stale disk cache) and never throws.</summary>
    public async Task<IReadOnlyList<CommunityPrompt>> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_memory is not null && DateTimeOffset.UtcNow - _memoryAt < CacheTtl)
        {
            return _memory;
        }

        var (diskAt, diskItems) = ReadDiskCache();
        if (diskItems.Count > 0 && DateTimeOffset.UtcNow - diskAt < CacheTtl)
        {
            _memory = diskItems;
            _memoryAt = diskAt;
            _source = "cache";
            return _memory;
        }

        foreach (var url in Sources)
        {
            try
            {
                var csv = await _http.GetStringAsync(url, cancellationToken);
                var items = ParseDataset(csv);
                if (items.Count > 0)
                {
                    _memory = items;
                    _memoryAt = DateTimeOffset.UtcNow;
                    _source = url.Contains("jsdelivr", StringComparison.OrdinalIgnoreCase) ? "jsdelivr" : "github";
                    WriteDiskCache(_memoryAt, items);
                    return _memory;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
            {
                // try the next source
            }
        }

        if (diskItems.Count > 0)
        {
            _memory = diskItems;
            _memoryAt = DateTimeOffset.UtcNow;
            _source = "cache";
            return _memory;
        }

        return [];
    }

    /// <summary>Case-insensitive contains over title/content; empty query yields nothing.</summary>
    public async Task<IReadOnlyList<CommunityPrompt>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var all = await GetAsync(cancellationToken);
        var matches = new List<CommunityPrompt>();
        foreach (var p in all)
        {
            if (p.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                || p.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(p);
                if (matches.Count >= 50) break;
            }
        }
        return matches;
    }

    public static List<CommunityPrompt> ParseDataset(string csv)
    {
        var rows = CsvParser.Parse(csv);
        var items = new List<CommunityPrompt>();
        // First row is the act,prompt header.
        for (var i = 1; i < rows.Count; i++)
        {
            var act = rows[i].Count > 0 ? rows[i][0].Trim() : string.Empty;
            var prompt = rows[i].Count > 1 ? rows[i][1] : string.Empty;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                continue;
            }
            items.Add(new CommunityPrompt(
                Id: $"net-{items.Count}",
                Title: act.Length > 0 ? act : $"Prompt {items.Count + 1}",
                Content: prompt,
                Category: InferCategory($"{act} {prompt}")));
        }
        return items;
    }

    internal static string InferCategory(string text)
    {
        var t = text.ToLowerInvariant();
        if (System.Text.RegularExpressions.Regex.IsMatch(t, "(code|program|develop|sql|regex|debug|software|javascript|python|linux|terminal|git|ethicist hacker|cyber)")) return "code";
        if (System.Text.RegularExpressions.Regex.IsMatch(t, "(image|art|design|midjourney|photograph|draw|logo|illustr|paint)")) return "image";
        if (System.Text.RegularExpressions.Regex.IsMatch(t, "(writ|essay|story|novel|blog|poet|journal|edit|screenplay)")) return "writing";
        return "chat";
    }

    private (DateTimeOffset At, List<CommunityPrompt> Items) ReadDiskCache()
    {
        try
        {
            var json = File.ReadAllText(_cacheFilePath);
            var cache = JsonSerializer.Deserialize<DiskCache>(json, CacheOptions);
            if (cache?.Items is { Count: > 0 })
            {
                return (DateTimeOffset.FromUnixTimeMilliseconds(cache.At), cache.Items);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // fall through to network
        }
        return (DateTimeOffset.MinValue, []);
    }

    private void WriteDiskCache(DateTimeOffset at, List<CommunityPrompt> items)
    {
        try
        {
            var json = JsonSerializer.Serialize(new DiskCache(at.ToUnixTimeMilliseconds(), items), CacheOptions);
            File.WriteAllText(_cacheFilePath, json, new UTF8Encoding(false));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // cache write is best-effort
        }
    }
}
