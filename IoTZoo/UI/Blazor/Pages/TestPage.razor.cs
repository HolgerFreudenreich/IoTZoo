// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers without programming knowledge.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Interfaces.RuleEngine;
using Domain.Pocos;
using Domain.Services.RuleEngine;
using Infrastructure;
using Microsoft.AspNetCore.Components;

namespace IotZoo.Pages
{
   public class TestPageBase : PageBase
   {
      [Inject]
      protected IExpressionParser ExpressionParser { get; set; } = null!;

      [Inject]
      protected ICalculationService CalculationService { get; set; } = null!;

      protected string Expression { get; set; } = string.Empty;
      protected string Payload { get; set; } = string.Empty;

      protected override void OnInitialized()
      {
         DataTransferService.CurrentScreen = ScreenMode.TestPage;
         base.OnInitialized();
      }

      protected async Task Execute()
      {
         //string expression = "::TemperatureToColor(45.5)";
         //ExpressionEvaluationResult expressionEvaluationResult = await ExpressionEvaluationService.EvaluateExpressionAsync(expression, string.Empty);
         //if (expressionEvaluationResult != null)
         //{
         //   Snackbar.Add(expressionEvaluationResult.Result.ToString());
         //}
         //else
         //{
         //   Snackbar.Add("undefined", Severity.Warning);
         //}

         try
         {
            var topic1 = new TopicEntry { Topic = "heatmap/index", Payload = "14" };
            var topic2 = new TopicEntry { Topic = "heatmap/index2", Payload = "16" };

            //if (!DataTransferService.ReceivedTopicsQueue.Contains(topic))
            //{
            //   this.DataTransferService.ReceivedTopicsQueue.Clear();
            //   this.DataTransferService.ReceivedTopicsQueue.Enqueue(topic1);
            //   this.DataTransferService.ReceivedTopicsQueue.Enqueue(topic2);
            //}

            DateTime startDateTime = DateTime.UtcNow;

            //string expression = $"{FunctionNames.ReadFromMemory}('heatmap/index') + {FunctionNames.ReadFromMemory}('heatmap/index2') - ({FunctionNames.ReadFromMemory}('heatmap/index2'))";
            //string parsedExpression = await ExpressionParser.Parse(expression);
            //var result = CalculationService.Calculate(parsedExpression);
            //var diff = DateTime.UtcNow - startDateTime;
            //if (result != null)
            //{
            //   Snackbar.Add($"{expression}->{parsedExpression}->{result.ToString()} in {diff.TotalMilliseconds}ms");
            //}
            //else
            //{
            //   Snackbar.Add("undefined", Severity.Warning);
            //}
            //expression = $"{FunctionNames.Script}GetCalendarWeek() + {FunctionNames.Script}GetCalendarWeek() + {FunctionNames.ReadFromMemory}('heatmap/index')";
            //parsedExpression = await ExpressionParser.Parse(expression);

            //Expression = "::GetJoke()";
            var parsedExpression = await ExpressionParser.ResolveExpression(Expression);
            var result = CalculationService.Calculate(parsedExpression);
            var diff = DateTime.UtcNow - startDateTime;
            Snackbar.Add($"Expression: {Expression} → Parsed Expression: {parsedExpression} Result: {result} → Duration: {diff.TotalMilliseconds.ToString("N1")} ms");
         }
         catch (Exception ex)
         {
            Snackbar.Add(ex.GetBaseException().Message, MudBlazor.Severity.Error);
         }
      }

      protected void TestButtonClicked()
      {
         ip = Tools.GetLocalIpAddress();

         TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
   
         // Get the adjustment rules for the current year
         var adjustmentRule = localTimeZone.GetAdjustmentRules().FirstOrDefault();
         if (adjustmentRule != null)
         {
            TimeZoneInfo.TransitionTime startTransitionTime = adjustmentRule.DaylightTransitionStart;
            string summerTimeStart = $"Summertime Start: {startTransitionTime}";
            
            TimeZoneInfo.TransitionTime endTransitionTime = adjustmentRule.DaylightTransitionEnd;
         }

         ////foreach (var rule in adjustmentRule)
         ////{
         ////   // Display the daylight saving time changes for the specified year
         ////   Console.WriteLine($"Adjustment Rule for Year {DateTime.Today.Year}:");
         ////   Console.WriteLine($"  Start: {rule.DaylightTransitionStart}");
         ////   Console.WriteLine($"  End: {rule.DaylightTransitionEnd}");
         ////   Console.WriteLine($"  Delta: {rule.DaylightDelta}");
         ////}
      }

      protected string ip = string.Empty;
   }
}
