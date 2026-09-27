using PromptRun.Library;

namespace PromptRun.Tests;

public sealed class SearchTests
{
    private static PromptEntry E(
        string title, string content = "", string[]? tags = null,
        bool favorite = false, long useCount = 0, long updatedAt = 0) => new()
    {
        Id = title,
        Title = title,
        Content = content,
        Tags = tags?.ToList() ?? new List<string>(),
        Favorite = favorite,
        UseCount = useCount,
        UpdatedAt = updatedAt,
    };

    [Fact]
    public void Search_MultiToken_AndSemantics()
    {
        var entries = new[]
        {
            E("专业翻译助手", content: "请翻译输入内容"),
            E("代码审查", content: "审查代码质量", tags: new[] { "review" }),
        };

        // "翻译" only hits entry 1, "代码" only hits entry 2 → nothing hits both → empty.
        Assert.Empty(PromptSearch.Search(entries, "翻译 代码"));

        // Both tokens hit entry 2 (title + content).
        var result = PromptSearch.Search(entries, "代码 审查");
        Assert.Single(result, entries[1]);
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmpty()
    {
        var entries = new[] { E("翻译助手") };

        Assert.Empty(PromptSearch.Search(entries, "zzz"));
    }

    [Fact]
    public void Search_FavoriteRanksFirst()
    {
        var plain = E("alpha");
        var favorite = E("alpha2", favorite: true);

        var result = PromptSearch.Search(new[] { plain, favorite }, "alpha");

        Assert.Equal(new[] { favorite, plain }, result);
    }

    [Fact]
    public void Search_HigherUseCountRanksFirst()
    {
        var cold = E("beta", useCount: 1);
        var hot = E("beta2", useCount: 9);

        var result = PromptSearch.Search(new[] { cold, hot }, "beta");

        Assert.Equal(new[] { hot, cold }, result);
    }

    [Fact]
    public void Search_MatchWeightTitleBeatsTagsBeatsContent()
    {
        var byTitle = E("review");
        var byTags = E("placeholder", tags: new[] { "review" });
        var byContent = E("placeholder2", content: "do a review");

        var result = PromptSearch.Search(new[] { byContent, byTags, byTitle }, "review");

        Assert.Equal(new[] { byTitle, byTags, byContent }, result);
    }

    [Fact]
    public void Search_NewerUpdatedAtBreaksTies()
    {
        var older = E("gamma", updatedAt: 100);
        var newer = E("gamma2", updatedAt: 200);

        var result = PromptSearch.Search(new[] { older, newer }, "gamma");

        Assert.Equal(new[] { newer, older }, result);
    }

    [Fact]
    public void Search_IsCaseInsensitive()
    {
        var entries = new[] { E("Code Reviewer") };

        Assert.Single(PromptSearch.Search(entries, "code"));
    }

    [Fact]
    public void All_RanksByFavoriteThenUseCountThenUpdated()
    {
        var plain = E("a");
        var hot = E("b", useCount: 5);
        var favorite = E("c", favorite: true, useCount: 1);

        var result = PromptSearch.All(new[] { plain, hot, favorite });

        Assert.Equal(new[] { favorite, hot, plain }, result);
    }

    [Fact]
    public void HasPlaceholder_DetectsTemplateVariables()
    {
        Assert.True(PromptSearch.HasPlaceholder(E("t", content: "translate {{target_language}} please")));
        Assert.False(PromptSearch.HasPlaceholder(E("t2", content: "no variables here")));
    }
}
