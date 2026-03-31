using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class MacFolderPicker
{
#if UNITY_STANDALONE_OSX
    [DllImport("__Internal")]
    private static extern IntPtr PickFolderNative(string title);

    public static string PickFolder(string title)
    {
        IntPtr ptr = PickFolderNative(title);

        if (ptr == IntPtr.Zero)
            return null;

        return Marshal.PtrToStringAnsi(ptr);
    }
#else
    public static string PickFolder(string title)
    {
        Debug.LogWarning("MacFolderPicker is only available in macOS standalone builds.");
        return null;
    }
#endif
}