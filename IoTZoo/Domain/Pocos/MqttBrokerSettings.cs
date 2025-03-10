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

public class MqttBrokerSettings
{
   private string ip = null!;

   public bool UseInternalMqttBroker
   {
      get;
      set;
   }

   /// <summary>
   /// Do not change this property name! It is used in the c++ firmware.
   /// </summary>
   public string Ip { get => ip; set => ip = value.Trim(); }
   /// <summary>
   /// Do not change this property name! It is used in the c++ firmware.
   /// </summary>
   public int Port
   {
      get;
      set;
   } = 1883;

   public string? User
   {
      get;
      set;
   } = null!;

   public string? Password
   {
      get;
      set;
   } = null!;

   public override string ToString()
   {
      return $"{Ip}:{Port}";
   }
}
