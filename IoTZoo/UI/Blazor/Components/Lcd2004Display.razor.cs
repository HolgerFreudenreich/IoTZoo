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

public partial class Lcd2004Display : LcdDisplayBase
{
    protected override int RowCount => 4;
    protected override int ColumnCount => 20;
    protected override int DisplayWidth => 704;
    protected override int DisplayHeight => 372;

    public static bool IsValidValue(string? value) => LcdText.TryGetLines(value, 4, 20, out _);
}
