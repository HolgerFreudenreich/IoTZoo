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

public class CronJob : BasePoco
{
   public CronJob()
   {
   }

   public CronJob(CronExpression cronExpression, string topic, bool enabled, bool editAllowed = false)
   {
      SecondExpression = cronExpression.SecondExpression;
      MinuteExpression = cronExpression.MinuteExpression;
      HourExpression = cronExpression.HourExpression;
      DayOfMonthExpression = cronExpression.DayOfMonthExpression;
      MonthOfYearExpression = cronExpression.MonthOfYearExpression;
      DayOfWeekExpression = cronExpression.DayOfWeekExpression;
      Topic = topic;
      Enabled = enabled;
      EditAllowed = editAllowed;
   }

   public bool Enabled { get; set; } = true;

   public int CronId { get; set; }

   public string SecondExpression { get; set; } = string.Empty;

   public string MinuteExpression { get; set; } = string.Empty;

   public string HourExpression { get; set; } = string.Empty;

   public string DayOfMonthExpression { get; set; } = string.Empty;

   public string MonthOfYearExpression { get; set; } = string.Empty;

   public string DayOfWeekExpression { get; set; } = string.Empty;

   public string Topic { get; set; } = string.Empty;

   public string NamespaceName { get; set; } = string.Empty;

   public string ProjectName { get; set; } = string.Empty;

   public bool EditAllowed { get; set; }

   public override string ToString()
   {
      return $"{SecondExpression} {MinuteExpression} {HourExpression} {DayOfMonthExpression} {MonthOfYearExpression} {DayOfWeekExpression}";
   }
}
