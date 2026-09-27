// Adapted from Tai-Yng/PromptRun (Run version, v0.1.0) on 2026-09-27: base path moved from the
// PowerToys Run plugin settings directory to the CmdPal data directory (design D3).

using System.IO;

namespace PromptRun.Library;

/// <summary>
/// Resolves the extension data directory:
/// %LOCALAPPDATA%\Microsoft\PowerToys\CmdPal\PromptRun.
/// </summary>
public static class DataPaths
{
    public const string FolderName = "PromptRun";
    public const string DataFileName = "prompts.json";

    /// <summary>The Run version's data file, used only by the one-time import.</summary>
    public static string RunVersionFilePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Microsoft", "PowerToys", "PowerToys Run", "Settings", "Plugins", FolderName, DataFileName);
    }

    public static string DefaultBasePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Microsoft", "PowerToys", "CmdPal", FolderName);
    }
}
