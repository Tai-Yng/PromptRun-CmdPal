// Copied from Tai-Yng/PromptRun (Run version, v0.1.0) on 2026-09-27 and made public
// for cross-assembly use (design D2).

namespace PromptRun.Sync;

/// <summary>Implemented by the GitHub sync coordinator (wired by the host once settings exist).</summary>
public interface ISyncCoordinator
{
    void Push();

    void Pull();
}
