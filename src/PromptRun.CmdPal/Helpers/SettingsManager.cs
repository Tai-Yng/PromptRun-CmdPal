// Based on the official CmdPal ExtensionTemplate (microsoft/PowerToys, MIT).
// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace PromptRun.CmdPal;

internal sealed partial class SettingsManager : JsonSettingsManager
{
    private static readonly string _namespace = "PromptRun.CmdPal";

    private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";

    private readonly TextSetting _githubRepo = new(
        Namespaced("GithubRepo"),
        "GitHub repository",
        "owner/name of the private repo that stores prompts.json",
        string.Empty);

    private readonly TextSetting _githubPat = new(
        Namespaced("GithubPat"),
        "GitHub fine-grained PAT",
        "Personal access token with Contents read/write on that single repo",
        string.Empty);

    internal static string SettingsJsonPath()
    {
        var directory = Utilities.BaseSettingsPath("PromptRun.CmdPal");
        Directory.CreateDirectory(directory);

        return Path.Combine(directory, "settings.json");
    }

    public SettingsManager()
    {
        FilePath = SettingsJsonPath();

        Settings.Add(_githubRepo);
        Settings.Add(_githubPat);

        // Load settings from file upon initialization
        LoadSettings();

        Settings.SettingsChanged += (_, _) => SaveSettings();
    }

    public (string Repo, string Token) GetSyncConfig() =>
        (_githubRepo.Value ?? string.Empty, _githubPat.Value ?? string.Empty);
}
