// Copyright (c) Tai-Yng. MIT license.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PromptRun.Sync;

namespace PromptRun.CmdPal.Commands;

/// <summary>Shared sink so the coordinator's notify messages surface as toasts.</summary>
public sealed class SyncMessageSink
{
    public string? Last { get; private set; }

    public void Receive(string message) => Last = message;
}

/// <summary>Runs Push or Pull synchronously and reports the coordinator's message as a toast.</summary>
public sealed partial class SyncCommand : InvokableCommand
{
    private readonly ISyncCoordinator _sync;
    private readonly SyncMessageSink _sink;
    private readonly bool _push;

    public SyncCommand(ISyncCoordinator sync, SyncMessageSink sink, bool push)
    {
        _sync = sync;
        _sink = sink;
        _push = push;
        Name = push ? "Push to GitHub" : "Pull from GitHub";
        Icon = new IconInfo(push ? "\uE898" : "\uE896"); // Send / Download glyphs
    }

    public override CommandResult Invoke()
    {
        if (_push) _sync.Push();
        else _sync.Pull();

        return CommandResult.ShowToast(new ToastArgs
        {
            Message = _sink.Last ?? "Sync finished",
            Result = CommandResult.Dismiss(),
        });
    }
}
