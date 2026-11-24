namespace LaptopTempMsi;

public sealed class TemperatureGraph : Panel
{
    private readonly Queue<double> _samples = new();
    private const int MaxSamples = 60;

    public TemperatureGraph()
    {
        BackColor = Color.FromArgb(13, 15, 20);
        Margin = new Padding(8);
        DoubleBuffered = true;
        MinimumSize = new Size(400, 150);
    }

    public void AddSample(double? temperature)
    {
        if (temperature.HasValue && double.IsFinite(temperature.Value))
        {
            _samples.Enqueue(temperature.Value);
            while (_samples.Count > MaxSamples)
                _samples.Dequeue();
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var borderPen = new Pen(Color.FromArgb(95, 235, 240, 248), 1F);
        graphics.DrawRectangle(borderPen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));

        using var titleBrush = new SolidBrush(Color.FromArgb(180, 190, 202));
        using var titleFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        graphics.DrawString("CPU TEMPERATURE  /  LAST 2 MINUTES", titleFont, titleBrush, 18, 15);

        int left = 54;
        int right = Width - 22;
        int top = 52;
        int bottom = Height - 24;

        if (right <= left || bottom <= top)
            return;

        using var gridPen = new Pen(Color.FromArgb(35, 230, 235, 245), 1F);
        using var axisBrush = new SolidBrush(Color.FromArgb(115, 160, 169, 184));
        using var axisFont = new Font("Segoe UI", 8F);

        foreach (int temperature in new[] { 100, 80, 60, 40, 20 })
        {
            float y = MapY(temperature, top, bottom);
            graphics.DrawLine(gridPen, left, y, right, y);
            graphics.DrawString($"{temperature}°", axisFont, axisBrush, 12, y - 8);
        }

        if (_samples.Count < 2)
        {
            using var hintBrush = new SolidBrush(Color.FromArgb(120, 155, 164, 178));
            using var hintFont = new Font("Segoe UI", 9F);
            graphics.DrawString("Waiting for CPU temperature samples...", hintFont, hintBrush, left + 12, top + 18);
            return;
        }

        double[] values = _samples.ToArray();
        var points = new PointF[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            float x = left + (right - left) * i / Math.Max(1F, values.Length - 1F);
            float y = MapY(values[i], top, bottom);
            points[i] = new PointF(x, y);
        }

        using var linePen = new Pen(Color.White, 2F)
        {
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round
        };
        graphics.DrawLines(linePen, points);

        using var dotBrush = new SolidBrush(Color.White);
        PointF last = points[^1];
        graphics.FillEllipse(dotBrush, last.X - 3.5F, last.Y - 3.5F, 7F, 7F);

        using var currentBrush = new SolidBrush(Color.FromArgb(210, 230, 235, 244));
        using var currentFont = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        graphics.DrawString($"{values[^1]:0.0}°C", currentFont, currentBrush, Math.Max(left + 8, right - 72), Math.Max(top, last.Y - 24));
    }

    private static float MapY(double temperature, int top, int bottom)
    {
        double clamped = Math.Clamp(temperature, 20, 100);
        return bottom - (float)((clamped - 20) / 80.0 * (bottom - top));
    }
}