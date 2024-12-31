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
// ---------------------------------------------------------------------------------------------------------------------

using Domain.Pocos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SunriseAndSunset;

namespace IotZoo.Controller;

[ApiController]
[Route("api/[controller]")]
public class TimeController : ControllerBase
{
  public TimeController(ILogger<TimeController> logger, IOptions<AppSettings> options)
  {
    this.Logger      = logger;
    this.AppSettings = options.Value;

    SunriseCalc sunriseCalc = new SunriseCalc(AppSettings.Latitude, AppSettings.Longitude);

    sunriseCalc.GetRiseAndSet(out DateTime sunriseDateTime, out DateTime sunsetDateTime);

    SunriseDateTimeUtc = sunriseDateTime;
    SunsetDateTimeUtc  = sunsetDateTime;
  }

  protected AppSettings AppSettings
  {
    get;
    set;
  }

  protected ILogger<TimeController> Logger
  {
    get;
    set;
  }

  protected DateTime SunriseDateTimeUtc
  {
    get;
    set;
  }

  protected DateTime SunsetDateTimeUtc
  {
    get;
    set;
  }

  [HttpGet]
  public DateTime Get()
  {
    return DateTime.Now;
  }

  [HttpGet("Sunset")]
  public DateTime GetSunset()
  {
    return SunsetDateTimeUtc.ToLocalTime();
  }

  [HttpGet("Sunrise")]
  public DateTime GetSunrise()
  {
    return SunriseDateTimeUtc.ToLocalTime();
  }
}