// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// Provides an interface to configure things/devices on the microcontroller (mapping to the correct gpio pins, etc). 
// --------------------------------------------------------------------------------------------------------------------

using Domain.Interfaces.Crud;
using Domain.Pocos;
using MQTTnet;

namespace Domain.Interfaces;

/// <summary>
/// </summary>
public interface IMicrocontrollerService : IKnownMicrocontrollerCrudService
{
   event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync;
   public event Func<AliveMessage, Task> AliveMessageAsync;
   public event Action<List<ConnectedDevice>> OnReceivedDeviceConfig;

   public void AddConnectedDevice(KnownMicrocontroller microcontroller, ConnectedDevice connectedDevice);

   public string GetBaseTopic(KnownMicrocontroller microcontroller);

   public List<ConnectedDevice> ConnectedDevicesList
   {
      get;
      set;
   }

   #region REST
   public Task<List<ConnectedDevice>> GetDeviceConfig(KnownMicrocontroller microcontroller);

   public Task<bool> IsMicrocontrollerOnline(KnownMicrocontroller microcontroller);

   /// <summary>
   /// REST
   /// </summary>
   /// <returns></returns>
   public Task<HttpResponseMessage?> PostDeviceConfigToMicrocontroller(KnownMicrocontroller microcontroller);

   public Task<bool> PostMicrocontrollerConfigToMicrocontroller(KnownMicrocontroller microcontroller);

   #endregion REST

   #region MQTT
   /// <summary>
   /// MQTT
   /// </summary>
   /// <returns></returns>
   public Task<bool> PushDeviceConfigToMicrocontroller(KnownMicrocontroller microcontroller);

   public Task<bool> PushMicrocontrollerConfigToMicrocontroller(KnownMicrocontroller microcontroller);

   public Task RequestDeviceConfiguration(KnownMicrocontroller microcontroller);

   public Task RequestAliveMessageAsync(KnownMicrocontroller microcontroller);

   public Task Reboot(KnownMicrocontroller microcontroller);
   #endregion
}
