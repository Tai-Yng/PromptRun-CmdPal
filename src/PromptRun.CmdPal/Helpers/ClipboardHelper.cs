// Copyright (c) Tai-Yng. MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace PromptRun.CmdPal;

/// <summary>
/// Win32 clipboard writer. The WinRT Clipboard API fails hard while the clipboard is
/// momentarily held by another app; this implementation retries the whole open/empty/set
/// cycle for ~250 ms and reports the precise Win32 error if it still fails. Data written
/// via SetClipboardData is owned by the OS, so it survives our process exiting — no Flush.
/// </summary>
internal static partial class ClipboardHelper
{
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenClipboard(IntPtr hWndNewOwner);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GlobalAlloc(uint uFlags, nuint dwBytes);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GlobalLock(IntPtr hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalUnlock(IntPtr hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GlobalFree(IntPtr hMem);

    public static void SetText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var bytes = Encoding.Unicode.GetBytes(text + "\0");
        Exception? last = null;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                last = Win32("OpenClipboard");
                Thread.Sleep(25);
                continue;
            }

            try
            {
                if (!EmptyClipboard())
                {
                    throw Win32("EmptyClipboard");
                }

                var hMem = GlobalAlloc(GMEM_MOVEABLE, (nuint)bytes.Length);
                if (hMem == IntPtr.Zero)
                {
                    throw Win32("GlobalAlloc");
                }

                var ptr = GlobalLock(hMem);
                if (ptr == IntPtr.Zero)
                {
                    GlobalFree(hMem);
                    throw Win32("GlobalLock");
                }

                try
                {
                    Marshal.Copy(bytes, 0, ptr, bytes.Length);
                }
                finally
                {
                    GlobalUnlock(hMem);
                }

                if (SetClipboardData(CF_UNICODETEXT, hMem) == IntPtr.Zero)
                {
                    GlobalFree(hMem);
                    throw Win32("SetClipboardData");
                }

                return; // success — the OS owns hMem now
            }
            finally
            {
                CloseClipboard();
            }
        }

        throw last ?? new Exception("clipboard unavailable");
    }

    private static Exception Win32(string operation) =>
        new($"{operation} failed (Win32 error {Marshal.GetLastWin32Error()})");
}
