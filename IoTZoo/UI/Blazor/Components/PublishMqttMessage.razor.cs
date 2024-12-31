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

using Domain.Interfaces;
using Domain.Interfaces.MQTT;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

namespace IotZoo.Components;

public partial class PublishMqttMessage : ComponentBase
{

   [Parameter]
   public TopicEntry TopicEntry
   {
      get;
      set;
   }

   [Inject]
   protected ILogger<PublishMqttMessage> Logger { get; set; } = null!;

   [Inject]
   protected IIoTZooMqttClient MqttClient
   {
      get;
      set;
   } = null!;

   [Inject]
   protected ISnackbar Snackbar
   {
      get;
      set;
   } = null!;

   [Inject]
   IDataTransferService DataTransferService { get; set; } = null!;

   public PublishMqttMessage()
   {
      TopicEntry = new TopicEntry();
   }

   public async Task PublishAsync()
   {
      try
      {
         bool success = await MqttClient.PublishTopic(TopicEntry.Topic,
                                                      TopicEntry.Payload,
                                                      MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce,
                                                      TopicEntry.Retain);

         if (!success)
         {
            Snackbar.Add("Not successful!", Severity.Error);
         }
         else
         {
            Snackbar.Add("Successfully sent.", Severity.Success);
         }
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }
}