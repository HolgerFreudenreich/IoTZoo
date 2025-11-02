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

public class TimerService : ITimerService, IDisposable
{
    public delegate void Elapsed(Timer timer, TimerServiceEventArgs elapsedEventArgs);
    public event Elapsed? OnElapsed;

    private readonly Timer timer;
    private readonly object syncRoot = new();
    private bool disposed;

    public bool IsRunning => timer.Enabled;

    public MqttApplicationMessage ApplicationMessage { get; set; } = null!;

    public TimerService(double interval, bool start = true)
    {
        timer = new Timer(interval);
        timer.Elapsed += NotifyTimer;
        timer.AutoReset = true;
        timer.Enabled = start;
    }

    public void Start()
    {
        if (disposed) return;
        lock (syncRoot)
        {
            timer.Start();
        }
    }

    public void Stop()
    {
        if (disposed) return;
        lock (syncRoot)
        {
            timer.Stop();
        }
    }

    public void SetInterval(double interval)
    {
        if (disposed) return;
        lock (syncRoot)
        {
            timer.Interval = interval;
        }
    }

    public void Dispose()
    {
        lock (syncRoot)
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            timer.Elapsed -= NotifyTimer;
            timer.Stop();
            timer.Dispose();
        }
    }

    private void NotifyTimer(object? sender, ElapsedEventArgs e)
    {
        lock (syncRoot)
        {
            if (disposed) return;
            TimerServiceEventArgs args = new() { ApplicationMessage = ApplicationMessage };
            OnElapsed?.Invoke(timer, args);
        }
    }
}
