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

namespace IotZoo.Pages;

using MQTTnet;
using MudBlazor;
using System.Reflection;

public class MqttPageBase : PageBase, IDisposable
{
   protected long MessageCounter { get; set; }

   protected IMqttClient MqttClient { get; set; } = null!;

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
         var factory = new MqttClientFactory();
         MqttClient = factory.CreateMqttClient();

         MqttClient.ApplicationMessageReceivedAsync += Client_ApplicationMessageReceivedAsync;
         MqttClient.ConnectedAsync += Client_ConnectedAsync;
         MqttClient.DisconnectedAsync += Client_DisconnectedAsync;

         var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(DataTransferService.MqttBrokerSettings.Ip,
                                                                              DataTransferService.MqttBrokerSettings.Port).Build();

         MqttClient.DisconnectedAsync += async e =>
         {
            await Task.Delay(TimeSpan.FromSeconds(1));
            try
            {
               await MqttClient.ConnectAsync(mqttClientOptions);
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