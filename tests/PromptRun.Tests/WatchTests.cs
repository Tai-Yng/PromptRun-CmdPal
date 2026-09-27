using PromptRun.Library;

namespace PromptRun.Tests;

public sealed class WatchTests : IDisposable
{
    private readonly string _dir;
    private readonly PromptLibrary _library;

    public WatchTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "promptrun-watch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _library = new PromptLibrary(_dir);
    }

    public void Dispose()
    {
        _library.Dispose();
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public void ExternalEdit_TriggersDebouncedReload()
    {
        _library.Initialize();
        var fired = new ManualResetEventSlim(false);
        _library.Changed += () => fired.Set();

        File.WriteAllText(_library.FilePath,
            """{"version":1,"prompts":[{"id":"1","title":"Fresh","content":"edited externally"}]}""");

        Assert.True(fired.Wait(TimeSpan.FromSeconds(5)), "Changed event was not raised in time");
        Assert.Equal("Fresh", _library.Entries[0].Title);
    }

    [Fact]
    public void WriteBack_DoesNotSelfTriggerReload()
    {
        _library.Initialize();
        var changedCount = 0;
        _library.Changed += () => Interlocked.Increment(ref changedCount);

        _library.IncrementUseCount(_library.Entries[0].Id);

        Thread.Sleep(1500); // debounce (300ms) + suppression window (900ms) + margin
        Assert.Equal(0, Volatile.Read(ref changedCount));
        Assert.Equal(1, _library.Entries[0].UseCount);
    }
}
