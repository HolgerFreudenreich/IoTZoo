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

namespace IotZoo.Components;

internal static class DisplayDimensions
{
    public static string Style(int? width, int? height, int naturalWidth, int naturalHeight)
    {
        if (width is int explicitWidth && height is int explicitHeight)
        {
            return $"width: {explicitWidth}px; max-width: 100%; height: auto; aspect-ratio: {explicitWidth} / {explicitHeight};";
        }
        if (width is int widthOnly)
        {
            return $"width: {widthOnly}px; max-width: 100%; height: auto;";
        }
        if (height is int heightOnly)
        {
            string scaledWidth = (heightOnly * (decimal)naturalWidth / naturalHeight).ToString(CultureInfo.InvariantCulture);
            return $"width: {scaledWidth}px; max-width: 100%; height: auto;";
        }
        return "max-width: 100%; height: auto;";
    }

    public static void Validate(int? width, int? height)
    {
        if (width is <= 0)
        {
            throw new ArgumentOutOfRangeException("Width", width, "Width must be greater than zero.");
        }
        if (height is <= 0)
        {
            throw new ArgumentOutOfRangeException("Height", height, "Height must be greater than zero.");
        }
    }
}
