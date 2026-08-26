namespace modshell_hwtest.Models;

public record HardwareSnapshot(
    string CpuName,
    float CpuUsage,
    float? CpuTempC,
    double[] CpuPerCoreUsage,
    string GpuName,
    float GpuUsage,
    float? GpuTempC,
    float? GpuPowerWatts,
    float? GpuVramUsedMb,
    float? GpuVramTotalMb,
    float MemUsedGb,
    float MemTotalGb,
    float NetUpBytesPerSec,
    float NetDownBytesPerSec,
    double? PingMs);
