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

public enum SettingCategory
{
  MqttClientSettings = 0,
  MqttBrokerSettings = 1,
  Location = 2,
  General = 3,
  MqttPublishTestMessage = 4,
  PhilipsHue = 5,
  UiSettings = 6,
}

public enum SettingKey
{
  Namespace = 0,
  Latitude = 1,
  Longitude = 2,
  MqttBrokerSettings = 3,
  DateAndTimeFormat = 4,
  MqttData = 5,
  Ip = 6,
  AppKey = 7,
  IsDarkMode = 8,
}

public class Setting : BasePoco
{
  public int SettingId
  {
    get;
    set;
  }

  public string Category
  {
    get;
    init;
  } = null!;

  public string SettingKey
  {
    get;
    init;
  } = null!;

  public string SettingValue
  {
    get;
    init;
  } = null!;

  // Note: this is important too!
  public override int GetHashCode() => SettingId.GetHashCode();

  // to display correctly in MudSelect.
  // Alternative: ToStringFunc="@converter"
  //    Func<Planum, string> converter = p => p?.PlanumName;
  public override string ToString() => SettingValue;


  /// <summary>
  ///  // return Less than zero if this object 
  /// is less than the object specified by the CompareTo method.

  /// return Zero if this object is equal to the object 
  /// specified by the CompareTo method.

  /// return Greater than zero if this object is greater than 
  /// the object specified by the CompareTo method.
  /// </summary>
  /// <param name="obj"></param>
  /// <returns></returns>
  public int CompareTo(object? obj)
  {
    var other = obj as Setting;
    return string.Compare(other?.SettingValue, this.SettingValue, StringComparison.Ordinal);
  }

  public override bool Equals(object? o)
  {
    var other = o as Setting;
    return other?.SettingId == SettingId;
  }
}