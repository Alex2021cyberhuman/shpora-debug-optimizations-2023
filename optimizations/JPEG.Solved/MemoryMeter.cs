using System.Diagnostics;
using System.Runtime.InteropServices;

namespace JPEG.Solved;

public static class MemoryMeter
{
	private static Process process = Process.GetCurrentProcess();

	public static long PrivateBytes()
	{
		var sizeOfCountersEx = Marshal.SizeOf<PROCESS_MEMORY_COUNTERS_EX>();
		return PInvoke.GetProcessMemoryInfo(process.Handle, out var counters, sizeOfCountersEx)
			? counters.PrivateUsage.ToInt64()
			: 0;
	}

	public static long PeakPrivateBytes()
	{
		var sizeOfCountersEx = Marshal.SizeOf<PROCESS_MEMORY_COUNTERS_EX>();
		return PInvoke.GetProcessMemoryInfo(process.Handle, out var counters, sizeOfCountersEx)
			? counters.PeakPagefileUsage.ToInt64()
			: 0;
	}

	public static long PeakWorkingSet()
	{
		var sizeOfCountersEx = Marshal.SizeOf<PROCESS_MEMORY_COUNTERS_EX>();
		return PInvoke.GetProcessMemoryInfo(process.Handle, out var counters, sizeOfCountersEx)
			? counters.PeakWorkingSetSize.ToInt64()
			: 0;
	}
}