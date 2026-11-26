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

using System.Globalization;
using System.Text.RegularExpressions;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;

namespace IotZoo.Components;

public abstract class TM1637DisplayBase : ComponentBase
{
    protected string UnlitColor { get; private set; } = string.Empty;

    protected static readonly string[] SegmentPoints =
    [
        "10,0 38,0 44,4 38,8 10,8 4,4",
        "44,8.5 48,13 48,34 44,38.5 40,34 40,13",
        "44,47.5 48,52 48,73 44,77.5 40,73 40,52",
        "10,78 38,78 44,82 38,86 10,86 4,82",
        "4,47.5 8,52 8,73 4,77.5 0,73 0,52",
        "4,8.5 8,13 8,34 4,38.5 0,34 0,13",
        "10,39 38,39 44,43 38,47 10,47 4,43"
    ];

    private static readonly byte[] DigitSegments =
    [
        0x3f, 0x06, 0x5b, 0x4f, 0x66,
        0x6d, 0x7d, 0x07, 0x7f, 0x6f
    ];

    private string displayText = string.Empty;
    private string accessibleValue = "0";
    protected int decimalPosition = -1;
    protected bool showColon;
    protected bool showSecondsColon;

    protected abstract int DigitCount { get; }
    protected abstract int DisplayWidth { get; }
    protected abstract int BoardWidth { get; }

    protected int VisibleWidth => ShowConnections ? DisplayWidth : BoardWidth;
    protected string ViewBox => ShowConnections ? $"0 0 {DisplayWidth} 231" : $"3 0 {BoardWidth} 231";

    [Parameter]
    public string Value { get; set; } = "0";

    [Parameter]
    public bool LeadingZeros { get; set; }

    [Parameter]
    public bool ShowConnections { get; set; } = false;

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string SegmentColor { get; set; } = GaugeSettings.DefaultSegmentColor;

    [Parameter]
    public int? Height { get; set; }

    [Parameter]
    public int? Width { get; set; }

    protected string AccessibleLabel => $"TM1637 display: {accessibleValue}";

    protected string DisplayStyle => DisplayDimensions.Style(Width, Height, VisibleWidth, 231);

    protected string AspectRatio => Width.HasValue && Height.HasValue ? "none" : "xMidYMid meet";

    private enum ValueFormat { Time, Number, Invalid, TooLong }

    private static ValueFormat GetValueFormat(string value, int digitCount)
    {
        if (Regex.IsMatch(value, @"\A(?:[01][0-9]|2[0-3]):[0-5][0-9]\z") ||
            (digitCount == 6 && Regex.IsMatch(value, @"\A(?:[01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]\z")))
        {
            return ValueFormat.Time;
        }

        if (!Regex.IsMatch(value, @"\A-?[0-9]+([.,][0-9]+)?\z"))
        {
            return ValueFormat.Invalid;
        }

        return value.Length - (value.Contains('.') || value.Contains(',') ? 1 : 0) > digitCount
            ? ValueFormat.TooLong : ValueFormat.Number;
    }

    protected static bool IsValidValue(string? value, int digitCount) =>
        value is not null && GetValueFormat(value, digitCount) is ValueFormat.Time or ValueFormat.Number;

    public static bool IsValidSegmentColor(string? color) =>
        color is not null && Regex.IsMatch(color, @"\A#[0-9a-fA-F]{6}\z");

    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Value);
        if (!IsValidSegmentColor(SegmentColor))
        {
            throw new ArgumentException("SegmentColor must be a color in #RRGGBB format.", nameof(SegmentColor));
        }
        DisplayDimensions.Validate(Width, Height);

        int red = int.Parse(SegmentColor.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 5;
        int green = int.Parse(SegmentColor.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 5;
        int blue = int.Parse(SegmentColor.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 5;
        UnlitColor = $"#{red:x2}{green:x2}{blue:x2}";

        showColon = false;
        showSecondsColon = false;
        decimalPosition = -1;
        ValueFormat format = GetValueFormat(Value, DigitCount);

        if (format == ValueFormat.Time)
        {
            displayText = Value.Replace(":", "").PadLeft(DigitCount);
            showColon = true;
            showSecondsColon = DigitCount == 6 && Value.Length == 8;
        }
        else
        {
            if (format == ValueFormat.Invalid)
            {
                throw new ArgumentException("Value must be a number or a valid 24-hour time.", nameof(Value));
            }
            if (format == ValueFormat.TooLong)
            {
                throw new ArgumentOutOfRangeException(nameof(Value), Value,
                    $"A {DigitCount}-digit display can show at most {DigitCount} characters, including the minus sign.");
            }

            int separator = Value.IndexOfAny(['.', ',']);
            string characters = separator < 0 ? Value : Value.Remove(separator, 1);
            int padding = DigitCount - characters.Length;
            decimalPosition = separator < 0 ? -1 : padding + separator - 1;
            displayText = LeadingZeros
                ? characters.StartsWith('-') ? "-" + characters[1..].PadLeft(DigitCount - 1, '0') : characters.PadLeft(DigitCount, '0')
                : characters.PadLeft(DigitCount);
        }

        accessibleValue = Value;
    }

    protected bool IsSegmentOn(int digit, int segment)
    {
        char character = displayText[digit];
        int mask = character == '-' ? 0x40 : character is >= '0' and <= '9' ? DigitSegments[character - '0'] : 0;
        return (mask & (1 << segment)) != 0;
    }
}
