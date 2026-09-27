// Copyright (c) Tai-Yng. MIT license.

using System;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PromptRun.Library;

namespace PromptRun.CmdPal.Commands;

/// <summary>Copies a prompt's content verbatim to the clipboard, bumps useCount, shows a toast.</summary>
public sealed partial class CopyPromptCommand : InvokableCommand
{
    private readonly PromptEntry _entry;
    private readonly PromptLibrary _library;

    public CopyPromptCommand(PromptEntry entry, PromptLibrary library)
    {
        _entry = entry;
        _library = library;
        Name = "Copy";
        Icon = new IconInfo("\uE8C8"); // Copy glyph
    }

    public override CommandResult Invoke()
    {
        try
        {
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(_entry.Content);
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

        _library.IncrementUseCount(_entry.Id);

        return CommandResult.ShowToast(new ToastArgs
        {
            Message = $"Copied \"{_entry.Title}\" to clipboard",
            Result = CommandResult.Dismiss(),
        });
    }
}
