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

using DataAccess.Interfaces;
using DataAccess.Services;
using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Interfaces.MQTT;
using Domain.Interfaces.RuleEngine;
using Domain.Interfaces.Timer;
using Domain.Pocos;
using Domain.Services;
using Domain.Services.MQTT;
using Domain.Services.RuleEngine;
using Domain.Services.Timer;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace DeveloperTests;

internal class Program
{
   static async Task Main(string[] args)
   {
      // create service collection
      var services = new ServiceCollection();
      ConfigureServices(services);

      // create service provider
      using var serviceProvider = services.BuildServiceProvider();

      var dataTransferService = serviceProvider.GetService<IDataTransferService>();
      if (null != dataTransferService)
      {
         dataTransferService.MqttBrokerSettings.Ip = "test.mosquitto.org";
      }

      var iotZooMqttClient = serviceProvider.GetService<IIoTZooMqttClient>();
      if (null == iotZooMqttClient)
      {
         throw new Exception("Unable to instantiate IoTZooMqttClient!");
      }
      else
      {
         // This must be done here! There is only one client (singleton).
         await iotZooMqttClient.Connect();
      }

      // entry to run app
      var testCases = serviceProvider.GetService<TestCases>();
   }

   static void ConfigureServices(IServiceCollection services)
   {
      // configure logging
      services.AddLogging(builder =>
      {
         builder.AddConsole();
         builder.AddDebug();
      });
      // build config
      var configuration = new ConfigurationBuilder()
         .SetBasePath(Directory.GetCurrentDirectory())
         .AddJsonFile("appsettings.json", optional: false)
         .AddEnvironmentVariables()
         .Build();

      services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

      // add app
      services.AddSingleton<ISettingsCrudService, SettingsDatabaseService>();

      services.AddSingleton<IDataTransferService, DataTransferService>();
      services.AddSingleton<IRulesCrudService, RulesDatabaseService>();

      services.AddSingleton<IMicrocontrollerService, MicrocontrollerService>();

      services.AddSingleton<IKnownTopicsCrudService, KnownTopicsDatabaseService>();

      services.AddSingleton<IPrepareSourceCodeForGit, PrepareSourceCodeForGitCheckinService>();
      services.AddSingleton<IPrepareTargetPayload, PrepareTargetPayload>();
      services.AddSingleton<ICalculationService, CalculationService>();
      services.AddSingleton<IExpressionEvaluationService, ExpressionEvaluationService>();
      services.AddSingleton<IScriptCrudService, ScriptDatabaseService>();
      services.AddSingleton<IScriptService, CSharpScriptService>();
      services.AddSingleton<IVariablesResolver, VariablesResolver>();
      services.AddSingleton<IScriptsResolver, ScriptsResolver>();
      services.AddSingleton<IExpressionParser, ExpressionParser>();

      // Quartz.net
      services.AddSingleton<Quartz.Spi.IJobFactory, JobFactory>();
      services.AddSingleton<PublishTimeJob>();

      services.AddSingleton<ICronService, CronService>();

      services.AddSingleton<ICronCrudService, CronDatabaseService>();

      services.AddSingleton<IProjectCrudService, ProjectDatabaseService>();
      //services.AddSingleton<ITopicHistoryCrudService, TopicHistoryDatabaseService>();
      services.AddSingleton(new Mock<ITopicHistoryCrudService>().Object);

      //services.AddSingleton<IHueBridgeService, HueBridgeService>();
      services.AddSingleton(new Mock<IHueBridgeService>().Object);

      services.AddSingleton<IIoTZooMqttClient, IotZooMqttClient>();
      //services.AddSingleton(new Mock<IIoTZooMqttClient>().Object);


      services.AddHttpClient("UsersClient", client =>
      {
         //client.BaseAddress = new Uri($"{baseurl}/{userService}");
         client.Timeout = new TimeSpan(0, 0, 30);
         client.DefaultRequestHeaders.Clear();
         // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", applicationKey);
      });
      services.AddTransient<TestCases>();
   }
}