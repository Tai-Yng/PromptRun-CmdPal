// Copyright (c) Tai-Yng. MIT license.

using System;
using System.IO;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PromptRun.Library;

namespace PromptRun.CmdPal.Commands;

/// <summary>One-time migration: copies prompts.json from the Run version, then reloads.</summary>
public sealed partial class ImportRunDataCommand : InvokableCommand
{
    private readonly PromptLibrary _library;

    public ImportRunDataCommand(PromptLibrary library)
    {
        _library = library;
        Name = "Import from PowerToys Run";
        Icon = new IconInfo("\uE8B5"); // Import/download glyph
    }

    public override CommandResult Invoke()
    {
        try
        {
            RunDataImport.Import(DataPaths.RunVersionFilePath(), _library.FilePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return CommandResult.ShowToast(new ToastArgs
            {
                Message = $"Import failed: {ex.Message}",
                Result = CommandResult.Dismiss(),
            });
        }

        _library.Reload();
        return CommandResult.ShowToast(new ToastArgs
        {
            Message = "Import complete: prompts.json copied from PowerToys Run",
            Result = CommandResult.Dismiss(),
        });
    }
}
