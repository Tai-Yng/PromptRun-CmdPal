// Copyright (c) Tai-Yng. MIT license.
//
// Port of the "ext / external prompt resources" concept: community prompt search over
// the public awesome-chatgpt-prompts dataset plus hand-picked prompt website cards.
// Independent implementation — no shared code/data with any other local project.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PromptRun.CmdPal.Commands;
using PromptRun.Community;
using PromptRun.Library;

namespace PromptRun.CmdPal;

internal sealed partial class ExternalPromptsPage : DynamicListPage
{
    private static readonly (string Name, string Url, string Desc, string Tags)[] Sites =
    [
        ("PromptHero", "https://prompthero.com/", "AI Prompt Library - Midjourney, SD, ChatGPT", "image | chat"),
        ("FlowGPT", "https://flowgpt.com/", "ChatGPT Prompt Community", "chat | community"),
        ("OpenArt", "https://openart.ai/", "AI Art Prompts & Gallery", "image | art"),
        ("Lexica", "https://lexica.art/", "Stable Diffusion Search Engine", "image | search"),
        ("PublicPrompts", "https://publicprompts.art/", "Free Quality Prompt Collection", "image | free"),
        ("Snoozy AI", "https://snoozy.io/", "Midjourney Prompt Resources", "image | mj"),
        ("Learning Prompt", "https://learningprompt.wiki/", "Chinese Prompt Tutorial", "cn | learn"),
        ("Awesome ChatGPT", "https://github.com/f/awesome-chatgpt-prompts", "GitHub Prompt Collection", "github | list"),
    ];

    private const int DebounceMs = 300;

    private readonly PromptLibrary _library;
    private readonly CommunityPrompts _community;
    private CancellationTokenSource? _cts;
    private IReadOnlyList<CommunityPrompt>? _results;
    private string _source = string.Empty;
    private string _error = string.Empty;
    private string _query = string.Empty;

    public ExternalPromptsPage(PromptLibrary library, CommunityPrompts community)
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "External prompt resources";
        Name = "Open";
        PlaceholderText = "search community prompts...";
        ShowDetails = true;

        _library = library;
        _community = community;
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _query = newSearch;
        _error = string.Empty;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        if (string.IsNullOrWhiteSpace(newSearch))
        {
            _results = null;
            RaiseItemsChanged(0);
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceMs, token);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                var results = await _community.SearchAsync(newSearch, token);
                _results = results;
                _source = _community.Source;
                _error = results.Count == 0 ? "no matches" : string.Empty;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _results = [];
                _error = ex.Message;
            }

            RaiseItemsChanged(0);
        }, token);

        RaiseItemsChanged(0);
    }

    public override IListItem[] GetItems()
    {
        var items = new List<IListItem>();

        if (string.IsNullOrWhiteSpace(_query))
        {
            items.Add(new ListItem(new NoOpCommand())
            {
                Title = "Type to search community prompts...",
                Subtitle = "searches the public awesome-chatgpt-prompts dataset",
            });
            items.Add(new ListItem(new OpenDataFolderCommand(_library))
            {
                Title = "Open data folder",
                Subtitle = _library.FilePath,
            });
            foreach (var (name, url, desc, tags) in Sites)
            {
                items.Add(new ListItem(new OpenUrlCommand(url))
                {
                    Title = name,
                    Subtitle = $"{desc}  [{tags}]",
                });
            }
            return items.ToArray();
        }

        if (_results is null)
        {
            items.Add(new ListItem(new NoOpCommand()) { Title = "searching community prompts..." });
            return items.ToArray();
        }

        if (_results.Count == 0)
        {
            items.Add(new ListItem(new NoOpCommand())
            {
                Title = string.IsNullOrEmpty(_error) ? "no matches" : $"[ERR] {_error}",
                Subtitle = _source.Length > 0 ? $"source: {_source}" : string.Empty,
            });
            return items.ToArray();
        }

        foreach (var p in _results)
        {
            var prompt = p;
            items.Add(new ListItem(new CopyExternalPromptCommand(p.Title, p.Content))
            {
                Title = p.Title,
                Subtitle = $"{p.Category} | {Preview(p.Content)}",
                Details = new Details
                {
                    Title = p.Title,
                    Body = p.Content,
                },
                MoreCommands =
                [
                    new CommandContextItem(new SaveCommunityPromptCommand(_library, prompt,
                        () => RaiseItemsChanged(0))),
                ],
            });
        }

        return items.ToArray();
    }

    private static string Preview(string content)
    {
        var single = content.Replace("\r", " ").Replace("\n", " ");
        return single.Length <= 80 ? single : single[..80] + "...";
    }
}
