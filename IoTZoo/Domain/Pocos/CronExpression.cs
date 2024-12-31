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

namespace Domain.Pocos;

public class CronExpression
{
   public string SecondExpression { get; set; } = "*";
   public string MinuteExpression { get; set; } = "*";
   public string HourExpression { get; set; } = "*";
   public string DayOfMonthExpression { get; set; } = "*";
   public string MonthOfYearExpression { get; set; } = "*";
   public string DayOfWeekExpression { get; set; } = "*";

   internal static CronExpression FromString(string expression)
   {
      var segment = expression.Split(' ');
      if (segment.Length != 6)
      {
         throw new ArgumentException("expression must contain 6 segments.");
      }
      return new CronExpression { SecondExpression = segment[0],
                                  MinuteExpression = segment[1],
                                  HourExpression = segment[2],
                                  DayOfMonthExpression = segment[3],
                                  MonthOfYearExpression = segment[4],
                                  DayOfWeekExpression = segment[5] };
   }

   public override string ToString()
   {
      return $"{SecondExpression} {MinuteExpression} {HourExpression} {DayOfMonthExpression} {MonthOfYearExpression} {DayOfWeekExpression}";
   }
}