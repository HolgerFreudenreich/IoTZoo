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

public class DataSource : BasePoco
{
    // Topic to subscribe.
    public String Topic { get; set; } = null!;

    // Method to call when a message is received on the topic. The method must be implemented in the microcontroller class.
    public String Method { get; set; } = null!;

    public DataLinkType DataLinkType { get; set; } = DataLinkType.Mqtt;
}
