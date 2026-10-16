// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Globalization;

namespace IotZoo.Dialogs;

public class TopicChartDialogBase : ComponentBase
{
   [CascadingParameter]
   protected IMudDialogInstance MudDialog { get; set; } = null!;

   [Parameter]
   public string Topic { get; set; } = string.Empty;

   [Parameter]
   public List<TopicHistory> Entries { get; set; } = new();

   protected List<ChartSeries<double>> Series { get; private set; } = new();

   protected string[] ChartLabels { get; private set; } = Array.Empty<string>();

   private DateTime? _startDate;

   private DateTime? _endDate;

   private TimeSpan? _startTime;

   private TimeSpan? _endTime;

   protected DateTime? StartDate
   {
      get => _startDate;
      set
      {
         _startDate = value;
         BuildChart();
      }
   }

   protected DateTime? EndDate
   {
      get => _endDate;
      set
      {
         _endDate = value;
         BuildChart();
      }
   }

   protected TimeSpan? StartTime
   {
      get => _startTime;
      set
      {
         _startTime = value;
         BuildChart();
      }
   }

   protected TimeSpan? EndTime
   {
      get => _endTime;
      set
      {
         _endTime = value;
         BuildChart();
      }
   }

   protected override void OnParametersSet()
   {
      if (Entries.Count > 0)
      {
         var last = Entries.Max(e => e.DateReceived);
         _endDate ??= last.Date;
         _endTime ??= last.TimeOfDay;
         var startDefault = (_endDate.Value.Date + (_endTime ?? TimeSpan.Zero)).AddDays(-1);
         _startDate ??= startDefault.Date;
         _startTime ??= startDefault.TimeOfDay;
      }
      BuildChart();
   }

   private void BuildChart()
   {
      DateTime? start = StartDate?.Date + (StartTime ?? TimeSpan.Zero);
      DateTime? end = EndDate?.Date + (EndTime ?? TimeSpan.FromDays(1) - TimeSpan.FromTicks(1));

      var numericEntries = Entries
         .Where(e => double.TryParse(e.Payload, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
         .Where(e => start is null || e.DateReceived >= start)
         .Where(e => end is null || e.DateReceived <= end)
         .OrderBy(e => e.DateReceived)
         .ToList();

      ChartLabels = numericEntries.Select(e => e.DateReceived.ToString("HH:mm:ss")).ToArray();
      Series = new List<ChartSeries<double>>();
      if (numericEntries.Count > 0)
      {
         Series.Add(new ChartSeries<double>
         {
            Name = Topic,
            Data = numericEntries.Select(e => double.Parse(e.Payload!, CultureInfo.InvariantCulture)).ToArray()
         });
      }
   }

   protected void ShiftDays(int days)
   {
      _startDate = StartDate?.AddDays(days);
      _endDate = EndDate?.AddDays(days);
      BuildChart();
   }

   protected void ShiftTime(TimeSpan offset)
   {
      if (StartDate is not null)
      {
         var start = StartDate.Value.Date + (StartTime ?? TimeSpan.Zero) + offset;
         _startDate = start.Date;
         _startTime = start.TimeOfDay;
      }
      if (EndDate is not null)
      {
         var end = EndDate.Value.Date + (EndTime ?? TimeSpan.Zero) + offset;
         _endDate = end.Date;
         _endTime = end.TimeOfDay;
      }
      BuildChart();
   }

   protected void Close() => MudDialog.Close();
   }
