-- This script migrates the scripting database to version 2. It adds the example scripts GetCurrentWeatherForecast and GetCheapestDiesel.

INSERT INTO "script" (script_name, description, source_code, script_language_id) VALUES ('GetCurrentWeatherForecast','Gets the current temperature from Open-Meteo for latitude and longitude','using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;

public class CurrentWeatherResponse
{
  public CurrentWeather current_weather { get; set; }
}

public class CurrentWeather
{
  public double temperature { get; set; }
  public double windspeed { get; set; }
}

string GetCurrentWeatherForecast(double latitude, double longitude)
{
  string url = string.Format(CultureInfo.InvariantCulture,
                             "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&current_weather=true",
                             latitude,
                             longitude);

  using var httpClient = new HttpClient();
  string responseMessageContent = httpClient.GetStringAsync(url).Result;

  var options = new JsonSerializerOptions
  {
    PropertyNameCaseInsensitive = true
  };

  var data = JsonSerializer.Deserialize<CurrentWeatherResponse>(responseMessageContent, options);
  if (null != data?.current_weather)
  {
    return string.Format(CultureInfo.InvariantCulture,
                         "{0} °C, wind {1} km/h",
                         data.current_weather.temperature,
                         data.current_weather.windspeed);
  }
  return "no weather data";
}',0);

---------------------------------------------------------------------------------------------------
-- Diesel price script is based on the Tankerkoenig API. The API key can be obtained for free at
-- https://creativecommons.tankerkoenig.de/
---------------------------------------------------------------------------------------------------
INSERT INTO "script" (script_name, description, source_code, script_language_id) VALUES ('GetCheapestDiesel','Gets the cheapest open diesel station within the given radius in km from Tankerkoenig. Parameters: apiKey, latitude, longitude, radius.',
'using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

public class DieselStationsResponse
{
  public bool ok { get; set; }
  public DieselStation[] stations { get; set; }
}

public class DieselStation
{
  public string Name { get; set; }
  public string Brand { get; set; }
  public double? Price { get; set; }
  public bool IsOpen { get; set; }
}

string GetCheapestDiesel(string apiKey, double latitude, double longitude, double radius)
{
  string url = string.Format(CultureInfo.InvariantCulture,
                             "https://creativecommons.tankerkoenig.de/json/list.php?lat={0}&lng={1}&rad={2}&sort=price&type=diesel&apikey={3}",
                             latitude,
                             longitude,
                             radius,
                             apiKey);

  using var httpClient = new HttpClient();
  string responseMessageContent = httpClient.GetStringAsync(url).Result;

  var options = new JsonSerializerOptions
  {
    PropertyNameCaseInsensitive = true
  };

  var data = JsonSerializer.Deserialize<DieselStationsResponse>(responseMessageContent, options);
  if (null == data || !data.ok || null == data.stations)
  {
    return "no diesel data";
  }

  var station = data.stations.Where(s => s.Price.HasValue && s.IsOpen).OrderBy(s => s.Price).FirstOrDefault();
  if (null == station)
  {
    return "no open station with diesel price nearby";
  }
  return string.Format(CultureInfo.InvariantCulture,
                       "{0} {1}: {2:F3} EUR/l",
                       station.Brand,
                       station.Name,
                       station.Price);
}',0);

update db_version set version_number = 2;