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

namespace IotZoo.Components;

public partial class Lcd1602Display : LcdDisplayBase
{
    protected override int RowCount => 2;
    protected override int ColumnCount => 16;
    protected override int DisplayWidth => 600;
    protected override int DisplayHeight => 250;

    public static bool IsValidValue(string? value) => LcdText.TryGetLines(value, 2, 16, out _);
}
