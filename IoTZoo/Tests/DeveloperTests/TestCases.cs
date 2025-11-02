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

using Domain.Pocos;

namespace DeveloperTests;

using DataAccess.Interfaces;
using DataAccess.Services;
using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Interfaces.MQTT;
using Domain.Interfaces.RuleEngine;
using Domain.Pocos;
using Domain.Services.RuleEngine;
using Microsoft.Extensions.Options;
using System;
using System.Data;
using System.Globalization;
using System.Text.Json;
using Rule = Rule;

public partial class TestCases
{
   protected IPrepareTargetPayload PrepareTargetPayload
   {
      get;
   }

   protected IExpressionEvaluationService ExpressionEvaluationService
   {
      get;
   }

   protected IIoTZooMqttClient IoTZooMqttClient { get; }

   protected IHueBridgeService HueBridgeService { get; }

   protected IDataTransferService DataTransferService { get; }
   protected IExpressionParser ExpressionParser { get; }

   protected IScriptsResolver ScriptsResolver { get; }

   protected IRulesCrudService RulesService { get; }

   protected IKnownTopicsCrudService KnownTopicsService {  get; }

   public TestCases(IOptions<AppSettings> options,
                    IPrepareSourceCodeForGit prepareSourceCodeForGitService,
                    IPrepareTargetPayload prepareTargetPayload,
                    IExpressionEvaluationService expressionEvaluationService,
                    IDataTransferService dataTransferService,
                    IScriptCrudService scriptService,
                    IExpressionParser expressionParser,
                    IVariablesResolver variablesResolver,
                    IScriptsResolver scriptsResolver,
                    IHueBridgeService hueBridgeService,
                    IIoTZooMqttClient ioTZooMqttClient,
                    IRulesCrudService rulesService,
                    IKnownTopicsCrudService knownTopicsService)
   {
      AppSettings = options.Value;
      this.DataTransferService = dataTransferService;
      this.ScriptsResolver = scriptsResolver;
      this.PrepareTargetPayload = prepareTargetPayload;
      this.ExpressionEvaluationService = expressionEvaluationService;
      this.ExpressionParser = expressionParser;
      this.IoTZooMqttClient = ioTZooMqttClient;
      this.HueBridgeService = hueBridgeService;
      this.RulesService = rulesService;
      this.KnownTopicsService = knownTopicsService;

      prepareSourceCodeForGitService.Execute();

      // TestRegisterMicrocontroller(microcontrollerDatabaseService);
      // TestRegisterKnownTopic(knownTopicsService);
      TestCalc();

      TestExpressionEvaluationAsync();

      _ = TestPrepareTargetPayload();
      _ = TestLoadscriptAsync(scriptService);
      _ = TestScriptExecutionAsync();
      _ = TestExpressionParser1Async();
      _ = TestScriptExecution2Async();
      _ = TestExpressionParser2Async();
      _ = TestHandleMqttMessage();
   }

   private void TestCalc()
   {
      double result1 = Calc($"({FunctionNames.Input} * 5) + 3", 1.3);
      if (result1 != 9.5)
      {
         throw new Exception();
      }
   }

   private async Task TestScriptExecutionAsync()
   {
      Rule rule = new Rule();

      rule.SourceTopic = "TEMPERATURE_CHANGED";
      rule.TargetTopic = "COLOR";
      rule.TargetPayload = $"{FunctionNames.Script}(TemperatureToColor({FunctionNames.Input}));";

      var topicEntry = new TopicEntry();
      topicEntry.Topic = "TEMPERATURE_CHANGED";
      topicEntry.Payload = "33.4";
      var targetPayload0 = await PrepareTargetPayload!.Execute(rule, topicEntry);
      var targetPayload1 = await PrepareTargetPayload!.Execute(rule.TargetPayload, topicEntry.Payload);
      if (targetPayload0 != targetPayload1)
      {
         throw new Exception();
      }
   }

   private async Task TestScriptExecution2Async()
   {
      this.DataTransferService!.ReceivedTopicsQueue.Clear();
      this.DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry { Topic = "Temperature", Payload = "12.2" });

      Rule rule = new Rule();

      rule.SourceTopic = "CHANGED";
      rule.TargetTopic = "COLOR";
      rule.TargetPayload = $"{FunctionNames.Script}(TemperatureToColor({FunctionNames.ReadFromMemory}(\"Temperature\")));";

      var topicEntry = new TopicEntry();
      topicEntry.Topic = "CHANGED";
      topicEntry.Payload = string.Empty;
      var targetPayload0 = await PrepareTargetPayload!.Execute(rule, topicEntry);
   }

   private async Task TestExpressionParser1Async()
   {
      this.DataTransferService!.ReceivedTopicsQueue.Clear();
      this.DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry { Topic = "Temperature", Payload = "12.2" });

      Rule rule = new Rule();

      rule.SourceTopic = "CHANGED";
      rule.TargetTopic = "COLOR";
      rule.TargetPayload = $"{FunctionNames.Script}TemperatureToColor({FunctionNames.ReadFromMemory}(\"Temperature\"));";

      var topicEntry = new TopicEntry();
      topicEntry.Topic = "CHANGED";
      topicEntry.Payload = string.Empty;
      var targetPayload0 = await PrepareTargetPayload!.Execute(rule, topicEntry);

      var result = await ExpressionParser.ResolveExpression(rule.TargetPayload);
   }

   private async Task TestPrepareTargetPayload()
   {
      // Scenario 1: print temperature value on an LCD display.
      var rule = new Rule();

      rule.SourceTopic = "esp32/123/temperature/0";
      rule.TargetTopic = "esp32/129/lcd160x/0";
      rule.TargetPayload = "{'text': \"Boiler: " + FunctionNames.Input + " °C \", \"clear\": false, \"x\":0, \"y\": 1}";

      var topicEntry = new TopicEntry();
      topicEntry.Topic = "esp32/1/temperature/0";
      topicEntry.Payload = "59.8";

      string? targetPayload = await PrepareTargetPayload.Execute(rule, topicEntry);
      if (null == targetPayload)
      {
         throw new Exception();
      }
      targetPayload = System.Text.RegularExpressions.Regex.Unescape(targetPayload);
      if (targetPayload != """{'text': "Boiler: 59.8 °C ", "clear": false, "x":0, "y": 1}""")
      {
         throw new Exception();
      }

      // Scenario 2: Incement a value

      rule = new Rule();

      rule.SourceTopic = "esp32/123/counter/0";
      rule.TargetTopic = "IncResult";
      rule.TargetPayload = $"{FunctionNames.Calc}({FunctionNames.Input} + 1);";

      topicEntry = new TopicEntry();
      topicEntry.Topic = "esp32/123/counter/0";
      topicEntry.Payload = "159";
      targetPayload = await PrepareTargetPayload.Execute(rule, topicEntry); // 160
      if (targetPayload != "160")
      {
         throw new Exception();
      }

      // Scenario 3: Compare a value
      rule = new Rule();

      rule.SourceTopic = "esp32/123/state/0";
      rule.TargetTopic = "CompareResult";
      rule.TargetPayload = $"Calc({FunctionNames.Input} == 100);";

      topicEntry = new TopicEntry();
      topicEntry.Topic = "esp32/123/state/0";
      topicEntry.Payload = "100";
      targetPayload = await PrepareTargetPayload.Execute(rule, topicEntry); // 1
      if (targetPayload != "1")
      {
         throw new Exception();
      }

      // Scenario 4: Invert a value
      // a) 0
      rule = new Rule();

      rule.SourceTopic = "esp32/123/state/0";
      rule.TargetTopic = "Inverted";
      rule.TargetPayload = $"Calc(not {FunctionNames.Input});";

      topicEntry = new TopicEntry();
      topicEntry.Topic = "esp32/123/state/0";
      topicEntry.Payload = "0";
      targetPayload = await PrepareTargetPayload.Execute(rule, topicEntry); // 1
      if (targetPayload != "1")
      {
         throw new Exception();
      }
      // b) <> 0
      topicEntry.Payload = "true";
      targetPayload = await PrepareTargetPayload.Execute(rule, topicEntry);

      if (targetPayload != "0")
      {
         throw new Exception();
      }

      topicEntry.Payload = "543543.56345643";
      targetPayload = await PrepareTargetPayload.Execute(rule, topicEntry);

      if (targetPayload != "0")
      {
         throw new Exception();
      }

      topicEntry.Payload = "true";
      targetPayload = await PrepareTargetPayload.Execute(rule, topicEntry);

      if (targetPayload != "0")
      {
         throw new Exception();
      }

      topicEntry.Payload = "'Holger' == 'Holger'";
      targetPayload = await PrepareTargetPayload.Execute(rule, topicEntry);

      if (targetPayload != "0")
      {
         throw new Exception();
      }

      this.DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry { Topic = "Button1State", Payload = "1" });

      rule = new Rule();

      rule.SourceTopic = "Topic/A/B/C";
      string sourcePayload = "4"; // input

      rule.Expression = $"{FunctionNames.Input} >= 0 && ReadFromMemory(Button1State) == 1";

      var ok = this.ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, sourcePayload);
      // -----------------

      this.DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry { Topic = "esp32/E4:65:B8:77:55:00/button_matrix/0/toggle_button/7", Payload = "HOLD" });
      this.DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry { Topic = "esp32/E4:65:B8:77:55:00/button_matrix/0/toggle_button/4", Payload = "HOLD" });

      rule = new Rule();

      rule.SourceTopic = "esp32/E4:65:B8:77:55:00/button_matrix/0/toggle_button/7";
      sourcePayload = "HOLD"; // input

      rule.Expression = "\"ReadFromMemory(esp32/E4:65:B8:77:55:00/button_matrix/0/toggle_button/4)\" == \"HOLD\"";

      rule.Expression += $" && \"{FunctionNames.Input}" == "\"HOLD\"";

      var ok2 = this.ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, sourcePayload);
   }

   private double Calc(string formula, double input)
   {
      using DataTable dataTable = new DataTable();
      string expression = formula.Replace(FunctionNames.Input, input.ToString(CultureInfo.InvariantCulture));
      object result = dataTable.Compute(expression, null);
      return Convert.ToDouble(result);
   }

   private void TestExpressionEvaluationAsync()
   {
      try
      {
         string payload = "{\"creationtime\":\"2024-02-01T16:26:41+00:00\",\"data\":[{\"id\":\"5c59ff83-69b8-47ad-997c-6ebc2f8adea1\",\"id_v1\":\"/sensors/8\",\"type\":\"button\",\"metadata\":null,\"creation_time\":null,\"owner\":{\"rid\":\"452a82fd-eefa-4605-b61c-aff624d38d99\",\"rtype\":\"device\"},\"services\":null,\"button\":{\"button_report\":{\"event\":\"initial_press\",\"updated\":\"2024-02-01T16:26:42.141Z\"},\"last_event\":\"initial_press\"}}],\"id\":\"3a301501-07d2-4e5a-b9c5-6778c45e5d3c\",\"id_v1\":null,\"type\":\"update\",\"metadata\":null,\"creation_time\":null,\"owner\":null,\"services\":null}";
         string operation = "{   \"Operation\": \"Contains\",   \"Value\": \"initial_press\" }";

         var expressionPoco = JsonSerializer.Deserialize<ExpressionPoco>(operation);

         if (expressionPoco?.Operator == "Contains")
         {
            if (!string.IsNullOrEmpty(expressionPoco.Value))
            {
               var test = payload.Contains(expressionPoco.Value);
            }
         }

         var ok = payload.Contains("initial_press");

         // another way
         //var result = await CSharpScript.EvaluateAsync($"string payload = \"initial_press\"; payload.Contains(\"initial_press\");");
      }
      catch (Exception)
      {
      }
   }

   private int TestRegisterMicrocontroller(IMicrocontrollerService microcontrollerService)
   {
      KnownMicrocontroller microcontroller = new KnownMicrocontroller
      {
         MacAddress = "77:68:11:11:22:22",
         IpAddress = "192.168.178.1"
      };
      microcontrollerService.Save(microcontroller, pushToMicrocontroller: false);
      return microcontroller.MicroControllerId;
   }

   public AppSettings AppSettings { get; }

   private async Task TestRegisterKnownTopicAsync(IKnownTopicsCrudService knownTopicsService)
   {
      var knownTopic = new KnownTopic
      {
         Topic = "esp8266/0001/temperature/1",
         Description = "Temperatursensor"
      };

      //string        json    = JsonSerializer.Serialize(knownTopic);
      //StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

      //string url = $"{AppSettings.RestApiSettings.Url}/KnownTopics/RegisterKnownTopic";
      //HttpClient httpClient = new HttpClient();
      //HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(url,
      //                                                                     content);
      //string responseContent = await httpResponseMessage.Content.ReadAsStringAsync();

      await knownTopicsService.Save(knownTopic);

      bool exists = await knownTopicsService.ExistsByTopicName(knownTopic.Topic);
   }

   private async Task<string> TestLoadscriptAsync(IScriptCrudService scriptCrudService)
   {
      Script? script = await scriptCrudService.LoadScript("TemperatureToColor");
      if (script == null)
      {
         throw new Exception("Script with this name does not exist!");
      }
      return script.SourceCode;
   }

   private async Task TestExpressionParser2Async()
   {
      string expression = "::GetCalendarWeek()";
      string parsedExpression1 = await ExpressionParser.ResolveExpression(expression);

      expression = "::TemperatureToColor(12.5)";
      string parsedExpression2 = await ExpressionParser.ResolveExpression(expression);

      expression += " + Read('heatmap/index') + 1";
      string parsedExpression3 = await ExpressionParser.ResolveExpression(expression);
   }

   private  Task TestHandleMqttMessage()
   {
     return Task.CompletedTask;
   }
}
