using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Domain.Services
{
    public interface IMailReceiverFactory
    {
        MailReceiverService Create(MailReceiverConfig config, IDataTransferService dataTransferService);
    }

    public class MailReceiverFactory : IMailReceiverFactory
    {
        protected ILogger<MailReceiverService> Logger { get; }


        public MailReceiverFactory(ILogger<MailReceiverService> logger, ISettingsCrudService settingsCrudService, IDataTransferService dataTransferService)
        {
            Logger = logger;
            var jsonMailSettings = settingsCrudService.GetSettingString(SettingCategory.Mail, SettingKey.MailReceiverConfigs).Result;
            if (!string.IsNullOrEmpty(jsonMailSettings))
            {
                var configs = JsonSerializer.Deserialize<List<MailReceiverConfig>>(jsonMailSettings);
                if (null != configs)
                {
                    foreach (var config in configs)
                    {
                        if (config.Enabled)
                        {
                            Create(config, dataTransferService);
                        }
                    }
                }
            }
        }

        public MailReceiverService Create(MailReceiverConfig config, IDataTransferService dataTransferService)
        {
            return new MailReceiverService(Logger, config, dataTransferService);
        }
    }
}