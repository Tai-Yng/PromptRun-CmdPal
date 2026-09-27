// New for the CmdPal port: the one-time Run-version import (spec: prompt-library,
// Requirement "Run 版数据一次性导入").

using PromptRun.Library;

namespace PromptRun.Tests;

public sealed class ImportTests : IDisposable
{
    private readonly string _root;
    private readonly string _localDir;
    private readonly string _runFile;
    private readonly string _localFile;

    public ImportTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "promptrun-import-" + Guid.NewGuid().ToString("N"));
        _localDir = Path.Combine(_root, "cmdpal", "PromptRun");
        Directory.CreateDirectory(_localDir);
        Directory.CreateDirectory(Path.Combine(_root, "run", "Plugins", "PromptRun"));
        _runFile = Path.Combine(_root, "run", "Plugins", "PromptRun", DataPaths.DataFileName);
        _localFile = Path.Combine(_localDir, DataPaths.DataFileName);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    private void WriteRunFile(string json) => File.WriteAllText(_runFile, json);

    private const string RunJson = """
    {"version":1,"prompts":[{"id":"42","title":"Legacy prompt","content":"from the Run version","tags":["migrated"],"favorite":true,"useCount":11,"createdAt":1,"updatedAt":2}]}
    """;

    [Fact]
    public void IsAvailable_RunFileExistsAndLocalMissing_True()
    {
        WriteRunFile(RunJson);

        Assert.True(RunDataImport.IsAvailable(_localFile, _runFile));
    }

    [Fact]
    public void IsAvailable_LocalFileExists_False()
    {
        WriteRunFile(RunJson);
        File.WriteAllText(_localFile, "{}");

        Assert.False(RunDataImport.IsAvailable(_localFile, _runFile));
    }

    [Fact]
    public void IsAvailable_RunFileMissing_False()
    {
        Assert.False(RunDataImport.IsAvailable(_localFile, _runFile));
    }

    [Fact]
    public void Import_CopiesWholesaleAndKeepsOriginal()
    {
        WriteRunFile(RunJson);

        RunDataImport.Import(_runFile, _localFile);

        Assert.True(File.Exists(_localFile));
        Assert.True(File.Exists(_runFile), "the Run version's original must be untouched");
        Assert.Equal(File.ReadAllText(_runFile), File.ReadAllText(_localFile));
    }

    [Fact]
    public void Import_ThenReload_LibraryShowsMigratedEntry()
    {
        WriteRunFile(RunJson);
        using var library = new PromptLibrary(_localDir);
        library.Initialize(createTemplateIfMissing: false);

        Assert.Empty(library.Entries); // no template was generated while import was pending

        RunDataImport.Import(_runFile, _localFile);
        library.Reload();

        var entry = Assert.Single(library.Entries);
        Assert.Equal("Legacy prompt", entry.Title);
        Assert.Equal(11, entry.UseCount);
        Assert.True(entry.Favorite);
    }
}
