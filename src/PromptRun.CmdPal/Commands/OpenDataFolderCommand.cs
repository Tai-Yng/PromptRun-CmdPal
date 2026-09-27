// Copyright (c) Tai-Yng. MIT license.

using System.Diagnostics;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PromptRun.Library;

namespace PromptRun.CmdPal.Commands;

/// <summary>Opens the directory holding prompts.json so the user can hand-edit it.</summary>
public sealed partial class OpenDataFolderCommand : InvokableCommand
{
    private readonly PromptLibrary _library;

    public OpenDataFolderCommand(PromptLibrary library)
    {
        _library = library;
        Name = "Open data folder";
        Icon = new IconInfo("\uED25"); // Folder glyph
    }

    public override CommandResult Invoke()
    {
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{System.IO.Path.GetDirectoryName(_library.FilePath)}\""));
        return CommandResult.Dismiss();
    }
}
