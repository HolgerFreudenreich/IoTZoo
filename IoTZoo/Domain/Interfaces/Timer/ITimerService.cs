// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Services.Timer;
using MQTTnet;

namespace Domain.Interfaces.Timer
{
   public interface ITimerService
   {
      MqttApplicationMessage ApplicationMessage { get; set; }
      bool IsRunning { get; }

      event TimerService.Elapsed OnElapsed;


      /// <summary>
      /// 
      /// </summary>
      /// <param name="interval in Milliseconds"></param>
      void SetInterval(double interval);

      void Dispose();
      void Start();
      void Stop();
   }
}