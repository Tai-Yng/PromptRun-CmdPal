// Based on the official CmdPal ExtensionTemplate (microsoft/PowerToys, MIT).
// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PromptRun.CmdPal.Commands;
using PromptRun.Library;
using PromptRun.Sync;

namespace PromptRun.CmdPal;

public partial class PromptRunCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly IFallbackCommandItem[] _fallbacks;

    public PromptRunCommandsProvider()
    {
        DisplayName = "PromptRun";
        Icon = IconHelpers.FromRelativePath("Assets\\icon.png");

        // One shared library/sync instance across the browse page and the fallback search.
        var settings = new SettingsManager();
        var library = new PromptLibrary(DataPaths.DefaultBasePath());
        var syncSink = new SyncMessageSink();
        var sync = new GitHubSyncCoordinator(library, () => settings.GetSyncConfig(), syncSink.Receive);
        library.Initialize(createTemplateIfMissing: !RunDataImport.IsAvailable(library.FilePath));

        _commands =
        [
            new CommandItem(new MainListPage(settings, library, sync, syncSink))
            {
                Title = DisplayName,
                MoreCommands = [new CommandContextItem(settings.Settings.SettingsPage)],
            },
        ];

        _fallbacks = [new FallbackCommandItem(new PromptSearchPage(library), "Search prompts", "PromptRun.Search")
        {
            Title = DisplayName,
        }];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

    public override IFallbackCommandItem[] FallbackCommands()
    {
        return _fallbacks;
    }
}
