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
using System.Xml;

namespace IotZoo.Components;

internal static class LcdText
{
    public static bool TryGetLines(string? value, int rowCount, int columnCount, out string[][] lines)
    {
        lines = Enumerable.Range(0, rowCount).Select(_ => Array.Empty<string>()).ToArray();
        if (value is null)
        {
            return false;
        }

        try
        {
            XmlConvert.VerifyXmlChars(value);
        }
        catch (XmlException)
        {
            return false;
        }

        string[] inputLines = value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        if (inputLines.Length > rowCount || inputLines.Any(line => line.Any(char.IsControl)))
        {
            return false;
        }

        string[][] characters = inputLines.Select(GetTextElements).ToArray();
        if (characters.Length == 1)
        {
            if (characters[0].Length > rowCount * columnCount)
            {
                return false;
            }
            for (int row = 0; row < rowCount; row++)
            {
                lines[row] = characters[0].Skip(row * columnCount).Take(columnCount).ToArray();
            }
            return true;
        }

        if (characters.Any(line => line.Length > columnCount))
        {
            return false;
        }
        Array.Copy(characters, lines, characters.Length);
        return true;
    }

    private static string[] GetTextElements(string value)
    {
        var elements = StringInfo.GetTextElementEnumerator(value);
        var result = new List<string>();
        while (elements.MoveNext())
        {
            result.Add(elements.GetTextElement());
        }
        return result.ToArray();
    }
}
