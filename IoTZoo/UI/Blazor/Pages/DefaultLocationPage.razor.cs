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
// Page for managing the location/geo-coordinate to calculate sunrise and sunset.
// --------------------------------------------------------------------------------------------------------------------

using Domain.Interfaces.Crud;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SunriseAndSunset;
using System.Reflection;

namespace IotZoo.Pages;

public class DefaultLocationPageBase : PageBase
{
   [Inject]
   public required ISettingsCrudService SettingsService
   {
      get;
      set;
   }

   protected double Latitude
   {
      get => latitude;
      set
      {
         latitude = value;
         Calculate();
      }
   }

   protected double Longitude
   {
      get => longitude;
      set
      {
         longitude = value;
         Calculate();
      }
   }

   protected List<double> dayLengthInHoursList = new();

   protected ChartOptions ChartOptions = new();

   public string[] XAxisLabels = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];


   protected string Sunrise
   {
      get;
      set;
   } = null!;

   protected string Sunset
   {
      get;
      set;
   } = null!;

   protected double DayLengthHours { get; set; }


   public List<ChartSeries> Series = new List<ChartSeries>();
   private double latitude;
   private double longitude;

   protected override async Task OnInitializedAsync()
   {
      await base.OnInitializedAsync();
      DataTransferService.CurrentScreen = ScreenMode.SetLocation;
      ChartOptions.LineStrokeWidth = 2;
      ChartOptions.YAxisTicks = 24;
      ChartOptions.XAxisLines = true;
      //ChartOptions.InterpolationOption = InterpolationOption.NaturalSpline;
      try
      {
         Latitude = await SettingsService.GetSettingDouble(SettingCategory.Location, SettingKey.Latitude);
         Longitude = await SettingsService.GetSettingDouble(SettingCategory.Location, SettingKey.Longitude);

         CalculateSunriseAndSunsetForTheYear(Latitude, Longitude);

         Series.Add(new ChartSeries()
         {
            Name = "Hours with daylight",
            Data = dayLengthInHoursList.ToArray()
         });

         CalculateSunriseAndSunset(Latitude, Longitude);
         base.OnInitialized();
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   protected async Task Save()
   {
      if (await SettingsService.Update(SettingCategory.Location,
                                 SettingKey.Latitude,
                                 Latitude) == 1 &&
          await SettingsService.Update(SettingCategory.Location,
                                 SettingKey.Longitude,
                                 Longitude) == 1)
      {
         Snackbar.Add("Location Saved!", Severity.Info);
      }
      else
      {
         Snackbar.Add("NOT Saved!", Severity.Error);
      }
      Calculate();
   }

   private void Calculate()
   {
      CalculateSunriseAndSunsetForTheYear(Latitude,
                                          Longitude);
      CalculateSunriseAndSunset(Latitude,
                                Longitude);
      InvokeAsync(StateHasChanged);
   }

   protected void CalculateSunriseAndSunsetForTheYear(double latitude, double longitude)
   {
      try
      {
         dayLengthInHoursList.Clear();
         DateTime dateTime = new DateTime(DateTime.Now.Year,
                                                          1,
                                                          15);
         for (int i = 0; i < 13; i++)
         {
            SunriseCalc homeLocationSunrise = new SunriseCalc(latitude,
                                                              longitude)
            {
               Day = dateTime.Add(TimeSpan.FromDays(i * (365.0 / 13)))
            };
            double totalHours = homeLocationSunrise.GetDayLength().TotalHours;
            if (totalHours < 0)
            {
               totalHours = 0;
            }

            if (totalHours > 24)
            {
               totalHours = 24;
            }
            dayLengthInHoursList.Add(totalHours);
         }
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   protected void CalculateSunriseAndSunset(double latitude, double longitude)
   {
      try
      {
         CalculateSunriseAndSunset(latitude,
                                   longitude,
                                   DateTime.Now);
         Series.First().Data = dayLengthInHoursList.ToArray();
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   protected void CalculateSunriseAndSunset(double latitude,
                                            double longitude,
                                            DateTime day)
   {
      try
      {
         SunriseCalc homeLocationSunrise = new SunriseCalc(latitude,
                                                           longitude)
         {
            Day = day
         };

         // Get today's sunrise and sunset in UTC.
         homeLocationSunrise.GetRiseAndSet(out DateTime todaysSunriseUtc,
                                           out DateTime todaysSunsetUtc);

         Sunrise = todaysSunriseUtc.ToLocalTime().ToLongTimeString();
         Sunset = todaysSunsetUtc.ToLocalTime().ToLongTimeString();
         DayLengthHours = homeLocationSunrise.GetDayLength().TotalHours;

         InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }
}
