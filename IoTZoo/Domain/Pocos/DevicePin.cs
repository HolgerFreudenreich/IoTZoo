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

public class DevicePin
{
   /// <summary>
   /// Name of the Pin like CLK
   /// </summary>
   public string? PinName { get; set; }

   /// <summary>
   /// GPIO Pin where the DevicePin is connected to.
   /// </summary>
   public string? MicrocontrollerGpoPin { get; set; }

   public bool IsReadOnly { get; set; } = false;

}