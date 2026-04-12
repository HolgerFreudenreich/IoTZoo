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

namespace Domain.Pocos;

public class TopicLink : BasePoco
{
    /// <summary>
    /// Example: iotzoo/picea/esp32/E4:65:B8:B0:45:B4/reed_contact/0/rpm
    /// </summary>
    public string TriggeringTopic { get; set; } = string.Empty;

    /// <summary>
    /// Condition to fire. For example, input (value of TriggeringTopic) > 130
    /// Example 1: { "Operator": "contains", "Value": "rang"}
    /// Example 2: { "Operator": ">", "Value": "130"}
    /// </summary>
    public string? Expression { get; set; } = null;

    /// <summary>
    /// Example 1: iotzoo/picea/esp32/24:DC:C3:A8:47:24/tm1637_4/0/number
    /// Example 2: iotzoo/picea/esp32/E4:65:B8:B0:45:B4/buzzer/0/beep
    /// </summary>
    public string TargetTopic { get; set; } = string.Empty;

    /// <summary>
    /// if "input" or null/empty, the payload send to TriggeringTopic is the value of the device for
    /// example1: 138.
    /// example2: [{'FrequencyHz': 1000, 'DurationMs': 100}, {'FrequencyHz': 0, 'DurationMs': 100}, {'FrequencyHz': 2000, 'DurationMs': 100}]
    /// </summary>
    public string? TargetPayload { get; set; } = null;
}
