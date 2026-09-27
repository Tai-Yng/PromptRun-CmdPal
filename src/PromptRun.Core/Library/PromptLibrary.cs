// Copied from Tai-Yng/PromptRun (Run version, v0.1.0) on 2026-09-27. Keep in sync only by explicit decision.
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PromptRun.Library;

/// <summary>
/// Owns prompts.json: template bootstrap, defensive parsing, debounced hot reload
/// and useCount write-back. Pure C#, no Wox types — testable in isolation.
/// </summary>
public sealed class PromptLibrary : IDisposable
{
    private const int DebounceMs = 300;

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // UnsafeRelaxedJsonEscaping keeps CJK text readable so users can hand-edit the file.
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly object _gate = new();
    private readonly string _basePath;
    private readonly string _filePath;
    private readonly List<PromptEntry> _entries = new();
    private FileSystemWatcher? _watcher;
    private Timer? _debounce;
    private byte[]? _lastOwnWrite;

    /// <summary>Raised after the file changed externally and was reloaded.</summary>
    public event Action? Changed;

    public PromptLibrary(string basePath)
    {
        _basePath = basePath;
        Directory.CreateDirectory(basePath);
        _filePath = Path.Combine(basePath, DataPaths.DataFileName);
    }

    public string FilePath => _filePath;
    public bool TemplateCreated { get; private set; }
    public bool IsCorrupt { get; private set; }

    public IReadOnlyList<PromptEntry> Entries
    {
        get { lock (_gate) return _entries.ToList(); }
    }

    /// <summary>
    /// Loads and starts watching. Creates the template only when missing and allowed —
    /// the host passes false while a Run-version import is pending (design D3).
    /// </summary>
    public void Initialize(bool createTemplateIfMissing = true)
    {
        if (!File.Exists(_filePath) && createTemplateIfMissing)
        {
            SaveDocument(TemplateDocument());
            TemplateCreated = true;
        }

        Reload();
        StartWatcher();
    }

    public static PromptDocument TemplateDocument() => new()
    {
        Version = 1,
        Prompts =
        {
            new PromptEntry
            {
                Id = Guid.NewGuid().ToString(),
                Title = "专业翻译助手",
                Content = "你是一位专业的翻译专家。请将用户输入的内容翻译成目标语言，保持原文的语气和风格。",
                Tags = { "翻译", "示例" },
                Favorite = false,
                UseCount = 0,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            },
            new PromptEntry
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Code Reviewer",
                Content = "You are a senior code reviewer. Review the provided code for bugs, security issues and readability.",
                Tags = { "code", "example" },
                Favorite = false,
                UseCount = 0,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            },
        },
    };

    /// <summary>Re-reads prompts.json. Corrupt JSON degrades to an empty library, never throws.</summary>
    public void Reload()
    {
        lock (_gate)
        {
            _entries.Clear();
            IsCorrupt = false;

            try
            {
                var json = File.ReadAllText(_filePath);
                var doc = JsonSerializer.Deserialize<PromptDocument>(json, ReadOptions);

                if (doc?.Prompts is not null)
                {
                    foreach (var entry in doc.Prompts)
                    {
                        if (!IsValid(entry)) continue;
                        entry.Tags ??= new List<string>();
                        _entries.Add(entry);
                    }
                }
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                IsCorrupt = File.Exists(_filePath);
            }
        }
    }

    private static bool IsValid(PromptEntry? entry) =>
        entry is not null
        && !string.IsNullOrWhiteSpace(entry.Title)
        && !string.IsNullOrWhiteSpace(entry.Content);

    /// <summary>Bumps useCount and writes the whole document back in hand-editable form.</summary>
    public bool IncrementUseCount(string id)
    {
        // Re-read first so a just-landed external edit is not overwritten by our write-back.
        Reload();

        PromptDocument doc;
        lock (_gate)
        {
            var entry = _entries.FirstOrDefault(e => e.Id == id);
            if (entry is null) return false;
            entry.UseCount += 1;
            doc = ToDocument(_entries);
        }

        SaveDocument(doc);
        return true;
    }

    /// <summary>Replaces the on-disk file wholesale (used by the sync pull path).</summary>
    public void ReplaceDocument(PromptDocument doc)
    {
        SaveDocument(doc);
        Reload();
    }

    private static PromptDocument ToDocument(List<PromptEntry> entries) => new()
    {
        Version = 1,
        Prompts = entries.ToList(),
    };

    private void SaveDocument(PromptDocument doc)
    {
        var json = JsonSerializer.Serialize(doc, WriteOptions);
        _lastOwnWrite = Encoding.UTF8.GetBytes(json);
        File.WriteAllText(_filePath, json);

        lock (_gate)
        {
            _entries.Clear();
            if (doc.Prompts is not null)
            {
                foreach (var entry in doc.Prompts)
                {
                    if (!IsValid(entry)) continue;
                    entry.Tags ??= new List<string>();
                    _entries.Add(entry);
                }
            }
            IsCorrupt = false;
        }
    }

    private void StartWatcher()
    {
        _watcher = new FileSystemWatcher(_basePath, DataPaths.DataFileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };
        _watcher.Changed += OnFileEvent;
        _watcher.Created += OnFileEvent;
        _watcher.Renamed += OnFileEvent;
        _watcher.EnableRaisingEvents = true;
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        _debounce?.Dispose();
        _debounce = new Timer(_ =>
        {
            Reload();

            // Notify only when the on-disk content differs from our last write-back, so
            // events caused by our own writes never surface but external edits always do.
            bool isOwnWrite;
            try { isOwnWrite = StructuralComparisons.StructuralEqualityComparer.Equals(File.ReadAllBytes(_filePath), _lastOwnWrite); }
            catch (IOException) { isOwnWrite = false; }
            if (!isOwnWrite) Changed?.Invoke();
        }, null, DebounceMs, Timeout.Infinite);
    }

    public void Dispose()
    {
        _debounce?.Dispose();
        _watcher?.Dispose();
    }
}
