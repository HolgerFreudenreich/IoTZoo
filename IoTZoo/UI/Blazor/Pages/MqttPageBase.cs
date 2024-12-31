// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers without programming knowledge.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------

namespace IotZoo.Pages;

using MQTTnet;
using MQTTnet.Client;
using MudBlazor;
using System.Reflection;

public class MqttPageBase : PageBase, IDisposable
{
   protected long MessageCounter { get; set; }

   private IMqttClient mqttClient = null!;

   protected IMqttClient MqttClient
   {
      get => mqttClient;
      set { mqttClient = value; }
   }

   protected override async Task OnAfterRenderAsync(bool firstRender)
   {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender)
      {
         await InitMqttClientAsync();
      }
   }

   protected virtual async Task Client_ConnectedAsync(MqttClientConnectedEventArgs arg)
   {
      Snackbar.Add("MQTT connected!", Severity.Info);
      //await MqttClient.SubscribeAsync("#");
   }

   protected virtual Task Client_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
   {
      Snackbar.Add("MQTT disconnected!", Severity.Error);
      return Task.CompletedTask;
   }

   protected virtual Task Client_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
   {
      MessageCounter++;

      arg.IsHandled = true;
      return Task.CompletedTask;
   }

   private async Task InitMqttClientAsync()
   {
      try
      {
         var factory = new MqttFactory();
         MqttClient = factory.CreateMqttClient();

         mqttClient.ApplicationMessageReceivedAsync += Client_ApplicationMessageReceivedAsync;
         mqttClient.ConnectedAsync += Client_ConnectedAsync;
         mqttClient.DisconnectedAsync += Client_DisconnectedAsync;

         var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(
             DataTransferService.MqttBrokerSettings.Ip,
             DataTransferService.MqttBrokerSettings.Port).Build();

         mqttClient.DisconnectedAsync += async e =>
         {
            await Task.Delay(TimeSpan.FromSeconds(1));
            try
            {
               await mqttClient.ConnectAsync(mqttClientOptions);
               Snackbar.Add("MQTT reconnected");
            }
            catch
            {
               Logger.LogError("Reconnecting failed!");
            }
         };

         await MqttClient.ConnectAsync(mqttClientOptions);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public virtual void Dispose()
   {
      MqttClient?.Dispose();
   }
}