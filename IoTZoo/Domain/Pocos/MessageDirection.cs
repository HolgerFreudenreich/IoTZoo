// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

namespace Domain.Pocos;

/// <summary>
/// Direction of the message from the perspective of the IotZooClient.
/// </summary>
public enum MessageDirection
{
   Unknown = -1,
   /// <summary>
   /// Message from an external system like a ESP32.
   /// </summary>
   Inbound = 0,
   /// <summary>
   /// Message subscribed by an external system like a ESP32 or Philips HUE Bridge.
   /// </summary>
   Outbound = 1,
   /// <summary>
   /// Message for internal usage which will not be published.
   /// </summary>
   Internal = 2,
   All = 1000
}
