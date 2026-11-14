// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using DataAccess.Services;
using Domain.Interfaces;
using Domain.Interfaces.Timer;
using Domain.Pocos;
using Domain.Services.MQTT;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Domain.Services.Timer
{
    public class CountDownService : MqttPublisher, IDisposable
    {
        protected CountDownData CountDownData { get; }

        protected ITimerService TimerService { get; }

        private readonly object stateLock = new();

        public event System.Action<CountDownService>? Completed;

        public CountDownService(ILogger<CountDownService> logger, IDataTransferService dataTransferService,
                                CountDownData countDownData)
            : base(logger, dataTransferService)
        {
            CountDownData = countDownData;
            if (countDownData.ReportProgress)
            {
                TimerService = new TimerService(1000);
            }
            else
            {
                TimerService = new TimerService(countDownData.Seconds * 1000);
            }
            TimerService.OnElapsed += TimerService_OnElapsed;
        }

        public void Restart(CountDownData countDownData)
        {
            lock (stateLock)
            {
                CountDownData.Seconds = countDownData.Seconds;
                CountDownData.ReportProgress = countDownData.ReportProgress;
                TimerService.SetInterval(countDownData.ReportProgress ? 1000 : countDownData.Seconds * 1000);
                TimerService.Start();
            }
        }

        private async void TimerService_OnElapsed(System.Timers.Timer timer, TimerServiceEventArgs elapsedEventArgs)
        {
            string payload;
            bool publish;
            bool publishFinished;
            bool finished;
            lock (stateLock)
            {
                finished = !CountDownData.ReportProgress;
                publish = !finished || CountDownData.Seconds >= 0;
                if (CountDownData.ReportProgress)
                {
                    CountDownData.Seconds--;
                    finished = CountDownData.Seconds < 0;
                    publish = !finished;
                }
                else
                {
                    CountDownData.Seconds = 0;
                }
                publishFinished = CountDownData.Seconds == 0;
                payload = JsonSerializer.Serialize(CountDownData);
            }

            if (publish)
            {
                await MqttClient.PublishAsync($"{DataTransferService.NamespaceName}/{CountDownData.ProjectName}/{TopicConstants.COUNTDOWN_STATUS}", payload);
            }

            if (publishFinished)
            {
                await MqttClient.PublishAsync($"{DataTransferService.NamespaceName}/{CountDownData.ProjectName}/{TopicConstants.COUNTDOWN_FINISHED}", CountDownData.CounterName);
            }

            if (finished)
            {
                Dispose();
            }
        }

        new void Dispose()
        {
            base.Dispose();
            TimerService?.Dispose();
            Completed?.Invoke(this);
        }
    }
}