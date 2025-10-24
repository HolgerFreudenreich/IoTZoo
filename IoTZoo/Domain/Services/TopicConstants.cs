// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// Service that determines whether one or more expressions are true.
// --------------------------------------------------------------------------------------------------------------------

namespace DataAccess.Services
{
    public static class TopicConstants
    {
        public const string REGISTER_MICROCONTROLLER = "register_microcontroller";
        public const string REGISTER_KNOWN_TOPIC = "register_known_topic";

        public const string ALIVE = "alive";
        public const string ALIVE_ACK = "alive_ack";
        public const string I_AM_LOST = "i_am_lost";

        public const string DEVICE_CONFIG = "device_config"; // receive the device config to the microcontroller.
        public const string SAVE_DEVICE_CONFIG = "save_device_config";
        public const string SAVE_MICROCONTROLLER_CONFIG = "save_microcontroller_config";

        // .../timer
        public const string TIMER = "timer";

        public const string IS_DAY_MODE = "is_day_mode";
        public const string SUNRISE_NOW = "sunrise_now";
        public const string SUNSET_NOW = "sunset_now";

        public const string MINUTES_NEXT_SUNRISE = "minutes_next_sunrise"; // Minutes until the next sunrise
        public const string MINUTES_NEXT_SUNSET = "minutes_next_sunset"; // Minutes until the next sunset

        public const string MINUTES_AFTER_SUNRISE = "minutes_after_sunrise"; // Minutes after the last sunrise
        public const string MINUTES_AFTER_SUNSET = "minutes_after_sunset"; // Minutes after the last sunset

        public const string INIT = "init";
        public const string EVERY_SECOND = "every_second";
        public const string EVERY_05_SECONDS = "every_05_seconds";
        public const string EVERY_10_SECONDS = "every_10_seconds";
        public const string EVERY_15_SECONDS = "every_15_seconds";
        public const string EVERY_30_SECONDS = "every_30_seconds";

        public const string EVERY_MINUTE = "every_minute";
        public const string EVERY_05_MINUTES = "every_05_minutes";
        public const string EVERY_10_MINUTES = "every_10_minutes";
        public const string EVERY_15_MINUTES = "every_15_minutes";
        public const string EVERY_30_MINUTES = "every_30_minutes";

        public const string EVERY_HOUR = "every_hour";

        public const string HUE_BRIDGE = "hue_bridge";

        // .../hue
        public const string HUE = "hue";
        // .../hue/light
        public const string LIGHT = "light";

        public const string ON = "on";
        public const string OFF = "off";
        public const string TOGGLE = "toggle";
        public const string DARKER = "darker";
        public const string BRIGHTER = "brighter";
        public const string BRIGHTNESS = "brightness";

        // .../hue/light/color
        public const string COLOR = "color";
        // .../hue/light/color/cyan
        public const string CYAN = "cyan";
        // .../hue/light/color/green
        public const string GREEN = "green";
        // .../hue/light/color/yellow
        public const string YELLOW = "yellow";
        // .../hue/light/color/orange
        public const string ORANGE = "orange";
        // .../hue/light/color/red
        public const string RED = "red";
        // .../hue/light/color/blue
        public const string BLUE = "blue";
        // .../hue/light/color/purple
        public const string PURPLE = "purple";
        // .../hue/light/color/gold
        public const string GOLD = "gold";
        // .../hue/light/color/white_cold
        public const string WHITE_COLD = "white_cold";
        // .../hue/light/color/white_warm
        public const string WHITE_WARM = "white_warm";
    }
}

