using LibreHardwareMonitor.Hardware;
using System.Runtime.InteropServices;

namespace LaptopTempMsi;

public sealed class HardwareMonitor : IDisposable
{
    private readonly Computer _computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMotherboardEnabled = true,
        IsMemoryEnabled = true,
        IsStorageEnabled = true,
        IsControllerEnabled = true
    };

    private bool _opened;
    private string? _openError;

    public HardwareMonitor()
    {
        try
        {
            _computer.Open();
            _opened = true;
        }
        catch (Exception ex)
        {
            _openError = ex.Message;
        }
    }

    public SensorSnapshot Read()
    {
        var sensors = new List<SensorReading>();
        string? error = _openError;

        if (_opened)
        {
            try
            {
                _computer.Accept(new UpdateVisitor());

                foreach (IHardware hardware in _computer.Hardware)
                    CollectHardware(hardware, sensors);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
        }

        var cpuSensors = sensors.Where(s => s.HardwareType == HardwareType.Cpu).ToList();
        var gpuSensors = sensors.Where(s =>
            s.HardwareType.ToString().StartsWith("Gpu", StringComparison.OrdinalIgnoreCase)).ToList();
        var storageSensors = sensors.Where(s => s.HardwareType == HardwareType.Storage).ToList();

        string? cpuName = sensors.FirstOrDefault(s => s.HardwareType == HardwareType.Cpu)?.HardwareName;
        string? gpuName = gpuSensors.FirstOrDefault()?.HardwareName;

        return new SensorSnapshot(
            CpuTemperature: PickTemperature(cpuSensors),
            GpuTemperature: PickTemperature(gpuSensors),
            CpuLoad: PickLoad(cpuSensors),
            MemoryLoad: ReadMemoryLoad(),
            FanReadings: sensors
                .Where(s => s.SensorType == SensorType.Fan && s.Value >= 0)
                .GroupBy(s => $"{s.HardwareName} / {s.SensorName}", StringComparer.OrdinalIgnoreCase)
                .Select(group => new FanReading(group.Key, group.Max(s => s.Value)))
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            StorageTemperature: PickTemperature(storageSensors),
            CpuName: cpuName,
            GpuName: gpuName,
            Error: error,
            UpdatedAt: DateTime.Now);
    }

    private static void CollectHardware(IHardware hardware, List<SensorReading> destination)
    {
        foreach (ISensor sensor in hardware.Sensors)
        {
            if (sensor.Value is float value && float.IsFinite(value))
            {
                destination.Add(new SensorReading(
                    hardware.HardwareType,
                    hardware.Name,
                    sensor.Name,
                    sensor.SensorType,
                    value));
            }
        }

        foreach (IHardware child in hardware.SubHardware)
            CollectHardware(child, destination);
    }

    private static double? PickTemperature(IEnumerable<SensorReading> source)
    {
        var values = source
            .Where(s => s.SensorType == SensorType.Temperature)
            .ToList();

        if (values.Count == 0)
            return null;

        foreach (string hint in new[] { "package", "tctl", "die" })
        {
            var preferred = values.FirstOrDefault(s =>
                s.SensorName.Contains(hint, StringComparison.OrdinalIgnoreCase));

            if (preferred is not null)
                return preferred.Value;
        }

        return values.Max(s => s.Value);
    }

    private static double? PickLoad(IEnumerable<SensorReading> source)
    {
        var values = source
            .Where(s => s.SensorType == SensorType.Load)
            .ToList();

        if (values.Count == 0)
            return null;

        var total = values.FirstOrDefault(s =>
            s.SensorName.Contains("total", StringComparison.OrdinalIgnoreCase));

        return total?.Value ?? values.Average(s => s.Value);
    }

    private static double? PickMaximum(IEnumerable<SensorReading> source)
    {
        var values = source.Select(s => s.Value).ToList();
        return values.Count == 0 ? null : values.Max();
    }

    private static double? ReadMemoryLoad()
    {
        try
        {
            var status = new MemoryStatusEx
            {
                Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
            };

            return GlobalMemoryStatusEx(ref status) ? status.MemoryLoad : null;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (!_opened)
            return;

        try
        {
            _computer.Close();
        }
        catch
        {
            // Sensor shutdown errors should not prevent the application from closing.
        }

        _opened = false;
    }

    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();

            foreach (IHardware child in hardware.SubHardware)
                child.Accept(this);
        }

        public void VisitSensor(ISensor sensor) { }

        public void VisitParameter(IParameter parameter) { }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    private sealed record SensorReading(
        HardwareType HardwareType,
        string HardwareName,
        string SensorName,
        SensorType SensorType,
        double Value);
}

public sealed record FanReading(string Name, double Rpm);

public sealed record SensorSnapshot(
    double? CpuTemperature,
    double? GpuTemperature,
    double? CpuLoad,
    double? MemoryLoad,
    IReadOnlyList<FanReading> FanReadings,
    double? StorageTemperature,
    string? CpuName,
    string? GpuName,
    string? Error,
    DateTime UpdatedAt);