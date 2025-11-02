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

        private async void TimerService_OnElapsed(System.Timers.Timer timer, TimerServiceEventArgs elapsedEventArgs)
        {
            if (CountDownData.ReportProgress)
            {
                CountDownData.Seconds--;

                if (CountDownData.Seconds < 0)
                {
                    Dispose();
                }
                else
                {
                    await MqttClient.PublishAsync($"{DataTransferService.NamespaceName}/{CountDownData.ProjectName}/count_down", JsonSerializer.Serialize(CountDownData));
                }
            }
            else
            {
                CountDownData.Seconds = 0;
                await MqttClient.PublishAsync($"{DataTransferService.NamespaceName}/{CountDownData.ProjectName}/count_down", JsonSerializer.Serialize(CountDownData));
                Dispose();
            }
        }

        new void Dispose()
        {
            base.Dispose();
            TimerService?.Dispose();
        }
    }
}