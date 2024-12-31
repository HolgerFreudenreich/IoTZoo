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

using Dapper;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using NCrontab;

namespace IotZoo.Components;

public partial class CronExpression : ComponentBase
{
   public Domain.Pocos.CronExpression Cron { get; } = new();
   
   public List<DateTime> Occurences { get; set; } = new();

   [Inject]
   ISnackbar Snackbar { get; set; } = null!;

   protected void Parse()
   {
      try
      {
         CrontabSchedule schedule = CrontabSchedule.Parse(Cron.ToString(), new CrontabSchedule.ParseOptions { IncludingSeconds = true });

         Occurences = schedule.GetNextOccurrences(DateTime.UtcNow, DateTime.UtcNow.AddDays(100)).Take(25).AsList();
      }
      catch (Exception ex)
      {
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }
}
