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
using Domain.Pocos;
using Domain.Services;
using Domain.Services.RuleEngine;
using Domain.Services.Timer;
using Infrastructure;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using System.Globalization;
using System.Text.Json;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;
using Rule = Domain.Pocos.Rule;


namespace UnitTests
{
    public class ScriptDataIn
    {
        public int IndexLed { get; set; }
        public double Temperature { get; set; }

        /// <summary>
        /// Can be JSON.
        /// </summary>
        public string? Data { get; set; } = null;
    }

    public class TestFixture : TestBedFixture
    {
        protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
        {
            services.AddSingleton<IPrepareTargetPayload, PrepareTargetPayload>();
            services.AddSingleton<IDataTransferService, DataTransferService>();
            services.AddSingleton<IExpressionParser, ExpressionParser>();
            services.AddSingleton<ICalculationService, CalculationService>();
            services.AddSingleton<IKnownTopicsCrudService, KnownTopicsServiceMock>();
            services.AddSingleton<IVariablesResolver, VariablesResolver>();
            services.AddSingleton<IExpressionEvaluationService, ExpressionEvaluationService>();
            services.AddSingleton<IRulesCrudService, RulesServiceMock>();
            services.AddSingleton<IScriptsResolver, ScriptsResolver>();

            // using MOQ to create Mock objects where there are no special requirements for the MockObject
            services.AddSingleton(new Mock<ISettingsCrudService>().Object);
            services.AddSingleton(new Mock<IMicrocontrollerService>().Object);
            services.AddSingleton(new Mock<IHueBridgeService>().Object);
            services.AddSingleton(new Mock<ITopicHistoryCrudService>().Object);
            services.AddSingleton(new Mock<IScriptCrudService>().Object);
            services.AddSingleton(new Mock<IScriptService>().Object);
            services.AddSingleton(new Mock<IProjectCrudService>().Object);
            services.AddSingleton(new Mock<INamespaceCrudService>().Object);

            // Quartz.net
            services.AddSingleton<Quartz.Spi.IJobFactory, JobFactory>();
            services.AddSingleton<PublishTimeJob>();
            //services.AddSingleton<IIoTZooMqttClient, MqttClient>();

            services.AddSingleton(new Mock<IIoTZooMqttClient>().Object);
        }

        protected override ValueTask DisposeAsyncCore()
            => new();

        protected override IEnumerable<TestAppSettings> GetTestAppSettings()
        {
            yield return new() { Filename = "appsettings.json", IsOptional = false };
        }
    }

    public class RuleEngineUnitTest : TestBed<TestFixture>
    {
        private readonly ITestOutputHelper testOutHelper;

        private ICalculationService CalculationService { get; }

        private IPrepareTargetPayload PrepareTargetPayload { get; }

        private IExpressionEvaluationService ExpressionEvaluationService { get; }

        private IDataTransferService DataTransferService { get; }

        private IRulesCrudService RulesService { get; }

        private IExpressionParser ExpressionParser { get; }

        private IVariablesResolver VariablesResolver { get; }

        private IKnownTopicsCrudService KnownTopicsService { get; }

        private IIoTZooMqttClient IoTZooMqttClient { get; }

        public RuleEngineUnitTest(ITestOutputHelper testOutputHelper, TestFixture testFixture) : base(testOutputHelper,
            testFixture)
        {
            testOutHelper = testOutputHelper;
            CalculationService = testFixture.GetService<ICalculationService>(testOutHelper)!;
            PrepareTargetPayload = testFixture.GetScopedService<IPrepareTargetPayload>(testOutHelper)!;
            ExpressionEvaluationService = testFixture.GetScopedService<IExpressionEvaluationService>(testOutHelper)!;
            DataTransferService = testFixture.GetScopedService<IDataTransferService>(testOutHelper)!;
            ExpressionParser = testFixture.GetScopedService<IExpressionParser>(testOutputHelper)!;
            VariablesResolver = testFixture.GetScopedService<IVariablesResolver>(testOutHelper)!;
            KnownTopicsService = testFixture.GetScopedService<IKnownTopicsCrudService>(testOutHelper)!;
            RulesService = testFixture.GetScopedService<IRulesCrudService>(testOutHelper)!;
            IoTZooMqttClient = testFixture.GetScopedService<IIoTZooMqttClient>(testOutHelper)!;
        }

        /// <summary>
        /// Test simple addition.
        /// </summary>
        /// <exception cref="Exception"></exception>
        [Fact]
        public void CalculationServiceAdditionTest1()
        {
            var result = CalculationService.Calculate("1 + 1");
            Assert.NotNull(result);
            Assert.Equal(2, Convert.ToInt32(result));
        }

        /// <summary>
        /// Test addition.
        /// </summary>
        /// <exception cref="Exception"></exception>
        [Fact]
        public void CalculationServiceAdditionTest2()
        {
            var result = CalculationService.Calculate("1 + 2 + 3 + 4 + 5 + 6");
            Assert.NotNull(result);
            Assert.Equal(21, Convert.ToInt32(result));
        }

        /// <summary>
        /// Test substraction.
        /// </summary>
        /// <exception cref="Exception"></exception>
        [Fact]
        public void CalculationServiceSubstractionTest1()
        {
            var result = CalculationService.Calculate("1 - 1");
            Assert.NotNull(result);
            Assert.Equal(0, Convert.ToInt32(result));
        }

        /// <summary>
        /// Test sinus calculation.
        /// </summary>
        /// <exception cref="Exception"></exception>
        [Fact]
        public void CalculationServiceTestSinus()
        {
            var result = CalculationService.Calculate("sin(90)");

            Assert.Equal(1, Convert.ToInt32(result));
        }

        /// <summary>
        /// Test substraction.
        /// </summary>
        /// <exception cref="Exception"></exception>
        [Fact]
        public void CalculationServiceTestSinusMultiple()
        {
            var result = CalculationService.Calculate("sin(90) - 1");

            Assert.Equal(0, Convert.ToInt32(result));
        }

        /// <summary>
        /// Test text comparison matching.
        /// </summary>
        /// <exception cref="Exception"></exception>
        [Fact]
        public void CalcComparisonStringTest1()
        {
            var result = CalculationService.Calculate("'IoTZoo' == 'IoTZoo'");

            Assert.True(Convert.ToBoolean(result));
        }

        /// <summary>
        /// Test text comparison not matching.
        /// </summary>
        /// <exception cref="Exception"></exception>
        [Fact]
        public void CalcComparisonStringFailTest()
        {
            var result = CalculationService.Calculate("'iotzoo' == 'IOTZOO'");

            Assert.False(Convert.ToBoolean(result));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(-100)]
        public async Task IncrementTest1A(int value)
        {
            // Scenario: Increment a value by 1.
            // arrange
            Rule rule = new Rule
            {
                SourceTopic = "esp32/123/counter/0",
                TargetTopic = "IncResult",
                TargetPayload = $"{FunctionNames.Calc}({FunctionNames.Input} + {value});"
            };

            var topicEntry = new TopicEntry
            {
                Topic = "esp32/123/counter/0",
                Payload = (200).ToString()
            };
            // act
            string? targetPayload = await PrepareTargetPayload.Execute(rule, topicEntry);

            // assert
            Assert.NotNull(targetPayload);
            Assert.Equal(200 + value, Convert.ToInt32(targetPayload));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(-100)]
        public async Task IncrementTest1B(int value)
        {
            // Scenario: Increment a value by 1.
            // arrange
            Rule rule = new Rule
            {
                SourceTopic = "esp32/123/counter/0",
                TargetTopic = "IncResult",
                TargetPayload = $"{FunctionNames.Input} + {value};"
            };

            var topicEntry = new TopicEntry
            {
                Topic = "esp32/123/counter/0",
                Payload = (200).ToString()
            };
            // act
            string? targetPayload = await PrepareTargetPayload.Execute(rule, topicEntry);

            // assert
            Assert.NotNull(targetPayload);
            Assert.Equal(200 + value, Convert.ToInt32(targetPayload));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(-100)]
        public async Task IncrementTest2A(int value)
        {
            // arrange
            Rule rule = new Rule
            {
                SourceTopic = "IOTZOO/TIME/EVERY_01_SECOND",
                TargetTopic = "heatmap/index",
                TargetPayload = $"{FunctionNames.Calc}({FunctionNames.ReadFromMemory}('heatmap/index') + 1);"
            };

            var topicEntry = new TopicEntry
            {
                Topic = "IOTZOO/TIME/EVERY_01_SECOND"
            };

            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "heatmap/index", Payload = value.ToString() });

            // act
            string? targetPayload = await PrepareTargetPayload.Execute(rule, topicEntry);

            // assert
            Assert.NotNull(targetPayload);
            Assert.Equal(1 + value, Convert.ToInt32(targetPayload));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(-100)]
        public async Task IncrementTest2B(int value)
        {
            // arrange
            Rule rule = new Rule
            {
                SourceTopic = "IOTZOO/TIME/EVERY_01_SECOND",
                TargetTopic = "heatmap/index",
                TargetPayload = $"heatmap/index + 1;"
            };

            var topicEntry = new TopicEntry
            {
                Topic = "IOTZOO/TIME/EVERY_01_SECOND"
            };

            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "heatmap/index", Payload = value.ToString() });

            // act
            string? targetPayload = await PrepareTargetPayload.Execute(rule, topicEntry);

            // assert
            Assert.NotNull(targetPayload);
            Assert.Equal(1 + value, Convert.ToInt32(targetPayload));
        }


        //[Theory]
        //[InlineData(1)]
        //[InlineData(10)]
        //[InlineData(-100)]
        //public async Task WriteTest1(int value)
        //{
        //   string? targetPayload = $"{FunctionNames.WriteToMemory}('heatmap/index', {value}')";

        //   if (targetPayload!.Contains(FunctionNames.WriteToMemory, StringComparison.OrdinalIgnoreCase))
        //   {
        //      int startIndexTopic = targetPayload.IndexOf($"{FunctionNames.WriteToMemory}");
        //      startIndexTopic += FunctionNames.WriteToMemory.Length;
        //      int endIndexTopic = targetPayload.IndexOf('\'', startIndexTopic + 2);
        //      if (-1 == endIndexTopic)
        //      {
        //         endIndexTopic = targetPayload.IndexOf('"', startIndexTopic + 2);
        //      }

        //      int startIndexPayload = targetPayload.IndexOf(',', startIndexTopic + 2);
        //      int endIndexPayload = targetPayload.IndexOf(')', startIndexTopic + 2);

        //      string topic = targetPayload.Substring(startIndexTopic + 1, endIndexTopic - startIndexTopic);
        //      string? payload = targetPayload.Substring(startIndexPayload + 1, endIndexPayload - startIndexPayload - 2).Trim();

        //      if (!string.IsNullOrEmpty(topic))
        //      {
        //         var fromMemory = (from data in DataTransferService.ReceivedTopicsQueue where data.Topic == topic select data).FirstOrDefault();
        //         if (null != fromMemory)
        //         {
        //            fromMemory.Payload = payload;
        //         }

        //         this.KnownTopicsService.Save(new KnownTopic { Topic = topic, LastPayload = payload });
        //      }

        //      Assert.NotNull(targetPayload);

        //   }
        //}

        [Fact]
        public async Task CalcTestIgnoreCase()
        {
            // Scenario: Decrement a value

            Rule rule = new Rule
            {
                SourceTopic = "esp32/123/counter/0",
                TargetTopic = "IncResult",
                TargetPayload = $"{FunctionNames.Calc.ToLower()}({FunctionNames.Input} - 1);"
            };

            var topicEntry = new TopicEntry
            {
                Topic = "esp32/123/counter/0",
                Payload = "159"
            };
            string? targetPayload = await PrepareTargetPayload.Execute(rule, topicEntry); // 159
            Assert.NotNull(targetPayload);
            Assert.Equal(158, Convert.ToInt32(targetPayload));
        }

        [Fact]
        public async Task CreateTargetPayloadFromRuleTest()
        {
            // Scenario 1: print temperature value on an LCD display.
            Rule rule = new Rule
            {
                SourceTopic = "esp32/1D:10:AB:C5:CA:D0/temperature/0",
                TargetTopic = "esp32/F6:3E:97:E7:11:57/lcd160x/0",
                TargetPayload = "{'text': 'Boiler: " + FunctionNames.Input + " °C ', 'clear': false, 'x':0, 'y': 1}"
            };

            var topicEntry = new TopicEntry
            {
                Topic = "esp32/1D:10:AB:C5:CA:D0/temperature/0",
                Payload = "29.8"
            };

            string? targetPayload = await PrepareTargetPayload.Execute(rule, topicEntry);
            if (null == targetPayload)
            {
                throw new Exception();
            }

            targetPayload = System.Text.RegularExpressions.Regex.Unescape(targetPayload);
            Assert.NotEmpty(targetPayload);
            Assert.Equal("{'text': 'Boiler: 29.8 °C ', 'clear': false, 'x':0, 'y': 1}", targetPayload);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(-1000)]
        public async Task EvaluateExpressionGreaterThanTest(int value)
        {
            Rule rule = new Rule
            {
                Expression = $"{FunctionNames.Input} > {value}",
                SourceTopic = "esp32/1D:10:AB:C5:CA:D0/counter/0",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, (value + 1).ToString());

            Assert.True(expressionEvaluationResult.Matches);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(-1000)]
        public async Task EvaluateExpressionGreaterThanOrEqualsTest(int value)
        {
            Rule rule = new Rule
            {
                Expression = $"{FunctionNames.Input} >= {value}",
                SourceTopic = "esp32/1D:10:AB:C5:CA:D0/counter/0",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, (value + 0).ToString());
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(-1000)]
        public async Task EvaluateExpressionLessThanTest(int value)
        {
            Rule rule = new Rule
            {
                Expression = $"{FunctionNames.Input} < {value}",
                SourceTopic = "esp32/1D:10:AB:C5:CA:D0/counter/0",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, value.ToString());
            Assert.False(expressionEvaluationResult.Matches);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(-1000)]
        public async Task EvaluateExpressionLessThanOrEqualsTest(int value)
        {
            Rule rule = new Rule
            {
                Expression = $"{FunctionNames.Input} <= {value}",
                SourceTopic = "esp32/1D:10:AB:C5:CA:D0/counter/0",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, value.ToString());
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task EvaluateExpressionNotEqualTest1()
        {
            Rule rule = new Rule
            {
                Expression = $"{FunctionNames.Input} <> 77",
                SourceTopic = "esp32/1D:10:AB:C5:CA:D0/counter/0",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "78");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task EvaluateExpressionNotEqualTest2()
        {
            Rule rule = new Rule
            {
                Expression = $"{FunctionNames.Input} != 77",
                SourceTopic = "esp32/1D:10:AB:C5:CA:D0/counter/0",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "78");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task EvaluateExpressionModuloTest1()
        {
            Rule rule = new Rule
            {
                Expression = $"{FunctionNames.Input} % 2",
                SourceTopic = "esp32/1D:10:AB:C5:CA:D0/counter/0",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "80");
            Assert.False(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task EvaluateExpressionModuloTest2()
        {
            Rule rule = new Rule
            {
                Expression = $"{FunctionNames.Input} % 2",
                SourceTopic = "esp32/1D:10:AB:C5:CA:D0/counter/0",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "81");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task EvaluateExpressionEqualsTest()
        {
            Rule rule = new Rule
            {
                Expression = $"{FunctionNames.Input} == 77",
                SourceTopic = "esp32/123/counter/0",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "77");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task EvaluateExpressionLessThanJsonTest()
        {
            Rule rule = new Rule
            {
                Expression = """{ "Operator": "<", "Value": "70"}""",
                SourceTopic = "HEARTBEAT",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "78");
            Assert.False(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task EvaluateExpressionContainsJsonTest()
        {
            Rule rule = new Rule
            {
                Expression = """{ "Operator": "contains", "Value": "IoT"}""",
                SourceTopic = "HEARTBEAT",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "IoT");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task EvaluateExpressionContainsNotJsonTest()
        {
            Rule rule = new Rule
            {
                Expression = """{ "Operator": "contains not", "Value": "IoT"}""",
                SourceTopic = "HEARTBEAT",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "Freudenreich");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task EvaluateExpressionLessThanOrEqualsJsonTest()
        {
            Rule rule = new Rule
            {
                Expression = """{ "Operator": "<=", "Value": "70"}""",
                SourceTopic = "HEARTBEAT",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "78");
            Assert.False(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task EvaluateExpressionLargerThanJsonTest()
        {
            Rule rule = new Rule
            {
                Expression = """{ "Operator": ">", "Value": "70"}""",
                SourceTopic = "HEARTBEAT",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "78");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task EvaluateExpressionLargerThanOrEqualsJsonTest()
        {
            Rule rule = new Rule
            {
                Expression = """{ "Operator": ">=", "Value": "70"}""",
                SourceTopic = "HEARTBEAT",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "78");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task EvaluateExpressionStringEqualsJsonTest()
        {
            Rule rule = new Rule
            {
                Expression = """[{ "Operator": "==", "Value": "RED"}]""",
                SourceTopic = "IF_RED",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "RED");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task EvaluateExpressionStringEqualsAtLeastOneOfTwoValuesJsonTest()
        {
            Rule rule = new Rule
            {
                Expression = """[{ "Operator": "==", "Value": "RED"}, { "Operator": "==", "Value": "ORANGE"}]""", // OR
                SourceTopic = "IF_RED",
                TargetTopic = "TurnOnRedLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "ORANGE");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task EvaluateExpressionStringNotEqualsAtLeastOneOfTwoValuesJsonTest()
        {
            Rule rule = new Rule
            {
                Expression =
                    """[{ "Operator": "==", "Value": "RED"}, { "Operator": "==", "Value": "ORANGE"}, { "Operator": "==", "Value": "YELLOW"}]""", // OR
                SourceTopic = "IF_RED_OR_ORANGE_OR_YELLOW",
                TargetTopic = "TurnOnLed"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "BLUE");
            Assert.False(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task ReadFromMemoryTest1()
        {
            Rule rule = new Rule
            {
                Expression = $"{FunctionNames.Input} > 5 && {FunctionNames.ReadFromMemory}('Button1State') == 1",
                SourceTopic = "IOTZOO/TIME/SECOND_CHANGED",
                TargetTopic = "TurnOnRedLed"
            };

            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "Button1State", Payload = "1" });

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "7");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task ReadFromMemoryTest2()
        {
            Rule rule = new Rule
            {
                Expression = $"{FunctionNames.Input} > 5 && {FunctionNames.ReadFromMemory}('Button1State') == 1",
                SourceTopic = "IOTZOO/TIME/SECOND_CHANGED",
                TargetTopic = "TurnOnRedLed"
            };

            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "Button1State", Payload = "1" });

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "7");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task ReadFromMemoryTest3()
        {
            Rule rule = new Rule
            {
                Expression = string.Empty,
                SourceTopic = "INC_VARIABLE1",
                TargetTopic = "VARIABLE1",
                TargetPayload = $"{FunctionNames.Calc}({FunctionNames.ReadFromMemory}('VARIABLE1') + 1);"
            };

            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "VARIABLE1", Payload = 1.ToString() });

            TopicEntry topicEntry = new TopicEntry { Topic = "INC_VARIABLE1" };

            var targetPayload = await PrepareTargetPayload.Execute(rule, topicEntry);
            Assert.Equal("2", targetPayload);
        }

        [Fact]
        public async Task ReadFromMemoryTest4()
        {
            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            {
                Topic = "VARIABLE1",
                Payload = 1.ToString()
            });

            var targetPayload = await VariablesResolver.ResolveVariables("VARIABLE1");
            Assert.Equal("1", targetPayload);
        }

        [Fact]
        public async Task VariablesResolverTest1()
        {
            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "VARIABLE1", Payload = 1.ToString() });

            var expression = await VariablesResolver.ResolveVariables("VARIABLE1 + 1");
            Assert.Equal("1 + 1", expression);
            var result = CalculationService.Calculate(expression);
            Assert.Equal(2, Convert.ToInt32(result));
        }

        [Fact]
        public async Task VariablesResolverTest2()
        {
            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "VARIABLE1", Payload = 1.ToString() });

            var expression = await VariablesResolver.ResolveVariables("VARIABLE1 - 1");
            Assert.Equal("1 - 1", expression);
            var result = CalculationService.Calculate(expression);
            Assert.Equal(0, Convert.ToInt32(result));
        }

        [Fact]
        public async Task JsonPathTest2()
        {
            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "colors", Payload = """[{"red":{"r":255,"g":0,"b":0}},{"green":{"r":0,"g":255,"b":0}}]""" });

            Rule rule0 = new Rule
            {
                SourceTopic = "IOTZOO/JSONPATH/TEST1",
                TargetTopic = "esp32/E4:65:B8:79:27:E0/doesnotmatter",
                TargetPayload = $"{FunctionNames.ReadFromMemory}(\"colors\")"
            };

            var topicEntry0 = new TopicEntry();
            var targetPayload0 = await PrepareTargetPayload.Execute(rule0, topicEntry0);

            Assert.NotNull(targetPayload0);
            Assert.NotEmpty(targetPayload0);
            Assert.Equal("""[{"red":{"r":255,"g":0,"b":0}},{"green":{"r":0,"g":255,"b":0}}]""", targetPayload0);

            Rule rule1 = new Rule
            {
                SourceTopic = "IOTZOO/TEST",
                TargetTopic = "esp32/E4:65:B8:79:27:E0/doesnotmatter",
                TargetPayload = $"{FunctionNames.ReadFromMemory}('colors')['green']"
            };

            var topicEntry1 = new TopicEntry();
            var targetPayload1 = await PrepareTargetPayload.Execute(rule1, topicEntry1);

            Assert.NotNull(targetPayload1);
            Assert.NotEmpty(targetPayload1);
            Assert.Equal("""
                         {
                           "r": 0,
                           "g": 255,
                           "b": 0
                         }
                         """, targetPayload1);
        }

        [Fact]
        public async Task JsonPathTest3()
        {
            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "colors", Payload = """[{"red":"#F22A27"}, {"blue": "#5C27F2"}, {"green": "#27F25E"}]""" });

            Rule rule0 = new Rule
            {
                SourceTopic = "IOTZOO/JSONPATH/TEST2",
                TargetTopic = "esp32/E4:65:B8:79:27:E0/doesnotmatter",
                TargetPayload = $"{FunctionNames.ReadFromMemory}('colors')"
            };

            var topicEntry0 = new TopicEntry();
            var targetPayload0 = await PrepareTargetPayload.Execute(rule0, topicEntry0);

            Assert.NotNull(targetPayload0);
            Assert.NotEmpty(targetPayload0);
            Assert.Equal("""[{"red":"#F22A27"}, {"blue": "#5C27F2"}, {"green": "#27F25E"}]""", targetPayload0);

            Rule rule1 = new Rule
            {
                SourceTopic = "IOTZOO/TEST",
                TargetTopic = "esp32/E4:65:B8:79:27:E0/doesnotmatter",
                TargetPayload = $"{FunctionNames.ReadFromMemory}('colors')['green']"
            };

            var topicEntry1 = new TopicEntry();
            var targetPayload1 = await PrepareTargetPayload.Execute(rule1, topicEntry1);

            Assert.NotNull(targetPayload1);
            Assert.NotEmpty(targetPayload1);
            Assert.Equal("#27F25E", targetPayload1);
        }

        [Fact]
        public async Task TestStepperMotor()
        {
            Rule rule0 = new Rule
            {
                RuleId = 10,
                SourceTopic = "triggered",
                TargetTopic = "stepper/esp32/FC:B4:67:18:0D:90/stepper/0/actions",
                TargetPayload = """[{ "degrees": 90, "rpm": 10, "id": 31}, { "degrees": -136, "rpm": 16, "id": 41} ]"""
            };
            await RulesService.Save(rule0);
            var topicEntry0 = new TopicEntry();
            var targetPayload0 = await PrepareTargetPayload.Execute(rule0, topicEntry0);
            Assert.NotNull(targetPayload0);
        }

        [Fact]
        public async Task JsonPathTest4()
        {
            // Arrange
            DataTransferService.ReceivedTopicsQueue.Clear();
            //this.DataTransferService.Topics.Enqueue(new TopicEntry { Topic = "IOTZOO/COLORS", Payload = """
            //   [{"Red":"#F44336"}, {"Pink": "#E91E63"}, {"Purple": "#9C27B0"}, {"DeepPurple": "#673AB7"},
            //   {"Indigo": "#3F51B5"}, {"Blue": "#2196F3"}, {"LightBlue": "#03A9F4"}, {"Cyan": "#00BCD4"}, {"Teal": "#009688"}, {"Green": "#009688"}, {"LightGreen": "#8BC34A"},
            //   {"Lime": "#CDDC39"}, {"Yellow": "#FFEB3B"}, {"Amber": "#FFC107"}, {"Orange": "#FF9800"}, {"DeepOrange": "#FF5722"}, {"Brown": "#795548"}, {"BlueGray": "#607D8B"}, {"Gray": "#9E9E9E"}]
            //   """ });
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "temperature", Payload = "-11.7" });
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            {
                Topic = $"IOTZOO/TIME/{TopicConstants.EVERY_05_SECONDS}",
                Payload =
                    " { \"DateTime\": \"25.01.2025 17:32:45\", \"Time\": \"17:32:45\", \"TimeShort\": \"17:32\" }."
            });
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "HEATMAP_COLOR", Payload = string.Empty });

            await KnownTopicsService.Save(new KnownTopic
            { Topic = $"IOTZOO/TIME/{TopicConstants.EVERY_05_SECONDS}" });

            await KnownTopicsService.Save(new KnownTopic
            {
                Topic = "IOTZOO/COLORS",
                LastPayload = """
                          [{"Red":"#F44336"}, {"Pink": "#E91E63"}, {"Purple": "#9C27B0"}, {"DeepPurple": "#673AB7"},
                          {"Indigo": "#3F51B5"}, {"Blue": "#2196F3"}, {"LightBlue": "#03A9F4"}, {"Cyan": "#00BCD4"}, {"Teal": "#009688"}, {"Green": "#009688"}, {"LightGreen": "#8BC34A"},
                          {"Lime": "#CDDC39"}, {"Yellow": "#FFEB3B"}, {"Amber": "#FFC107"}, {"Orange": "#FF9800"}, {"DeepOrange": "#FF5722"}, {"Brown": "#795548"}, {"BlueGray": "#607D8B"}, {"Gray": "#9E9E9E"}]
                          """
            });

            Rule rule0 = new Rule
            {
                RuleId = 10,
                SourceTopic = "IOTZOO/JSONPATH/TEST2",
                TargetTopic = "esp32/E4:65:B8:79:27:E0/doesnotmatter",
                TargetPayload = $"{FunctionNames.ReadFromMemory}('IOTZOO/COLORS')"
            };
            await RulesService.Save(rule0);

            Rule rule1 = new Rule
            {
                RuleId = 11,
                SourceTopic = "IOTZOO/TIME/EVERY_05_SECONDS",
                Expression = $"{FunctionNames.ReadFromMemory}('temperature') < -20",
                TargetTopic = "HEATMAP_COLOR",
                TargetPayload = "Blue"
            };
            await RulesService.Save(rule1);

            Rule rule2 = new Rule
            {
                RuleId = 12,
                SourceTopic = $"IOTZOO/TIME/{TopicConstants.EVERY_05_SECONDS}",
                Expression =
                    $"{FunctionNames.ReadFromMemory}('temperature') > -20 && {FunctionNames.ReadFromMemory}('temperature') < -10",
                TargetTopic = "HEATMAP_COLOR",
                TargetPayload = "Green"
            };
            await RulesService.Save(rule2);

            var topicEntry0 = new TopicEntry();
            var targetPayload0 = await PrepareTargetPayload.Execute(rule0, topicEntry0);

            Assert.NotNull(targetPayload0);
            Assert.NotEmpty(targetPayload0);
            Assert.Equal("""
                         [{"Red":"#F44336"}, {"Pink": "#E91E63"}, {"Purple": "#9C27B0"}, {"DeepPurple": "#673AB7"},
                         {"Indigo": "#3F51B5"}, {"Blue": "#2196F3"}, {"LightBlue": "#03A9F4"}, {"Cyan": "#00BCD4"}, {"Teal": "#009688"}, {"Green": "#009688"}, {"LightGreen": "#8BC34A"},
                         {"Lime": "#CDDC39"}, {"Yellow": "#FFEB3B"}, {"Amber": "#FFC107"}, {"Orange": "#FF9800"}, {"DeepOrange": "#FF5722"}, {"Brown": "#795548"}, {"BlueGray": "#607D8B"}, {"Gray": "#9E9E9E"}]
                         """, targetPayload0);

            Rule ruleReadColorGreenByName = new Rule
            {
                SourceTopic = "IOTZOO/TEST",
                TargetTopic = "esp32/E4:65:B8:79:27:E0/doesnotmatter",
                TargetPayload = $"{FunctionNames.ReadFromMemory}('IOTZOO/COLORS')['Green']"
            };

            var topicEntry1 = new TopicEntry();
            var targetPayload1 = await PrepareTargetPayload.Execute(ruleReadColorGreenByName, topicEntry1);

            Assert.NotNull(targetPayload1);
            Assert.NotEmpty(targetPayload1);
            Assert.Equal("#009688", targetPayload1);


            /*
          var messageHandlingResult1 = await IoTZooMqttClient.HandleMqttMessage(new TopicEntry
          {
             Topic = $"IOTZOO/TIME/{TopicConstants.EVERY_05_SECONDS}",
             Payload =
                  " { \"DateTime\": \"25.01.2025 17:34:45\", \"Time\": \"17:34:45\", \"TimeShort\": \"17:34\" }."
          });

          Assert.NotNull(messageHandlingResult1.TargetPayloads);

          Assert.Equal("Green", messageHandlingResult1.TargetPayloads.First()!.Payload);

          var messageHandlingResult2 =
              await IoTZooMqttClient.HandleMqttMessage(new TopicEntry { Topic = "temperature", Payload = "-21.3" });
          var messageHandlingResult3 = await IoTZooMqttClient.HandleMqttMessage(new TopicEntry
              { Topic = $"IOTZOO/TIME/{TopicConstants.EVERY_05_SECONDS}" });

          Assert.NotNull(messageHandlingResult3.TargetPayloads);
          Assert.Equal("Blue", messageHandlingResult3.TargetPayloads.First()!.Payload);

          Rule ruleReadColorByName = new Rule
          {
              SourceTopic = "IOTZOO/TEST",
              TargetTopic = "esp32/E4:65:B8:79:27:E0/doesnotmatter",
              TargetPayload =
                  $"{FunctionNames.ReadFromMemory}('IOTZOO/COLORS')['{messageHandlingResult3.TargetPayloads.First()!.Payload}']"
          };

          var topicEntry2 = new TopicEntry();
          var targetPayload2 = await PrepareTargetPayload.Execute(ruleReadColorByName, topicEntry2);

          Assert.NotNull(targetPayload2);
          Assert.NotEmpty(targetPayload2);
          Assert.Equal("#2196F3", targetPayload2);
          */
        }

        [Fact]
        public async Task ReadFromMemoryMultipleTest()
        {
            Rule rule = new Rule
            {
                Expression =
                    $"{FunctionNames.Input} > 5 && {FunctionNames.ReadFromMemory}('Button1State') == 1 && {FunctionNames.ReadFromMemory}('Button2State') == 0",
                SourceTopic = "IOTZOO/TIME/SECOND_CHANGED",
                TargetTopic = "TurnOnRedLed"
            };

            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry { Topic = "Button1State", Payload = "1" });
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry { Topic = "Button2State", Payload = "0" });

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "7");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task ReadFromMemoryIsDayTest()
        {
            Rule rule = new Rule
            {
                Expression = $"{FunctionNames.ReadFromMemory}('{TopicConstants.IS_DAY_MODE}') == 1",
                SourceTopic = TopicConstants.SUNRISE_NOW,
                TargetTopic = "GetUpRoutine"
            };

            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = $"{TopicConstants.IS_DAY_MODE}", Payload = 1.ToString() });

            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = TopicConstants.SUNRISE_NOW, Payload = DateTime.Now.ToString(CultureInfo.InvariantCulture) });

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, "1");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task JsonPathTest1A()
        {
            Rule rule = new Rule
            {
                Expression = "$['t'] > 55",
                SourceTopic = "esp32/C4:DD:57:8E:5C:5C/temperature/1/corrected",
                TargetTopic = "HOT_HERE"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression,
                    "{\"t\": 18.7, \"anotherField\": 12.45455}");
            Assert.False(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task JsonPathTest1B()
        {
            Rule rule = new Rule
            {
                Expression = "$['t'] < 55",
                SourceTopic = "esp32/C4:DD:57:8E:5C:5C/temperature/1/corrected",
                TargetTopic = "HOT_HERE"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression,
                    "{\"t\": 18.7, \"anotherField\": 12.45455}");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task JsonPathMultipleAndTest1()
        {
            Rule rule = new Rule
            {
                Expression = "$['t'] > 55 && $['b'] > 11",
                SourceTopic = "esp32/C4:DD:57:8E:5C:5C/temperature/1/corrected",
                TargetTopic = "DO_SOMETHING"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression,
                    "{\"t\": 68.7, \"b\": 12.45455}");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task JsonPathMultipleAndTest2()
        {
            Rule rule = new Rule
            {
                Expression = "$['unit'] == 'cm'",
                SourceTopic = "esp32/C4:DD:57:8E:5C:5C/temperature/1/corrected",
                TargetTopic = "DO_SOMETHING"
            };

            var expressionEvaluationResult = await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression,
                "{\"unit\": \"cm\"}");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task JsonPathMultipleOrTest()
        {
            Rule rule = new Rule
            {
                Expression = "$['t'] > 55 || $['b'] > 11",
                SourceTopic = "esp32/C4:DD:57:8E:5C:5C/temperature/1/corrected",
                TargetTopic = "DO_SOMETHING"
            };

            var expressionEvaluationResult =
                await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression,
                    "{\"t\": 18.7, \"b\": 15.45455}");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task ReadFromMemoryMultipleJsonPathTest()
        {
            Rule rule = new Rule
            {
                Expression =
                    $"$['unit'] == 'cm' && {FunctionNames.ReadFromMemory}('Button1State') == 1 && {FunctionNames.ReadFromMemory}('Button2State') == 0",
                SourceTopic = "IOTZOO/TIME/SECOND_CHANGED",
                TargetTopic = "TurnOnRedLed"
            };

            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry { Topic = "Button1State", Payload = "1" });
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry { Topic = "Button2State", Payload = "0" });

            var expressionEvaluationResult = await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression,
                "{\"unit\": \"cm\"}");
            Assert.True(expressionEvaluationResult.Matches);
        }

        [Fact]
        public async Task ReactOnRuleTest()
        {
            Rule rule = new Rule
            {
                Expression = $"'{FunctionNames.Input}' == 'pressed'",
                SourceTopic = "esp32/B0:A7:32:28:3C:F8/rotary_encoder/0/button_pressed",
                TargetTopic = "IOTZOO/HUE/LIGHT_ON"
            };

            await RulesService.Save(rule);

            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "IOTZOO/HUE/LIGHT_ON", Payload = "pressed" });

            var expressionEvaluationResult = await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression,
                "pressed");
            Assert.True(expressionEvaluationResult.Matches);
        }

        /// <summary>
        /// The aim is to replace input1 in rule.TargetPayload with the payload value.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [Theory]
        [InlineData(19)]
        [InlineData(22)]
        public async Task SimpleTargetPayloadManipulationTest1(int lightId)
        {
            Rule rule = new Rule
            {
                SourceTopic = "HUE_LIGHT_ON",
                TargetTopic = "esp32/08:D1:F9:E0:F2:04/lcd160x/0",
                TargetPayload = """{"text": "Light with LightId """ + lightId.ToString() +
                                """ turned on!", "clear": true, "x":4, "y": 0}{"text": """ + '\"' +
                                FunctionNames.Input + '\"' + """, "clear": true, "x":4, "y": 0}"""
            };

            var topicEntry = new TopicEntry
            {
                Topic = "HUE_LIGHT_ON",
                Payload = lightId.ToString() // LightId
            };
            var targetPayload = await PrepareTargetPayload.Execute(rule, topicEntry);
            Assert.NotNull(targetPayload);
            Assert.NotEmpty(targetPayload);
            Assert.Equal(
                "{\"text\": \"Light with LightId " + lightId.ToString() +
                " turned on!\", \"clear\": true, \"x\":4, \"y\": 0}{\"text\": \"" + lightId.ToString() +
                "\", \"clear\": true, \"x\":4, \"y\": 0}",
                targetPayload);
        }

        [Fact]
        public async Task ComplexTargetPayloadManipulationTest1()
        {
            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "Button1State", Payload = "1" });

            Rule rule1 = new Rule
            {
                SourceTopic = "A",
                TargetTopic = "B",
                TargetPayload = "{\"A1\": 42, \"A2\": " + FunctionNames.ReadFromMemory + "(Button1State)}"
            };

            Rule rule2 = new Rule
            {
                SourceTopic = "B",
                TargetTopic = "C",
                TargetPayload = """{"B1": $['A1']}"""
            };

            var topicEntry1 = new TopicEntry
            {
                Topic = "A",
                Payload = "19"
            };
            var targetPayload1 = await PrepareTargetPayload.Execute(rule1, topicEntry1);
            Assert.Equal("{\"A1\": 42, \"A2\": 1}", targetPayload1);

            var topicEntry2 = new TopicEntry
            {
                Topic = "B",
                Payload = targetPayload1
            };
            var targetPayload2 = await PrepareTargetPayload.Execute(rule2, topicEntry2);

            Assert.NotNull(targetPayload2);
            Assert.NotEmpty(targetPayload2);
            Assert.Equal("{\"B1\": 42}", targetPayload2);
        }

        [Fact]
        public async Task ComplexTargetPayloadManipulationTest2()
        {
            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "VARIABLE1", Payload = "33" });
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "COLOR_RED_COMPONENT", Payload = "125" });

            Rule rule1 = new Rule
            {
                SourceTopic = "IOTZOO/TIME/EVERY_5_SECONDS",
                TargetTopic = "esp32/E4:65:B8:79:27:E0/neo/0/setPixelColor",
                TargetPayload = "{'r': " + $"{FunctionNames.ReadFromMemory}('COLOR_RED_COMPONENT')" +
                                ", 'g': 125, 'b': 125, 'index': " + $"{FunctionNames.ReadFromMemory}('VARIABLE1')" +
                                ", 'length': 1, 'brightness': 15}"
            };

            Rule rule2 = new Rule
            {
                SourceTopic = "INC_VARIABLE1",
                TargetTopic = "VARIABLE1",
                TargetPayload = $"{FunctionNames.Calc}({FunctionNames.ReadFromMemory}('VARIABLE1') + 1);"
            };

            TopicEntry topicEntry1 = new TopicEntry
            {
                Topic = "INC_VARIABLE1"
            };
            var targetPayload1 = PrepareTargetPayload.Execute(rule2, topicEntry1);

            var topicEntry2 = new TopicEntry
            {
                Topic = "IOTZOO/TIME/EVERY_5_SECONDS",
                Payload = string.Empty
            };

            var targetPayload3 = await PrepareTargetPayload.Execute(rule2, topicEntry2);

            Assert.NotNull(targetPayload3);
            Assert.NotEmpty(targetPayload3);
            Assert.Equal("34", targetPayload3);
        }

        [Fact]
        public void DataFromJsonTest1()
        {
            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "colors", Payload = """[{"red":{"r":255,"g":0,"b":0}},{"green":{"r":0,"g":255,"b":0}}]""" });

            Rule rule0 = new Rule
            {
                SourceTopic = "IOTZOO/TEST",
                TargetTopic = "esp32/E4:65:B8:79:27:E0/neo/0/setPixelColor",
                TargetPayload = "ReadFromMemory(colors)"
            };

            var topicEntry0 = new TopicEntry();
            var targetPayload0 = PrepareTargetPayload.Execute(rule0, topicEntry0);

            Rule rule1 = new Rule
            {
                SourceTopic = "IOTZOO/TEST",
                TargetTopic = "esp32/E4:65:B8:79:27:E0/neo/0/setPixelColor",
                TargetPayload =
                    """{"r": 0, "g": "$colors["green"].g", "b": 125, "index": 0, "length": 10, "brightness": 15}"""
            };

            var topicEntry1 = new TopicEntry();
            var targetPayload1 = PrepareTargetPayload.Execute(rule1, topicEntry1);

            Rule rule2 = new Rule
            {
                SourceTopic = "IOTZOO/TEST",
                TargetTopic = "esp32/E4:65:B8:79:27:E0/neo/0/setPixelColor",
                TargetPayload =
                    """{"r": $colors[0].r, "g": "$colors[0].g", "b": 125, "index": 0, "length": 10, "brightness": 15}"""
            };

            var topicEntry2 = new TopicEntry();
            var targetPayload2 = PrepareTargetPayload.Execute(rule2, topicEntry2);
        }

        [Fact]
        public async Task MqttClientTest1Async()
        {
            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(new TopicEntry
            { Topic = "colors", Payload = """[{"red":"#F51201", "green":"#17F53C"}]""" });

            Rule rule0 = new Rule
            {
                SourceTopic = "IOTZOO/TEST",
                TargetPayload = "esp32/E4:65:B8:79:27:E0/neo/0/setPixelColor"
            };
            rule0.TargetPayload = $"{FunctionNames.ReadFromMemory}(colors)";
            await RulesService.Save(rule0);

            var messageHandlingResult =
                await IoTZooMqttClient.HandleMqttMessage(new TopicEntry { Topic = "IOTZOO/TEST" });
        }

        [Fact]
        public async Task CSharpScriptEvaluateTest1()
        {
            var modifyByScript = new ScriptDataIn { IndexLed = 1, Temperature = 2.3 };
            var result1 =
                await CSharpScript.EvaluateAsync<string>("IndexLed = IndexLed + 1; Temperature = 33.344;return \"super\";", null, modifyByScript, cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(result1 == "super");
            Assert.True(modifyByScript.Temperature == 33.344);

            ScriptOptions scriptOptions = ScriptOptions.Default.AddReferences(typeof(JsonSerializer).Assembly)
                .AddReferences(typeof(Color).Assembly).AddImports("System.Text.Json").AddImports("MudBlazor");

            string baseCode = File.ReadAllText("CSharpScriptTemperatureToColor.csx");

            double t = -12.34;
            string sourceCode = baseCode +
                                $"{Environment.NewLine}double temperature = {t.ToString(CultureInfo.InvariantCulture)};{Environment.NewLine}return new HolgerDemo().TemperatureToColor(temperature);";

            var result1a = await CSharpScript.EvaluateAsync(sourceCode, scriptOptions, cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(result1a.ToString() == "#FFFFFF");

            t = 12.34;
            sourceCode = baseCode +
                         $"{Environment.NewLine}double temperature = {t.ToString(CultureInfo.InvariantCulture)};{Environment.NewLine}return TemperatureToColor(temperature);";

            var result2 = await CSharpScript.EvaluateAsync(sourceCode, scriptOptions, cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result2.ToString() == "#FF9800");

        }

        [Fact]
        public async Task CSharpScriptEvaluateTest2()
        {
            var baseCode = File.ReadAllText("CSharpScriptDeserializeJson.csx");

            var heatMap = new { Temperature = 12.5, LedIndex = 30 };
            string json = JsonSerializer.Serialize(heatMap);

            // Do this into the script... and return Person.Name
            //JsonElement element = JsonSerializer.Deserialize<JsonElement>(json);

            //double temperature = element.GetProperty("Temperature").GetDouble();
            //int ledIndex = element.GetProperty("LedIndex").GetInt32();


            string sourceCode = baseCode + $"{Environment.NewLine}return GetName(\"\"\"{json}\"\"\");";

            var scriptOptions = ScriptOptions.Default.AddReferences(typeof(JsonSerializer).Assembly)
                .AddImports("System.Text.Json");

            var result1 = await CSharpScript.EvaluateAsync<string>(sourceCode, scriptOptions, cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(result1 == "#111111");
        }

        [Fact]
        public async Task CSharpScriptEvaluateTest10()
        {
            var modifyByScript = new ScriptDataIn { IndexLed = 1, Temperature = 2.3 };

            var result1 =
                await CSharpScript.EvaluateAsync<string>("IndexLed = IndexLed + 1; Temperature = 33.344;return \"super\";", null, modifyByScript, cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(result1 == "super");
            Assert.True(modifyByScript.Temperature == 33.344);

            while (modifyByScript.IndexLed < 5)
            {
                // This is to slow! -> Comile the script!
                await CSharpScript.EvaluateAsync<string>("IndexLed = IndexLed + 1; Temperature = 33.344;return \"super\";", null, modifyByScript, cancellationToken: TestContext.Current.CancellationToken);
            }
        }

        /// <summary>
        /// This is the fastest!
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CSharpScriptDataExchangeTest1()
        {
            var modifyByScript = new ScriptDataIn { IndexLed = 1, Temperature = 2.3 };

            var script = CSharpScript.Create<string>(
                "if (IndexLed < 70) {IndexLed = IndexLed + 1;} else {IndexLed = 0;}; Temperature = 33.344;return \"super\";",
                null, typeof(ScriptDataIn));

            //ScriptRunner<string> runner = script.CreateDelegate();

            while (modifyByScript.IndexLed != 0)
            {
                await script.RunAsync(modifyByScript, cancellationToken: TestContext.Current.CancellationToken);
            }
        }

        /// <summary>
        /// Dataexchange with CSharpScript (in and out).
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CSharpScriptDataExchangeTest2()
        {
            var dataIn = new ScriptDataIn { Temperature = 2.3 };
            // Hint: anonymous type is immutable! So we cannot modify IndexLed and Temperature inside the script.

            var scriptDataIn = new ScriptDataIn { Data = JsonSerializer.Serialize(dataIn) };

            var scriptOptions = ScriptOptions.Default.AddReferences(typeof(JsonSerializer).Assembly)
                .AddReferences(typeof(Color).Assembly).AddImports("System.Text.Json").AddImports("MudBlazor");

            string sourceCode = File.ReadAllText("CSharpScriptTemperatureToColor.csx");
            sourceCode += "return TemperatureToColor(temperature: Temperature);";
            var script = CSharpScript.Create<string>(sourceCode,
                scriptOptions,
                typeof(ScriptDataIn));

            ScriptRunner<string> runner = script.CreateDelegate(TestContext.Current.CancellationToken);

            var scriptState = await script.RunAsync(scriptDataIn, cancellationToken: TestContext.Current.CancellationToken);
            string dataOut = scriptState.ReturnValue;

            Assert.True(dataOut == "#4CAF50");
        }

        /// <summary>
        /// Dataexchange with CSharpScript (in and out).
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CSharpScriptDataExchangeTest3()
        {
            var dataIn = new { IndexLed = 0, Temperature = 42.5, CountOfLeds = 64 };
            // Hint: anonymous type is immutable! So we cannot modify IndexLed and Temperature inside the script.

            var scriptDataIn = new ScriptDataIn { Data = JsonSerializer.Serialize(dataIn) };

            var scriptOptions = ScriptOptions.Default.AddReferences(typeof(JsonSerializer).Assembly)
                .AddReferences(typeof(Color).Assembly).AddImports("System.Text.Json").AddImports("MudBlazor");

            string sourceCode = await File.ReadAllTextAsync("CSharpScriptHeatmap.csx", TestContext.Current.CancellationToken);
            var script = CSharpScript.Create<string>(sourceCode,
                scriptOptions,
                typeof(ScriptDataIn));

            // ScriptRunner<string> runner = script.CreateDelegate();

            for (int i = 0; i < dataIn.CountOfLeds - 1; i++) // < 300 ms for 5000 iterations!
            {
                var scriptState = await script.RunAsync(scriptDataIn, cancellationToken: TestContext.Current.CancellationToken);
                string dataOut = scriptState.ReturnValue;

                JsonElement element = JsonSerializer.Deserialize<JsonElement>(dataOut);
                int indexLed = element.GetProperty("IndexLed").GetInt32();
                string? color = element.GetProperty("Color").GetString();

                string? targetPayload = element.GetProperty("TargetPayload").GetString();

                Assert.NotNull(color);
                Assert.NotNull(targetPayload);

                Assert.True(color == Colors.DeepPurple.Default.ToString());

                dataIn = new
                {
                    IndexLed = indexLed, // incremented
                    Temperature = 42.5,
                    CountOfLeds = 64
                };
                scriptDataIn = new ScriptDataIn { Data = JsonSerializer.Serialize(dataIn) };

                Assert.True(indexLed == i + 1);
            }
        }

        /// <summary>
        /// This is the fastest!
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CSharpScriptEvaluateTest12()
        {
            var modifyByScript = new ScriptDataIn { IndexLed = 1, Temperature = -2.3 };

            var script = CSharpScript.Create<string>(
                "if (IndexLed < 70) {IndexLed = IndexLed + 1;} else {IndexLed = 0;}; if (Temperature < 0) return \"cold\"; else return \"super\";",
                null, typeof(ScriptDataIn));

            ScriptRunner<string> runner = script.CreateDelegate(TestContext.Current.CancellationToken);

            while (modifyByScript.IndexLed != 0)
            {
                var scriptState = await script.RunAsync(modifyByScript, cancellationToken: TestContext.Current.CancellationToken);
                var color = scriptState.ReturnValue;
            }
        }

        [Fact]
        public async Task CSharpScriptEvaluateTest12A()
        {
            string sourceCode = @"
            public class MyClass
            {
                public int Counter { get; set; }
                public string Message { get; set; }

                public void Update()
                {
                    Counter += 10;
                    Message += "" from CSharpScript!"";
                }
            }

            var obj = new MyClass { Counter = 5, Message = ""Hello, World!"" };
            obj.Update();
            return obj;
        ";

            // Execute the script and get the result (an instance of MyClass)
            var result = await CSharpScript.EvaluateAsync(sourceCode, cancellationToken: TestContext.Current.CancellationToken);

            // Use reflection to access the properties (since the type is dynamic)
            object? counter = result.GetType().GetProperty("Counter")!.GetValue(result);
            object? message = result.GetType().GetProperty("Message")!.GetValue(result);

            testOutHelper.WriteLine($"Counter: {counter}"); // Output: Counter: 15
            testOutHelper.WriteLine($"Message: {message}"); // Output: Message: Hello, World! from CSharpScript!

            string sourceCode2 = @"
            public class MyClass
            {
                public int Counter { get; set; }
                public string Message { get; set; }

                public void Update()
                {
                    Counter += 1;
                    Message += "" from CSharpScript!"";
                }
            }

            var obj = new MyClass { Counter = 5, Message = ""Hello, World!"" };
            obj.Update();
            return obj.Counter;
        ";

            var script = CSharpScript.Create<int>(sourceCode2);
            script.Compile(TestContext.Current.CancellationToken);

            for (int a = 0; a < 10; a++)
            {
                var state = await script.RunAsync(cancellationToken: TestContext.Current.CancellationToken);
                int value = state.ReturnValue;
            }
        }

        /// <summary>
        /// This is the fastest!
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CSharpScriptEvaluateTest12B()
        {
            var modifyByScript = new ScriptDataIn { IndexLed = 1, Temperature = -2.3 };
            // We have to find a generic way to modify Properties into the Script! Input: Temperature, Output: IndexLed and Color
            // If this is not possible, we must transfer a complete json structure every call.
            var script = CSharpScript.Create<string>(
                "if (IndexLed < 70) {IndexLed = IndexLed + 1;} else {IndexLed = 0;}; if (Temperature < 0) return \"cold\"; else return \"super\";",
                null, typeof(ScriptDataIn));

            // ScriptRunner<string> runner = script.CreateDelegate();

            while (modifyByScript.IndexLed != 0)
            {
                var scriptState = await script.RunAsync(modifyByScript, cancellationToken: TestContext.Current.CancellationToken);
                var color = scriptState.ReturnValue;
            }
        }

        [Fact]
        public async Task CSharpScriptEvaluateTest13()
        {
            var modifyByScript = new ScriptDataIn { IndexLed = 1, Temperature = 2.3 };

            var script = CSharpScript.Create<string>("IndexLed = IndexLed + 1; Temperature = 33.344;return \"super\";",
                null, typeof(ScriptDataIn));

            script.Compile(TestContext.Current.CancellationToken);

            while (modifyByScript.IndexLed < 64)
            {
                await script.RunAsync(modifyByScript, cancellationToken: TestContext.Current.CancellationToken);
            }
        }

        [Fact]
        public void CSharpScriptCompileTest1()
        {
            string baseCode = File.ReadAllText("CSharpScriptTemperatureToColor.csx");

            double t = -12.34;
            string sourceCode = baseCode +
                                $"{Environment.NewLine}double temperature = {t.ToString(CultureInfo.InvariantCulture)};{Environment.NewLine}return new HolgerDemo().TemperatureToColor(temperature);";

            var options = ScriptOptions.Default.AddReferences(typeof(JsonSerializer).Assembly)
                .AddReferences(typeof(Color).Assembly).AddImports("System.Text.Json").AddImports("MudBlazor");

            var script = CSharpScript.Create<string>(sourceCode, options);
            script.Compile(TestContext.Current.CancellationToken);
            var compilation = script.GetCompilation();
            using var dllStream = new MemoryStream();
            using var pdbStream = new MemoryStream();
            var emitResult = compilation.Emit(dllStream, pdbStream, cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(emitResult.Success);

            File.WriteAllBytes("IoTZooScriptingTest2.dll", dllStream.ToArray());
            File.WriteAllBytes("IoTZooScriptingTest2.pdb", pdbStream.ToArray());
        }

        [Fact]
        public async Task CSharpScriptRunTest1()
        {
            string sourceCode = File.ReadAllText("CSharpScriptTemperatureToColor.csx");

            var scriptOptions = ScriptOptions.Default.AddReferences(typeof(JsonSerializer).Assembly)
                .AddReferences(typeof(Color).Assembly).AddImports("System.Text.Json").AddImports("MudBlazor");
            Microsoft.CodeAnalysis.Scripting.Script compiledScript = CSharpScript.Create(sourceCode, scriptOptions);

            var result = await compiledScript.RunAsync(cancellationToken: TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task ExpressionParserTest1()
        {
            var topic1 = new TopicEntry { Topic = "heatmap/index", Payload = "14" };
            var topic2 = new TopicEntry { Topic = "heatmap/index2", Payload = "16" };
            var topic3 = new TopicEntry { Topic = "heatmap/index3", Payload = "18" };

            DataTransferService.ReceivedTopicsQueue.Clear();
            DataTransferService.ReceivedTopicsQueue.Enqueue(topic1);
            DataTransferService.ReceivedTopicsQueue.Enqueue(topic2);
            DataTransferService.ReceivedTopicsQueue.Enqueue(topic3);

            string expression =
                $"{FunctionNames.ReadFromMemory}('heatmap/index') + {FunctionNames.ReadFromMemory}('heatmap/index2') - {FunctionNames.ReadFromMemory}('heatmap/index3');";
            string parsedExpression = await ExpressionParser.ResolveExpression(expression);
            var result = CalculationService.Calculate(parsedExpression);
            Assert.True(Convert.ToInt32(result) == 12);
        }
    }
}