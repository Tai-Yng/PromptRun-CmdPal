// Copyright (c) Tai-Yng. MIT license.

using System;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PromptRun.Library;

namespace PromptRun.CmdPal.Commands;

/// <summary>Deletes a prompt from the library (idempotent), then refreshes the list.</summary>
public sealed partial class DeletePromptCommand : InvokableCommand
{
    private readonly PromptLibrary _library;
    private readonly PromptEntry _entry;
    private readonly Action _onChanged;

    public DeletePromptCommand(PromptLibrary library, PromptEntry entry, Action onChanged)
    {
        _library = library;
        _entry = entry;
        _onChanged = onChanged;
        Name = "Delete";
        Icon = new IconInfo("\uE74D"); // Delete glyph
    }

    public override CommandResult Invoke()
    {
        var deleted = _library.Delete(_entry.Id);
        _onChanged();
        return CommandResult.ShowToast(new ToastArgs
        {
            Message = deleted ? $"Deleted \"{_entry.Title}\"" : "Already deleted",
            Result = CommandResult.Dismiss(),
        });
    }
}
