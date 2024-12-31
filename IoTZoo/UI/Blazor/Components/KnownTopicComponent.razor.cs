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

using Domain.Pocos;
using Microsoft.AspNetCore.Components;

namespace IotZoo.Components;

public class KnownTopicComponentBase : ComponentBase
{
   private string topic = string.Empty;

   [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
   public string Topic
#pragma warning restore BL0007 // Component parameters should be auto properties
   {
      get => topic;
      set
      {
         if (!string.IsNullOrEmpty(value) && value.StartsWith("/"))
         {
            topic = $"<Namespace>/<Project>/<BoardType>/<MacAddress>{value}";
         }
         else
         {
            topic = value;
         }
      }
   }

   [Parameter]
   public string Description { get; set; } = string.Empty;

   [Parameter]
   public string ExamplePayload { get; set; } = string.Empty;

   [Parameter]
   public MessageDirection MessageDirection { get; set; }
}
