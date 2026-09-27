// Copied from Tai-Yng/PromptRun (Run version, v0.1.0) on 2026-09-27. Keep in sync only by explicit decision.
using System.Text.RegularExpressions;

namespace PromptRun.Library;

public sealed record SearchResult(PromptEntry Entry, int MatchScore);

/// <summary>Multi-token AND matching and spec-ordered ranking. Pure C#, no Wox types.</summary>
public static class PromptSearch
{
    public static readonly Regex PlaceholderRegex = new(@"\{\{[^}]+\}\}", RegexOptions.Compiled);

    public static bool HasPlaceholder(PromptEntry entry) =>
        PlaceholderRegex.IsMatch(entry.Content);

    public static List<PromptEntry> Search(IReadOnlyList<PromptEntry> entries, string query)
    {
        var tokens = Tokenize(query);
        if (tokens.Count == 0) return new List<PromptEntry>();

        var matches = new List<SearchResult>();
        foreach (var entry in entries)
        {
            var score = MatchScore(entry, tokens);
            if (score.HasValue) matches.Add(new SearchResult(entry, score.Value));
        }

        // Spec order: favorite → useCount desc → match weight (title > tags > content) → updatedAt desc.
        return matches
            .OrderByDescending(m => m.Entry.Favorite)
            .ThenByDescending(m => m.Entry.UseCount)
            .ThenByDescending(m => m.MatchScore)
            .ThenByDescending(m => m.Entry.UpdatedAt)
            .Select(m => m.Entry)
            .ToList();
    }

    /// <summary>The whole library in browsing order (no query filter).</summary>
    public static List<PromptEntry> All(IReadOnlyList<PromptEntry> entries) =>
        entries
            .OrderByDescending(e => e.Favorite)
            .ThenByDescending(e => e.UseCount)
            .ThenByDescending(e => e.UpdatedAt)
            .ToList();

    /// <summary>Every token must hit somewhere (AND); the sum is the match weight.</summary>
    internal static int? MatchScore(PromptEntry entry, List<string> tokens)
    {
        int total = 0;
        foreach (var token in tokens)
        {
            var score = TokenScore(entry, token);
            if (score == 0) return null;
            total += score;
        }
        return total;
    }

    internal static int TokenScore(PromptEntry entry, string token)
    {
        if (entry.Title.Contains(token, StringComparison.OrdinalIgnoreCase)) return 3;
        if (entry.Tags.Any(tag => tag.Contains(token, StringComparison.OrdinalIgnoreCase))) return 2;
        if (entry.Content.Contains(token, StringComparison.OrdinalIgnoreCase)) return 1;
        return 0;
    }

    internal static List<string> Tokenize(string query) =>
        query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
             .ToList();
}
