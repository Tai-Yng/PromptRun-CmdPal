// Migrated from Tai-Yng/PromptRun (Run version, v0.1.0) on 2026-09-27; the
// DataPaths assertion now targets the CmdPal data directory.

using PromptRun.Library;

namespace PromptRun.Tests;

public sealed class PromptLibraryTests : IDisposable
{
    private readonly string _dir;
    private readonly PromptLibrary _library;

    public PromptLibraryTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "promptrun-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _library = new PromptLibrary(_dir);
    }

    public void Dispose()
    {
        _library.Dispose();
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    private void WriteFile(string json) =>
        File.WriteAllText(Path.Combine(_dir, DataPaths.DataFileName), json);

    [Fact]
    public void Reload_ValidFileWithArbitraryFieldOrder_LoadsEntries()
    {
        WriteFile("""
        {
          "prompts": [
            { "content": "hello world", "title": "Greeting", "extraField": 123,
              "id": "1", "tags": ["demo"], "favorite": true, "useCount": 7,
              "createdAt": 100, "updatedAt": 200 }
          ],
          "version": 1,
          "unknownTopLevel": true
        }
        """);

        _library.Reload();

        Assert.False(_library.IsCorrupt);
        var entry = Assert.Single(_library.Entries);
        Assert.Equal("Greeting", entry.Title);
        Assert.Equal("hello world", entry.Content);
        Assert.Equal(7, entry.UseCount);
        Assert.True(entry.Favorite);
        Assert.Equal("demo", Assert.Single(entry.Tags));
    }

    [Fact]
    public void Reload_EntryMissingContent_IsSkippedOthersKept()
    {
        WriteFile("""{"version":1,"prompts":[{"id":"1","title":"Good","content":"x"},{"id":"2","title":"NoContent"}]}""");

        _library.Reload();

        var entry = Assert.Single(_library.Entries);
        Assert.Equal("Good", entry.Title);
    }

    [Fact]
    public void Reload_EntryMissingTitle_IsSkipped()
    {
        WriteFile("""{"version":1,"prompts":[{"id":"1","content":"orphan"}]}""");

        _library.Reload();

        Assert.Empty(_library.Entries);
    }

    [Fact]
    public void Reload_CorruptJson_DegradesToEmptyLibrary()
    {
        WriteFile("{ this is not valid json !!!");

        _library.Reload();

        Assert.True(_library.IsCorrupt);
        Assert.Empty(_library.Entries);
    }

    [Fact]
    public void Initialize_MissingFile_CreatesHandEditableTemplate()
    {
        Assert.False(File.Exists(_library.FilePath));

        _library.Initialize();

        Assert.True(_library.TemplateCreated);
        Assert.True(_library.Entries.Count >= 2);
        Assert.False(_library.IsCorrupt);

        // No BOM, 2-space indent, CJK kept readable.
        var bytes = File.ReadAllBytes(_library.FilePath);
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
            "template must be written without a BOM");
        var json = File.ReadAllText(_library.FilePath);
        Assert.Contains("\n  \"", json);
        Assert.Contains("专业翻译助手", json);
    }

    [Fact]
    public void DataPaths_Default_MatchesCmdPalDataDirectory()
    {
        var path = DataPaths.DefaultBasePath();

        Assert.EndsWith(Path.Combine("PowerToys", "CmdPal", "PromptRun"), path);
    }

    [Fact]
    public void DataPaths_RunVersion_PointsAtRunPluginSettingsDirectory()
    {
        var path = DataPaths.RunVersionFilePath();

        Assert.EndsWith(Path.Combine("PowerToys Run", "Settings", "Plugins", "PromptRun", "prompts.json"), path);
    }

    [Fact]
    public void IncrementUseCount_BumpsCountAndFileStaysReadable()
    {
        _library.Initialize();
        var id = _library.Entries[0].Id;

        Assert.True(_library.IncrementUseCount(id));
        Assert.Equal(1, _library.Entries[0].UseCount);

        // A fresh instance must be able to read the written file back.
        using var fresh = new PromptLibrary(_dir);
        fresh.Reload();
        Assert.Equal(1, Assert.Single(fresh.Entries, e => e.Id == id).UseCount);
    }

    [Fact]
    public void IncrementUseCount_UnknownId_ReturnsFalse()
    {
        _library.Initialize();

        Assert.False(_library.IncrementUseCount("no-such-id"));
    }
}
