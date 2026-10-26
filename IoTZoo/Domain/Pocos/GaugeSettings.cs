namespace Domain.Pocos;

public class GaugeSettings
{
    public double Min { get; set; } = 0;

    public double Max { get; set; } = 100;

    public string Label { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public int Height { get; set; } = 240;

    public GaugeType GaugeType { get; set; } = GaugeType.Arc;

    public GaugeOrientation Orientation { get; set; } = GaugeOrientation.Horizontal;

    public double StartAngle { get; set; } = -135;

    public double EndAngle { get; set; } = 135;

    public bool ShowTickLabels { get; set; } = true;

    public string ArcColor { get; set; } = "#4caf50";

    public List<GaugeRange> Ranges { get; set; } = new();
}

public class GaugeRange
{
    public double From { get; set; }

    public double To { get; set; }

    public string Color { get; set; } = "#4caf50";
}
