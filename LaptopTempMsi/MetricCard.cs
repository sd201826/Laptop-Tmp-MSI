namespace LaptopTempMsi;

public sealed class MetricCard : Panel
{
    private readonly Label _title;
    private readonly Label _value;
    private readonly Label _detail;

    public MetricCard(string title, string detail)
    {
        BackColor = Color.FromArgb(13, 15, 20);
        Margin = new Padding(8);
        Padding = new Padding(18);
        MinimumSize = new Size(180, 140);
        DoubleBuffered = true;

        _title = new Label
        {
            Text = title.ToUpperInvariant(),
            AutoSize = false,
            Location = new Point(18, 15),
            Size = new Size(240, 22),
            ForeColor = Color.FromArgb(164, 171, 183),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            BackColor = Color.Transparent
        };

        _value = new Label
        {
            Text = "N/A",
            AutoSize = false,
            Location = new Point(18, 47),
            Size = new Size(270, 54),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 30F, FontStyle.Bold),
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        _detail = new Label
        {
            Text = detail,
            AutoSize = false,
            Location = new Point(18, 111),
            Size = new Size(270, 22),
            ForeColor = Color.FromArgb(124, 132, 145),
            Font = new Font("Segoe UI", 8.5F),
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        Controls.Add(_title);
        Controls.Add(_value);
        Controls.Add(_detail);
        Resize += (_, _) =>
        {
            int width = Math.Max(80, ClientSize.Width - 36);
            _title.Width = width;
            _value.Width = width;
            _detail.Width = width;
        };
    }

    public void SetValue(string value, string detail)
    {
        _value.Text = value;
        _detail.Text = detail;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Color.FromArgb(95, 235, 240, 248), 1F);
        e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
        using var accent = new Pen(Color.FromArgb(210, 245, 247, 250), 2F);
        e.Graphics.DrawLine(accent, 18, 0, 58, 0);
    }
}