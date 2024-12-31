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
/// One MQTT-Topic
/// </summary>
public class TopicEntry : KnownTopic
{
   private string? previousPayload;

   public string? Payload { get; set; } = null!;

   public DateTime DateOfReceipt
   {
      get;
      set;
   } = DateTime.UtcNow;

   public int QualityOfServiceLevel
   {
      get;
      set;
   }

   public bool Retain
   {
      get;
      set;
   }

   public string? PreviousPayload
   {
      get => previousPayload;
      set
      {
         previousPayload = value;
         //TimeDiff = DateTime.UtcNow - DateOfReceipt;
      }
   }

   public TimeSpan TimeDiff
   {
      get; set;
   }

   public bool IsKnown
   {
      get;
      set;
   }

   public List<Rule>? Rules
   {
      get;
      set;
   }

   public bool ShowRules
   {
      get;
      set;
   }
}