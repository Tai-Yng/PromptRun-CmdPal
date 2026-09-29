// Copyright (c) Tai-Yng. MIT license.

using System.Collections.Generic;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PromptRun.Library;
using PromptRun.Sync;

namespace PromptRun.CmdPal;

/// <summary>
/// Root-level fallback search: as the user types in Command Palette, matching prompts
/// appear directly (Run-version `pp &lt;query&gt;` parity). Empty query hides the item.
/// </summary>
internal sealed partial class PromptSearchPage : DynamicListPage
{
    private readonly PromptLibrary _library;
    private string _query = string.Empty;

    public PromptSearchPage(PromptLibrary library)
    {
        Icon = IconHelpers.FromRelativePath("Assets\\icon.png");
        Title = "PromptRun";
        Name = "Search prompts";
        PlaceholderText = "Search prompts...";
        ShowDetails = true;

        _library = library;
        _library.Changed += () => RaiseItemsChanged(0);
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _query = newSearch;
        RaiseItemsChanged(0);
    }

    public override IListItem[] GetItems()
    {
        if (string.IsNullOrWhiteSpace(_query))
        {
            return [];
        }

        var items = new List<IListItem>();
        foreach (var entry in PromptSearch.Search(_library.Entries, _query))
        {
            items.Add(MainListPage.PromptItem(entry, _library, () => RaiseItemsChanged(0)));
        }
        return items.ToArray();
    }
}
