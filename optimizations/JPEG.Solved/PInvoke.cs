using System;
using System.Runtime.InteropServices;

namespace JPEG.Solved;

internal static class PInvoke
{
    private const string Psapi = "Psapi.dll";
    private const string Kernel32 = "kernel32.dll";
    private const string User32 = "user32.dll";


    [DllImport(Psapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS_EX counters,
        Int32 cb);
}