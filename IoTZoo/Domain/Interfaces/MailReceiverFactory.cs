using Domain.Interfaces.Crud;
using Domain.Pocos;
using Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Domain.Interfaces
{

    public class MailReceiverFactory : IMailReceiverFactory
    {
        protected ILogger<MailReceiverService> Logger { get; }

        public MailReceiverFactory(ILogger<MailReceiverService> logger,
            ISettingsCrudService settingsCrudService,
            IDataTransferService dataTransferService,
            IProjectCrudService projectCrudService)
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
                            Create(config, dataTransferService, projectCrudService);
                        }
                    }
                }
            }
        }

        public MailReceiverService Create(MailReceiverConfig config, IDataTransferService dataTransferService, IProjectCrudService projectCrudService)
        {
            return new MailReceiverService(Logger, config, dataTransferService, projectCrudService);
        }
    }
}