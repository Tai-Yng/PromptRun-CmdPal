// Copyright (c) Tai-Yng. MIT license.

using System;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PromptRun.Community;
using PromptRun.Library;

namespace PromptRun.CmdPal.Commands;

/// <summary>Plain clipboard copy with a toast; no useCount write-back (external content).</summary>
public sealed partial class CopyExternalPromptCommand : InvokableCommand
{
    private readonly string _title;
    private readonly string _content;

    public CopyExternalPromptCommand(string title, string content)
    {
        _title = title;
        _content = content;
        Name = "Copy";
        Icon = new IconInfo("\uE8C8");
    }

    public override CommandResult Invoke()
    {
        try
        {
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(_content);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
            Windows.ApplicationModel.DataTransfer.Clipboard.Flush();
        }
        catch (Exception ex)
        {
            return CommandResult.ShowToast(new ToastArgs
            {
                Message = $"Copy failed: {ex.Message}",
                Result = CommandResult.Dismiss(),
            });
        }

        return CommandResult.ShowToast(new ToastArgs
        {
            Message = $"Copied \"{_title}\" to clipboard",
            Result = CommandResult.Dismiss(),
        });
    }
}

/// <summary>Saves a community prompt into the local library (tagged community/network); duplicate-safe.</summary>
public sealed partial class SaveCommunityPromptCommand : InvokableCommand
{
    private readonly PromptLibrary _library;
    private readonly CommunityPrompt _prompt;
    private readonly Action _onSaved;

    public SaveCommunityPromptCommand(PromptLibrary library, CommunityPrompt prompt, Action onSaved)
    {
        _library = library;
        _prompt = prompt;
        _onSaved = onSaved;
        Name = "Save to library";
        Icon = new IconInfo("\uE710"); // Add glyph
    }

    public override CommandResult Invoke()
    {
        var added = _library.Add(new PromptEntry
        {
            Title = _prompt.Title,
            Content = _prompt.Content,
            Tags = [_prompt.Category, "community", "network"],
            Favorite = false,
            UseCount = 0,
        });

        _onSaved();
        return CommandResult.ShowToast(new ToastArgs
        {
            Message = added
                ? $"Saved \"{_prompt.Title}\" to your library"
                : $"\"{_prompt.Title}\" is already in your library",
            Result = CommandResult.Dismiss(),
        });
    }
}
