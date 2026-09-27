// Copyright (c) Tai-Yng. MIT license.

using System.IO;

namespace PromptRun.Library;

/// <summary>
/// One-time migration from the Run version: prompts.json is copied wholesale
/// (favorites and useCount history preserved); the Run version's file is never modified.
/// </summary>
public static class RunDataImport
{
    /// <summary>Import is offered only while no local data file exists and the Run version's file does.</summary>
    public static bool IsAvailable(string localFilePath, string? runFilePath = null) =>
        !File.Exists(localFilePath) && File.Exists(runFilePath ?? DataPaths.RunVersionFilePath());

    public static void Import(string runFilePath, string localFilePath) =>
        File.Copy(runFilePath, localFilePath, overwrite: false);
}
