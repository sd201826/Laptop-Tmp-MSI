namespace LaptopTempMsi;

public sealed class FanStatusPanel : Panel
{
    private readonly Label _title;
    private readonly Label _rpm;
    private readonly Label _details;
    private readonly Label _mode;
    private readonly Button _decrease;
    private readonly Button _increase;
    private readonly Button _reset;
    private readonly ToolTip _toolTip;

    public FanStatusPanel()
    {
        BackColor = Color.FromArgb(13, 15, 20);
        Margin = new Padding(8);
        MinimumSize = new Size(180, 140);
        DoubleBuffered = true;

        _title = new Label
        {
            Text = "FAN SPEED",
            Location = new Point(14, 10),
            Size = new Size(250, 18),
            ForeColor = Color.FromArgb(164, 171, 183),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            BackColor = Color.Transparent
        };

        _rpm = new Label
        {
            Text = "N/A",
            Location = new Point(14, 29),
            Size = new Size(250, 39),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 25F, FontStyle.Bold),
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        _details = new Label
        {
            Text = "Waiting for fan sensors...",
            Location = new Point(14, 68),
            Size = new Size(260, 19),
            ForeColor = Color.FromArgb(139, 148, 162),
            Font = new Font("Segoe UI", 8F),
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        _decrease = CreateControlButton("DOWN", 54);
        _increase = CreateControlButton("UP", 44);
        _reset = CreateControlButton("RESET", 66);

        _decrease.Location = new Point(14, 91);
        _increase.Location = new Point(74, 91);
        _reset.Location = new Point(124, 91);

        // Fan speed writes are intentionally not sent to the embedded controller.
        // The app can report RPM, but it cannot safely assume a laptop-specific fan protocol.
        _decrease.Enabled = false;
        _increase.Enabled = false;
        _reset.Enabled = false;

        _mode = new Label
        {
            Text = "FIRMWARE AUTO  /  READ ONLY",
            Location = new Point(14, 123),
            Size = new Size(270, 17),
            ForeColor = Color.FromArgb(151, 159, 172),
            Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        _toolTip = new ToolTip();
        const string unavailable = "Manual fan control is not supported by this app. Your laptop firmware manages fan speed.";
        _toolTip.SetToolTip(_decrease, unavailable);
        _toolTip.SetToolTip(_increase, unavailable);
        _toolTip.SetToolTip(_reset, unavailable);

        Controls.AddRange(new Control[] { _title, _rpm, _details, _decrease, _increase, _reset, _mode });

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
            _mode.Text = "FIRMWARE AUTO  /  READ ONLY";
            return;
        }

        _rpm.Text = $"{available[0].Rpm:0} RPM";

        string[] labels = available
            .Take(2)
            .Select((r, i) => $"{ShortName(r.Name, 16)}: {r.Rpm:0} RPM")
            .ToArray();

        _details.Text = string.Join("   •   ", labels);
        if (available.Count > 2)
            _details.Text += $"   +{available.Count - 2} more";

        _mode.Text = "FIRMWARE AUTO  /  READ ONLY";
    }

    private static Button CreateControlButton(string text, int width)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 27,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(18, 20, 26),
            ForeColor = Color.FromArgb(135, 143, 157),
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            Cursor = Cursors.Default,
            TabStop = false
        };

        button.FlatAppearance.BorderColor = Color.FromArgb(75, 225, 231, 240);
        button.FlatAppearance.BorderSize = 1;
        return button;
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _toolTip.Dispose();

        base.Dispose(disposing);
    }
}