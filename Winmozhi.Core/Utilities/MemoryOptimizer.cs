using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Winmozhi.Core.Utilities;

public static partial class MemoryOptimizer
{
    // Windows API to flush unused memory pages from RAM to the pagefile
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetProcessWorkingSetSize(IntPtr proc, nint min, nint max);

    /// <summary>
    /// Forces the garbage collector to run and tells Windows to flush unused 
    /// memory out of active RAM. Ideal for background tray applications.
    /// </summary>
    public static void TrimMemory()
    {
        try
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();

            using var process = Process.GetCurrentProcess();
            SetProcessWorkingSetSize(process.Handle, -1, -1);
        }
        catch
        {
            // Fail silently. Memory trimming is a non-critical optimization.
        }
    }
}