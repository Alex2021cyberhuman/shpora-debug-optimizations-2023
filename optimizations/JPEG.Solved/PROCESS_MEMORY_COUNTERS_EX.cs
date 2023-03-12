using System;
using System.Runtime.InteropServices;

namespace JPEG.Solved;

[StructLayout(LayoutKind.Sequential)]
public struct PROCESS_MEMORY_COUNTERS_EX
{
    public Int32 cb;
    public Int32 PageFaultCount;
    public IntPtr PeakWorkingSetSize;
    public IntPtr WorkingSetSize;
    public IntPtr QuotaPeakPagedPoolUsage;
    public IntPtr QuotaPagedPoolUsage;
    public IntPtr QuotaPeakNonPagedPoolUsage;
    public IntPtr QuotaNonPagedPoolUsage;
    public IntPtr PagefileUsage;
    public IntPtr PeakPagefileUsage;
    public IntPtr PrivateUsage;
}