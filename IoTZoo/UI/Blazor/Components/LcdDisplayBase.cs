// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace IotZoo.Components;

public abstract class LcdDisplayBase : ComponentBase
{
    protected static readonly string[] PinLabels = ["GND", "VCC", "SDA", "SCL"];
    protected string[][] Lines { get; private set; } = [];

    protected abstract int RowCount { get; }
    protected abstract int ColumnCount { get; }
    protected abstract int DisplayWidth { get; }
    protected abstract int DisplayHeight { get; }
    protected int BoardWidth => DisplayWidth - 70;

    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public int? Height { get; set; }

    [Parameter]
    public int? Width { get; set; }

    [Parameter]
    public bool ShowConnections { get; set; } = true;

    [Parameter]
    public string? Label { get; set; }

    protected int VisibleWidth => ShowConnections ? DisplayWidth : BoardWidth;
    protected string ViewBox => ShowConnections ? $"0 0 {DisplayWidth} {DisplayHeight}" : $"1.5 0 {BoardWidth} {DisplayHeight}";
    protected string DisplayStyle => DisplayDimensions.Style(Width, Height, VisibleWidth, DisplayHeight);
    protected string AspectRatio => Width.HasValue && Height.HasValue ? "none" : "xMidYMid meet";

    protected override void OnParametersSet()
    {
        if (!LcdText.TryGetLines(Value, RowCount, ColumnCount, out var lines))
        {
            throw new ArgumentException($"Value must fit {RowCount} lines of {ColumnCount} printable characters.", nameof(Value));
        }
        DisplayDimensions.Validate(Width, Height);
        Lines = lines;
    }
}
