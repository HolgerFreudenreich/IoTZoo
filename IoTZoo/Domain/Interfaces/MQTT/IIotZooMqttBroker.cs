// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// Internal MQTT Broker. It is totally independent from the MQTTClient. You can use it if you do not want to setup
// an external MQTT Broker like Mosquitto or use a public MQTT Broker.
// --------------------------------------------------------------------------------------------------------------------

namespace Domain.Interfaces.MQTT
{
   public interface IIotZooMqttBroker
   {
      public Task StartServer();
      public Task StopServer();

   }
}