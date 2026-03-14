using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;

namespace AuxiliumLab.AiSandbox.Common.Helpers;

public class SystemInfo
{
    /// <summary>
    /// Gets the number of physical CPU cores, accounting for hyperthreading.
    /// Works on Windows, Linux, and macOS with fallback for unknown platforms.
    /// </summary>
    /// <returns>Number of physical cores</returns>
    public static int GetPhysicalCoreCount()
    {
        // Windows: Use WMI to query actual physical cores
        if (OperatingSystem.IsWindows())
        {
            try
            {
                int coreCount = 0;
                foreach (var item in new ManagementObjectSearcher("Select NumberOfCores from Win32_Processor").Get())
                {
                    coreCount += int.Parse(item["NumberOfCores"]?.ToString() ?? "0");
                }

                if (coreCount > 0)
                {
                    return coreCount; // Successfully detected physical cores via WMI
                }
            }
            catch
            {
                // WMI query failed (insufficient permissions or other error) - continue to fallback
            }
        }

        // Linux: Parse /proc/cpuinfo for accurate physical core count
        if (OperatingSystem.IsLinux())
        {
            try
            {
                if (File.Exists("/proc/cpuinfo"))
                {
                    var cpuInfo = File.ReadAllLines("/proc/cpuinfo");

                    // Count unique combinations of (physical id, core id)
                    var physicalCores = cpuInfo
                        .Select((line, index) => new { line, index })
                        .Where(x => x.line.StartsWith("physical id") || x.line.StartsWith("core id"))
                        .GroupBy(x => cpuInfo.Skip(x.index).Take(20).FirstOrDefault(l => l.StartsWith("processor")))
                        .Select(g => new
                        {
                            PhysicalId = g.FirstOrDefault(x => x.line.StartsWith("physical id"))?.line.Split(':').LastOrDefault()?.Trim(),
                            CoreId = g.FirstOrDefault(x => x.line.StartsWith("core id"))?.line.Split(':').LastOrDefault()?.Trim()
                        })
                        .Where(x => x.PhysicalId != null && x.CoreId != null)
                        .Select(x => $"{x.PhysicalId}-{x.CoreId}")
                        .Distinct()
                        .Count();

                    if (physicalCores > 0)
                    {
                        return physicalCores; // Successfully detected physical cores on Linux
                    }
                }
            }
            catch
            {
                // Failed to parse /proc/cpuinfo - continue to fallback
            }
        }

        // Fallback heuristic for macOS or if detection failed:
        // Assume hyperthreading if logical cores > 4, otherwise use logical cores directly
        int logicalCores = Environment.ProcessorCount;

        // No hyperthreading on very low-end CPUs (1-2 cores)
        if (logicalCores <= 2)
            return logicalCores;

        // Assume hyperthreading on modern multi-core CPUs
        // Most CPUs with 4+ logical cores have HT (Intel) or SMT (AMD)
        return logicalCores / 2;
    }
}

