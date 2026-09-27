// Copied from Tai-Yng/PromptRun (Run version, v0.1.0) on 2026-09-27. Keep in sync only by explicit decision.

namespace PromptRun.Library;

/// <summary>One prompt snippet as stored in schema v1.</summary>
public sealed class PromptEntry
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new();

    public bool Favorite { get; set; }

    public long UseCount { get; set; }

    public long CreatedAt { get; set; }

    public long UpdatedAt { get; set; }
}

/// <summary>Root object of prompts.json (schema v1).</summary>
public sealed class PromptDocument
{
    public int Version { get; set; } = 1;

    public List<PromptEntry> Prompts { get; set; } = new();
}
