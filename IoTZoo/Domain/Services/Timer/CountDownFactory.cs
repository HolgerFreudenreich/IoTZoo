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