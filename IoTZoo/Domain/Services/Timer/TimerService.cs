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

using MQTTnet;

namespace Domain.Services.Timer;

using Domain.Interfaces.Timer;
using System.Timers;

public class TimerServiceEventArgs : EventArgs
{
   public MqttApplicationMessage? ApplicationMessage
   {
      get;
      set;
   }
}

public class TimerService : ITimerService
{
   public delegate void Elapsed(Timer timer, TimerServiceEventArgs elapsedEventArgs);
   public event Elapsed OnElapsed = null!;

   private Timer timer;

   public bool IsRunning => timer.Enabled;

   public MqttApplicationMessage ApplicationMessage
   {
      get;
      set;
   } = null!;

   public TimerService(double interval, bool start = true)
   {
      timer = new Timer();
      timer.Elapsed -= NotifyTimer;
      timer.Elapsed += NotifyTimer;
      SetInterval(interval);
      timer.Enabled = start;
   }

   public void Start()
   {
      timer.Start();
   }

   public void Stop()
   {
      timer.Stop();
   }

   public void Dispose()
   {
      timer.Stop();
      timer.Dispose();
   }

   /// <summary>
   /// 
   /// </summary>
   /// <param name="interval in Milliseconds"></param>
   public void SetInterval(double interval)
   {
      timer.Interval = interval;
   }

   private void NotifyTimer(object? sender, ElapsedEventArgs e)
   {
      TimerServiceEventArgs timerServiceEventArgs = new TimerServiceEventArgs { ApplicationMessage = ApplicationMessage };
      OnElapsed?.Invoke(timer, timerServiceEventArgs);
   }
}