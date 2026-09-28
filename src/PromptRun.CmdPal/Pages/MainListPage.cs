// Copyright (c) Tai-Yng. MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PromptRun.CmdPal.Commands;
using PromptRun.Community;
using PromptRun.Library;
using PromptRun.Sync;

namespace PromptRun.CmdPal;

/// <summary>
/// The single top-level list: no query shows the whole library in spec order followed by
/// management actions; a query is filtered with multi-token AND and weighted ranking.
/// Shares its library/sync instances with the root-level fallback search page.
/// </summary>
internal sealed partial class MainListPage : DynamicListPage, IDisposable
{
    private readonly PromptLibrary _library;
    private readonly SyncMessageSink _syncSink;
    private readonly GitHubSyncCoordinator _sync;
    private CommunityPrompts? _community;
    private string _query = string.Empty;

    private CommunityPrompts Community => _community ??=
        new CommunityPrompts(new HttpClient(), Path.GetDirectoryName(_library.FilePath)!);

    public MainListPage(SettingsManager settings, PromptLibrary library, GitHubSyncCoordinator sync, SyncMessageSink syncSink)
    {
        Icon = IconHelpers.FromRelativePath("Assets\\icon.png");
        Title = "PromptRun";
        Name = "Open";
        PlaceholderText = "Search prompts...";
        ShowDetails = true;

        _library = library;
        _sync = sync;
        _syncSink = syncSink;
        _library.Changed += () => RaiseItemsChanged(0);
    }

    public void Dispose() => _library.Dispose();

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _query = newSearch;
        RaiseItemsChanged(0);
    }

    public override IListItem[] GetItems()
    {
        var items = new List<IListItem>();

        if (string.IsNullOrWhiteSpace(_query))
        {
            foreach (var entry in PromptSearch.All(_library.Entries))
            {
                items.Add(PromptItem(entry, _library));
            }

            if (RunDataImport.IsAvailable(_library.FilePath))
            {
                items.Add(new ListItem(new ImportRunDataCommand(_library))
                {
                    Title = "Import from PowerToys Run",
                    Subtitle = "One-time copy of the Run version's prompts.json (never modifies the original)",
                });
            }

            items.Add(new ListItem(new ExternalPromptsPage(_library, Community))
            {
                Title = "External prompt resources",
                Subtitle = "community prompt search + prompt websites",
            });
            items.Add(new ListItem(new OpenDataFolderCommand(_library))
            {
                Title = "Open data folder",
                Subtitle = _library.FilePath,
            });
            items.Add(new ListItem(new SyncCommand(_sync, _syncSink, push: true))
            {
                Title = "Push to GitHub",
                Subtitle = "Overwrite the remote prompts.json with the local file",
            });
            items.Add(new ListItem(new SyncCommand(_sync, _syncSink, push: false))
            {
                Title = "Pull from GitHub",
                Subtitle = "Replace the local file with the remote prompts.json (creates a .bak backup)",
            });
        }
        else
        {
            foreach (var entry in PromptSearch.Search(_library.Entries, _query))
            {
                items.Add(PromptItem(entry, _library));
            }
        }

        return items.ToArray();
    }

    internal static ListItem PromptItem(PromptEntry entry, PromptLibrary library)
    {
        var tags = entry.Tags.Count > 0 ? $" | {string.Join(", ", entry.Tags)}" : string.Empty;
        var placeholder = PromptSearch.HasPlaceholder(entry) ? " | {{placeholder}}" : string.Empty;

        return new ListItem(new CopyPromptCommand(entry, library))
        {
            Title = entry.Title,
            Subtitle = $"{entry.UseCount} uses{tags}{placeholder}",
            Details = new Details
            {
                Title = entry.Title,
                Body = entry.Content,
            },
        };
    }
}
