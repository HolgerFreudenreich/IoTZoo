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

using Domain.Interfaces;
using Domain.Interfaces.Timer;
using Domain.Pocos;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Domain.Services.Timer
{
    public class CountDownFactory : ICountDownFactory
    {
        private readonly Dictionary<string, CountDownService> activeCountDowns = new();

        private readonly object sync = new();

        public ILogger<CountDownService> Logger { get; }

        protected IDataTransferService DataTransferService { get; }

        public CountDownFactory(ILogger<CountDownService> logger, IDataTransferService dataTransferService)
        {
            Logger = logger;
            DataTransferService = dataTransferService;
        }

        public CountDownService Create(CountDownData countDownData)
        {
            string key = $"{countDownData.ProjectName}/{countDownData.CounterName}";

            lock (sync)
            {
                if (activeCountDowns.TryGetValue(key, out CountDownService? active))
                {
                    Logger.LogInformation("Restart countdown {Key} ({Instance}) with {Seconds}s", key, active.GetHashCode(), countDownData.Seconds);
                    active.Restart(countDownData);
                    return active;
                }

                var countDownService = new CountDownService(Logger, DataTransferService, countDownData);
                Logger.LogInformation("New countdown {Key} ({Instance}) with {Seconds}s, active: {Count}", key, countDownService.GetHashCode(), countDownData.Seconds, activeCountDowns.Count + 1);
                countDownService.Completed += service =>
                {
                    lock (sync)
                    {
                        if (activeCountDowns.TryGetValue(key, out CountDownService? current) && current == service)
                        {
                            activeCountDowns.Remove(key);
                        }
                    }
                };
                activeCountDowns[key] = countDownService;
                return countDownService;
            }
        }
    }
}
