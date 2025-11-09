// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers without programming knowledge.
// MIT License
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
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
using Domain.Services.ComponentManagement;
using Domain.Services.MQTT;
using Domain.Services.RuleEngine;
using Domain.Services.Timer;
using Infrastructure;
using IotZoo;
using IotZoo.Services;
using Microsoft.OpenApi.Models;
using MQTTnet.Exceptions;
using MudBlazor;
using MudBlazor.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(); // needed for swagger
builder.Services.AddSwaggerGen(c =>
                                        {
                                            c.SwaggerDoc("v1", new OpenApiInfo { Title = "IoTZoo", Version = "v1" });
                                        });

// Add services to the container.

builder.Services.AddMudServices(config =>
                                       {
                                           config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
                                           config.SnackbarConfiguration.PreventDuplicates = false;
                                           config.SnackbarConfiguration.NewestOnTop = false;
                                           config.SnackbarConfiguration.ShowCloseIcon = true;
                                           config.SnackbarConfiguration.VisibleStateDuration = 10000;
                                           config.SnackbarConfiguration.HideTransitionDuration = 500;
                                           config.SnackbarConfiguration.ShowTransitionDuration = 500;
                                           config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
                                       });

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

IConfigurationSection configurationSectionAppSettings = builder.Configuration.GetSection(nameof(AppSettings));
builder.Services.Configure<AppSettings>(configurationSectionAppSettings);

//builder.Services
//       .AddHostedMqttServer(mqttServer => mqttServer.WithDefaultEndpoint())
//       .AddMqttConnectionHandler()
//       .AddConnections();
builder.Services.AddSingleton<IHashingService, Sha1HashingService>();
builder.Services.AddSingleton<IDataTransferService, DataTransferService>();
builder.Services.AddSingleton<ICalculationService, CalculationService>();

builder.Services.AddSingleton<ISettingsCrudService, SettingsDatabaseService>();
builder.Services.AddSingleton<IProjectCrudService, ProjectDatabaseService>();
builder.Services.AddTransient<IMicrocontrollerService, MicrocontrollerService>();
builder.Services.AddSingleton<IKnownTopicsCrudService, KnownTopicsDatabaseService>();
builder.Services.AddSingleton<IScriptCrudService, ScriptDatabaseService>();
builder.Services.AddSingleton<IScriptService, CSharpScriptService>();

builder.Services.AddSingleton<IComponentService, ComponentDatabaseService>();
builder.Services.AddSingleton<IStorageBinCrudService, StorageBinService>();
builder.Services.AddSingleton<IStockingService, StockingDatabaseService>();
builder.Services.AddSingleton<ITopicHistoryCrudService, TopicHistoryDatabaseService>();
builder.Services.AddSingleton<ICronCrudService, CronDatabaseService>();
builder.Services.AddSingleton<INamespaceCrudService, NamespaceService>();
builder.Services.AddSingleton<ICronService, CronService>();

builder.Services.AddTransient<ISearchHelper, SearchHelper>();

//builder.Services.AddSingleton<IRestApi, KnownTopicsRestServiceCaller>();
builder.Services.AddSingleton<IHueBridgeService, HueBridgeService>();

// Quartz.net
builder.Services.AddSingleton<Quartz.Spi.IJobFactory, JobFactory>();
builder.Services.AddSingleton<PublishTimeJob>();
builder.Services.AddSingleton<CalculateNextSunriseAndSunsetJob>();

builder.Services.AddSingleton<IExpressionEvaluationService, ExpressionEvaluationService>();

builder.Services.AddSingleton<IVariablesResolver, VariablesResolver>();
builder.Services.AddSingleton<IScriptsResolver, ScriptsResolver>();
builder.Services.AddSingleton<IExpressionParser, ExpressionParser>();

builder.Services.AddSingleton<IIotZooMqttBroker, IotZooMqttBroker>();
builder.Services.AddSingleton<IIoTZooMqttClient, IotZooMqttClient>();
builder.Services.AddSingleton<IKnownMicrocontrollerCrudService, MicrocontrollerService>();

builder.Services.AddSingleton<IPrepareTargetPayload, PrepareTargetPayload>();
builder.Services.AddSingleton<IRulesCrudService, RulesDatabaseService>(); // set ConfigurationDatabaseType in appsetings.json to Postgres or Sqlite!

builder.Services.AddSingleton<IMailReceiverFactory, MailReceiverFactory>();

builder.Services.AddSingleton<ICountDownFactory, CountDownFactory>();


builder.Host.UseSerilog((ctx, lc) =>
                     {
                         lc.ReadFrom.Configuration(ctx.Configuration);
                     });

var app = builder.Build();

// Instantiate immediately (without calling the webpage)

var iotZooMqttBroker = app.Services.GetService<IIotZooMqttBroker>();
if (null != iotZooMqttBroker)
{
    await iotZooMqttBroker.StartServer();
}
try
{
    var iotZooMqttClient = app.Services.GetService<IIoTZooMqttClient>();
    if (null == iotZooMqttClient)
    {
        throw new Exception("Unable to instantiate IoTZooMqttClient!");
    }
    else
    {
        // This must be done here! There is only one client (singleton).
        await iotZooMqttClient.Connect();
    }
}
catch (MqttCommunicationTimedOutException exception)
{
    // Maybe you do not have internet access or a wrong configured MQTTClient.
    Console.WriteLine(exception.GetBaseException().Message);

}
catch (Exception exception)
{
    Console.WriteLine(exception.GetBaseException().Message);
}

var conJobService = app.Services.GetService<ICronService>();

var mailReceiverFactory = app.Services.GetService<IMailReceiverFactory>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSwagger();

//app.UseSwaggerUI();
app.UseSwaggerUI(c =>
                     {
                         c.SwaggerEndpoint("/swagger/v1/swagger.json", "IoTZoo");
                     });

app.UseHttpsRedirection();





//app.UseStaticFiles();

//app.UseRouting();

//app.MapBlazorHub();

//app.MapControllers();
//app.MapFallbackToPage("/_Host");
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();