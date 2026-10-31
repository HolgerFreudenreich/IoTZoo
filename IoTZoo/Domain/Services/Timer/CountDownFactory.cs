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

namespace Domain.Services.Timer
{
    public class CountDownFactory : ICountDownFactory
    {
        public ILogger<CountDownService> Logger { get; } = null!;

        protected IDataTransferService DataTransferService { get; }


        public CountDownFactory(ILogger<CountDownService> logger, IDataTransferService dataTransferService)
        {
            Logger = logger;
            DataTransferService = dataTransferService;
        }

        public CountDownService Create(CountDownData countDownData)
        {
            return new CountDownService(Logger, DataTransferService, countDownData);
        }
    }
}