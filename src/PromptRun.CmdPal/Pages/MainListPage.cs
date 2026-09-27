// Copyright (c) Tai-Yng. MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PromptRun.CmdPal.Commands;
using PromptRun.Library;
using PromptRun.Sync;

namespace PromptRun.CmdPal;

/// <summary>
/// The single top-level list: no query shows the whole library in spec order followed by
/// management actions; a query is filtered with multi-token AND and weighted ranking.
/// </summary>
internal sealed partial class MainListPage : DynamicListPage, IDisposable
{
    private readonly PromptLibrary _library;
    private readonly SyncMessageSink _syncSink = new();
    private readonly GitHubSyncCoordinator _sync;
    private string _query = string.Empty;

    public MainListPage(SettingsManager settings)
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "PromptRun";
        Name = "Open";
        PlaceholderText = "Search prompts...";
        ShowDetails = true;

        _library = new PromptLibrary(DataPaths.DefaultBasePath());
        _sync = new GitHubSyncCoordinator(_library, () => settings.GetSyncConfig(), _syncSink.Receive);
        _library.Changed += () => RaiseItemsChanged(0);
        _library.Initialize(createTemplateIfMissing: !RunDataImport.IsAvailable(_library.FilePath));
    }

    public void Dispose() => _library.Dispose();

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _query = newSearch;
        RaiseItemsChanged();
    }

    public override IListItem[] GetItems()
    {
        var items = new List<IListItem>();

        if (string.IsNullOrWhiteSpace(_query))
        {
            foreach (var entry in PromptSearch.All(_library.Entries))
            {
                items.Add(PromptItem(entry));
            }

            if (RunDataImport.IsAvailable(_library.FilePath))
            {
                items.Add(new ListItem(new ImportRunDataCommand(_library))
                {
                    Title = "Import from PowerToys Run",
                    Subtitle = "One-time copy of the Run version's prompts.json (never modifies the original)",
                });
            }

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
                items.Add(PromptItem(entry));
            }
        }

        return items.ToArray();
    }

    private ListItem PromptItem(PromptEntry entry)
    {
        var tags = entry.Tags.Count > 0 ? $" | {string.Join(", ", entry.Tags)}" : string.Empty;
        var placeholder = PromptSearch.HasPlaceholder(entry) ? " | {{placeholder}}" : string.Empty;

        return new ListItem(new CopyPromptCommand(entry, _library))
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
