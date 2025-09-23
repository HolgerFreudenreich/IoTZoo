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

using DataAccess.Interfaces;
using HueApi.Models;

namespace Domain.Pocos;

public class FswColorXY
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class HueColorCommand
{
    public int LightId { get; set; }
    public FswColorXY? Color { get; set; }
}

public class HueBrightnessCommand
{
    public int LightId { get; set; }
    public double Brightness { get; set; }
}

public enum ComponentType
{
    Unkown = -1,
    LightWhite = 0,
    LightColor = 1,
    Plug = 2
}

public class HueComponent
{
    public HueComponent(IHueBridgeService hueBridgeService)
    {
        HueBridgeService = hueBridgeService;
    }

    public IHueBridgeService HueBridgeService
    {
        get;
        set;
    }

    public Light Light
    {
        get; set;
    } = null!;

    public ComponentType ComponentType
    {
        get
        {
            if (null == Light)
            {
                return ComponentType.Unkown;
            }
            if (Light.Dimming == null)
            {
                return ComponentType.Plug;
            }
            if (Light.Color == null)
            {
                return ComponentType.LightWhite;
            }
            return ComponentType.LightColor;
        }
    }

    public string ComponentTypeString
    {
        get
        {
            switch (ComponentType)
            {
                default:
                    return ComponentType.ToString();
                case ComponentType.Unkown:
                    return "Unknown";
                case ComponentType.LightWhite:
                    return "Light White";
                case ComponentType.LightColor:
                    return "Light Color";
                case ComponentType.Plug:
                    return "Plug";
            }
        }
    }

    public string Topic
    {
        get
        {
            return $"hue{Light.IdV1}";
        }
    }

    public bool IsLightOn
    {
        get => Light.On.IsOn;

        set
        {
            Light.On.IsOn = value;
            if (Light.On.IsOn)
            {
                HueBridgeService.TurnOnLight(Light);
            }
            else
            {
                HueBridgeService.TurnOffLight(Light);
            }
        }
    }

    public double Brightness
    {
        get
        {
            if (null == Light)
            {
                return 0;
            }
            if (null == Light.Dimming)
            {
                return 0;
            }
            return Light.Dimming.Brightness;
        }

        set
        {
            if (null != Light.Dimming)
            {
                Light.Dimming.Brightness = value;
                HueBridgeService.SetLightBrightness(IdNumeric, value);
            }
        }
    }

    public int IdNumeric
    {
        get
        {
            if (null == Light)
            {
                return -1;
            }
            if (string.IsNullOrEmpty(Light.IdV1))
            {
                return -1;
            }
            return Convert.ToInt32(Light.IdV1.Substring(Light.IdV1.LastIndexOf("/") + 1));
        }
    }

    public string Description
    {
        get;
        set;
    } = null!;
}