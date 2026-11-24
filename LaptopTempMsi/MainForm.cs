using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;

namespace LaptopTempMsi;

public sealed class MainForm : Form
{
    private readonly HardwareMonitor _monitor;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly MetricCard _cpuTemp;
    private readonly MetricCard _gpuTemp;
    private readonly MetricCard _cpuLoad;
    private readonly MetricCard _memoryLoad;
    private readonly MetricCard _fanSpeed;
    private readonly MetricCard _storageTemp;
    private readonly TemperatureGraph _graph;
    private readonly Label _statusLabel;
    private readonly Button _adminButton;

    private static readonly Color Canvas = Color.FromArgb(8, 10, 14);
    private static readonly Color TextWhite = Color.FromArgb(245, 247, 250);
    private static readonly Color Muted = Color.FromArgb(145, 154, 168);

    public MainForm()
    {
        Text = "Laptop TEMP MSI";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(860, 620);
        ClientSize = new Size(1060, 720);
        BackColor = Canvas;
        ForeColor = TextWhite;
        Font = new Font("Segoe UI", 9F);
        DoubleBuffered = true;

        _monitor = new HardwareMonitor();

        _cpuTemp = new MetricCard("CPU TEMPERATURE", "Processor package temperature");
        _gpuTemp = new MetricCard("GPU TEMPERATURE", "Graphics sensor, when available");
        _cpuLoad = new MetricCard("CPU LOAD", "Total processor activity");
        _memoryLoad = new MetricCard("RAM USAGE", "Physical memory in use");
        _fanSpeed = new MetricCard("FAN SPEED", "Reported fan speed in RPM");
        _storageTemp = new MetricCard("STORAGE TEMPERATURE", "Drive sensor, when available");
        _graph = new TemperatureGraph();

        _statusLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Muted,
            Font = new Font("Segoe UI", 8.5F),
            Text = "INITIALIZING HARDWARE SENSORS..."
        };

        _adminButton = CreateButton(IsAdministrator() ? "ADMIN ACTIVE" : "RUN AS ADMIN", 142);
        _adminButton.Enabled = !IsAdministrator();
        _adminButton.Click += (_, _) => RestartAsAdministrator();

        BuildLayout();

        _timer = new System.Windows.Forms.Timer { Interval = 2000 };
        _timer.Tick += (_, _) => RefreshReadings();
        _timer.Start();

        RefreshReadings();

        FormClosed += (_, _) =>
        {
            _timer.Stop();
            _timer.Dispose();
            _monitor.Dispose();
        };
    }

    private void BuildLayout()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 104,
            BackColor = Canvas,
            Padding = new Padding(26, 17, 26, 10)
        };

        var mark = new Panel
        {
            Location = new Point(26, 21),
            Size = new Size(48, 48),
            BackColor = Color.FromArgb(14, 17, 23)
        };
        mark.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(220, 245, 247, 250), 1.2F);
            e.Graphics.DrawRectangle(pen, 0, 0, 47, 47);
            using var font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            using var brush = new SolidBrush(TextWhite);
            var size = e.Graphics.MeasureString("LT", font);
            e.Graphics.DrawString("LT", font, brush, (48 - size.Width) / 2, (48 - size.Height) / 2 - 1);
        };

        var title = new Label
        {
            Text = "LAPTOP TEMP MSI",
            Location = new Point(88, 19),
            Size = new Size(390, 30),
            ForeColor = TextWhite,
            Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold)
        };

        var subtitle = new Label
        {
            Text = $"REAL-TIME HARDWARE MONITOR  /  {Environment.MachineName.ToUpperInvariant()}",
            Location = new Point(90, 52),
            Size = new Size(470, 20),
            ForeColor = Muted,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold)
        };

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 310,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 13, 0, 0),
            BackColor = Canvas
        };

        var refreshButton = CreateButton("REFRESH", 105);
        refreshButton.Click += (_, _) => RefreshReadings();
        actions.Controls.Add(refreshButton);
        actions.Controls.Add(_adminButton);

        header.Controls.Add(mark);
        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        header.Controls.Add(actions);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Canvas,
            Padding = new Padding(20, 10, 20, 12),
            ColumnCount = 3,
            RowCount = 3
        };

        for (int i = 0; i < 3; i++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));

        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 158));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 158));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        grid.Controls.Add(_cpuTemp, 0, 0);
        grid.Controls.Add(_gpuTemp, 1, 0);
        grid.Controls.Add(_cpuLoad, 2, 0);
        grid.Controls.Add(_memoryLoad, 0, 1);
        grid.Controls.Add(_fanSpeed, 1, 1);
        grid.Controls.Add(_storageTemp, 2, 1);
        grid.Controls.Add(_graph, 0, 2);
        grid.SetColumnSpan(_graph, 3);

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            BackColor = Canvas,
            Padding = new Padding(28, 0, 28, 5)
        };

        _statusLabel.Text = "SENSOR STATUS: WAITING";
        footer.Controls.Add(_statusLabel);

        header.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(65, 235, 240, 248), 1F);
            e.Graphics.DrawLine(pen, 26, header.Height - 1, header.Width - 26, header.Height - 1);
        };

        footer.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(65, 235, 240, 248), 1F);
            e.Graphics.DrawLine(pen, 26, 0, footer.Width - 26, 0);
        };

        Controls.Add(grid);
        Controls.Add(footer);
        Controls.Add(header);
    }

    private Button CreateButton(string text, int width)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 36,
            Margin = new Padding(7, 0, 0, 0),
            BackColor = Color.FromArgb(13, 15, 20),
            ForeColor = TextWhite,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        button.FlatAppearance.BorderColor = Color.FromArgb(155, 225, 231, 240);
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(36, 40, 48);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 56, 64);
        return button;
    }

    private void RefreshReadings()
    {
        try
        {
            SensorSnapshot snapshot = _monitor.Read();

            _cpuTemp.SetValue(FormatTemperature(snapshot.CpuTemperature),
                snapshot.CpuName ?? (snapshot.CpuTemperature.HasValue ? "CPU package / core sensor" : "No CPU temperature sensor reported"));

            _gpuTemp.SetValue(FormatTemperature(snapshot.GpuTemperature),
                snapshot.GpuName ?? (snapshot.GpuTemperature.HasValue ? "Graphics temperature sensor" : "No GPU temperature sensor reported"));

            _cpuLoad.SetValue(FormatPercent(snapshot.CpuLoad), "Processor activity");
            _memoryLoad.SetValue(FormatPercent(snapshot.MemoryLoad), "Physical memory in use");
            _fanSpeed.SetValue(snapshot.FanRpm.HasValue ? $"{snapshot.FanRpm.Value:0} RPM" : "N/A",
                snapshot.FanRpm.HasValue ? "Reported fan speed" : "No fan speed sensor reported");
            _storageTemp.SetValue(FormatTemperature(snapshot.StorageTemperature),
                snapshot.StorageTemperature.HasValue ? "Drive temperature sensor" : "No storage temperature sensor reported");

            _graph.AddSample(snapshot.CpuTemperature);

            if (snapshot.Error is not null)
            {
                _statusLabel.Text = "SENSOR WARNING  /  " + Shorten(snapshot.Error, 105);
            }
            else if (!snapshot.CpuTemperature.HasValue && !snapshot.GpuTemperature.HasValue && !snapshot.StorageTemperature.HasValue)
            {
                _statusLabel.Text = "NO TEMPERATURE SENSOR REPORTED  /  TRY RUN AS ADMIN IF YOUR LAPTOP REQUIRES IT";
            }
            else
            {
                _statusLabel.Text = $"LIVE SENSOR DATA  /  UPDATED {snapshot.UpdatedAt:HH:mm:ss}";
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "READ ERROR  /  " + Shorten(ex.Message, 100);
        }
    }

    private static string FormatTemperature(double? value) =>
        value.HasValue ? $"{value.Value:0.0}°C" : "N/A";

    private static string FormatPercent(double? value) =>
        value.HasValue ? $"{Math.Clamp(value.Value, 0, 100):0}%" : "N/A";

    private static string Shorten(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";

    private static bool IsAdministrator()
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private void RestartAsAdministrator()
    {
        try
        {
            string executable = Environment.ProcessPath ?? Application.ExecutablePath;
            Process.Start(new ProcessStartInfo(executable)
            {
                UseShellExecute = true,
                Verb = "runas"
            });

            Close();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            _statusLabel.Text = "ADMIN REQUEST CANCELLED  /  APP IS STILL RUNNING NORMALLY";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "ADMIN START FAILED  /  " + Shorten(ex.Message, 85);
        }
    }
}