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

using System.Text.Json.Serialization;

namespace Domain.Pocos;

public class KnownMicrocontroller : BasePoco
{
   public int MicroControllerId
   {
      get; set;
   }

   /// <summary>
   /// Every microcontroller has a unique MacAddress from factory.
   /// </summary>
   public string MacAddress
   {
      get;
      set;
   } = string.Empty;

   public string? IpAddress
   {
      get;
      set;
   }

   public string? IpMqttBroker
   {
      get;
      set;
   }

   public string NamespaceName
   {
      get; set;
   } = string.Empty;

   public string ProjectName
   {
      get; set;
   } = string.Empty;

   public string BoardType
   {
      get; set;
   } = string.Empty;

   public string? FirmwareVersion
   {
      get;set;
   }

   public string? Description
   {
      get;
      set;
   }

   [JsonIgnore]
   public bool? Online
   {
      get;
      set;
   }

   [JsonIgnore]
   public DateTime? BootDateTime
   {
      get;
      set;
   }

   public List<KnownTopic>? KnownTopics { get; set; } = null;
}