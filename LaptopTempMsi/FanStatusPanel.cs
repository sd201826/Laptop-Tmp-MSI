namespace LaptopTempMsi;

public sealed class FanStatusPanel : Panel
{
    private readonly Label _title;
    private readonly Label _rpm;
    private readonly Label _details;
    private readonly Label _mode;

    public FanStatusPanel()
    {
        BackColor = Color.FromArgb(13, 15, 20);
        Margin = new Padding(8);
        MinimumSize = new Size(180, 140);
        DoubleBuffered = true;

        _title = new Label
        {
            Text = "FAN SPEED",
            Location = new Point(14, 13),
            Size = new Size(250, 20),
            ForeColor = Color.FromArgb(164, 171, 183),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            BackColor = Color.Transparent
        };

        _rpm = new Label
        {
            Text = "N/A",
            Location = new Point(14, 39),
            Size = new Size(250, 42),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 26F, FontStyle.Bold),
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        _details = new Label
        {
            Text = "Waiting for fan RPM sensors...",
            Location = new Point(14, 84),
            Size = new Size(260, 20),
            ForeColor = Color.FromArgb(139, 148, 162),
            Font = new Font("Segoe UI", 8F),
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        _mode = new Label
        {
            Text = "AUTOMATIC FAN CONTROL",
            Location = new Point(14, 111),
            Size = new Size(270, 18),
            ForeColor = Color.FromArgb(151, 159, 172),
            Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        Controls.AddRange(new Control[] { _title, _rpm, _details, _mode });

        Resize += (_, _) =>
        {
            int width = Math.Max(90, ClientSize.Width - 28);
            _title.Width = width;
            _rpm.Width = width;
            _details.Width = width;
            _mode.Width = width;
        };
    }

    public void SetReadings(IReadOnlyList<FanReading> readings)
    {
        var available = readings
            .Where(r => double.IsFinite(r.Rpm) && r.Rpm >= 0)
            .OrderByDescending(r => r.Rpm)
            .ToList();

        if (available.Count == 0)
        {
            _rpm.Text = "N/A";
            _details.Text = "No fan RPM sensor reported";
            _mode.Text = "AUTOMATIC FAN CONTROL";
            return;
        }

        _rpm.Text = $"{available[0].Rpm:0} RPM";

        if (available.Count == 1)
        {
            _details.Text = ShortName(available[0].Name, 34);
        }
        else
        {
            _details.Text = $"{available.Count} fan sensors detected";
        }

        _mode.Text = "LIVE READING  /  FIRMWARE CONTROL";
    }

    private static string ShortName(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var border = new Pen(Color.FromArgb(95, 235, 240, 248), 1F);
        e.Graphics.DrawRectangle(border, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
        using var accent = new Pen(Color.FromArgb(210, 245, 247, 250), 2F);
        e.Graphics.DrawLine(accent, 14, 0, 54, 0);
    }
}