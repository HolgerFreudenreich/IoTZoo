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

namespace Domain.Pocos;

public enum DataLinkType
{
    Unknown = -1,
    Mqtt = 0,
    Internal = 1
}

public class PropertyValue
{
    public PropertyValue()
    {
    }

    public PropertyValue(string name, string value)
    {
        Name = name; Value = value;
    }

    public string? Name
    {
        get;
        set;
    }

    public string? Value { get; set; }

    public bool IsReadOnly { get; set; } = false;
}

/// <summary>
/// Represents a connected device, e.g. a sensor.
/// </summary>
public class ConnectedDevice : BasePoco
{
    public bool IsEnabled { get; set; }

    public string DeviceType { get; set; } = null!;

    /// <summary>
    /// Increment within each device type. Can not be edited in the Editor. The number remains constant after the assignment because it is part of the topic name.
    /// </summary>
    public int? DeviceIndex { get; set; } = null;

    public List<DevicePin>? Pins
    {
        get;
        set;
    }

    public List<PropertyValue>? PropertyValues
    {
        get;
        set;
    }

    public List<DataSource>? DataSources { get; set; }

    ///// <summary>
    ///// Gets or sets the list of devices that are currently linked to this instance.
    ///// </summary>
    //public List<ConnectedDevice>? LinkedDevices { get; set; } = null;
}