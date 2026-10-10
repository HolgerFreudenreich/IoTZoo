// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Private extension without a public use case
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_WS2818_PIXEL_MATRIX
#ifdef USE_WS2818

#include "./DeviceExtensions/AlarmZonesDeviceExtension.hpp"
#include "./pocos/Topic.hpp"
#include "DeviceBase.hpp"
#include "PixelMatrix.hpp"

namespace IotZoo
{
    AlarmZonesDeviceExtension::AlarmZonesDeviceExtension(DeviceBase* const deviceBase) : DeviceExtension(deviceBase)
    {
        Serial.println("Constructor AlarmZonesDeviceExtension");
    }

    void AlarmZonesDeviceExtension::onMqttConnectionEstablished()
    {
        String topic = deviceBase->getBaseTopic() + "/" + deviceBase->getDeviceName() + "/" + String(deviceBase->getDeviceIndex()) + "/alarm";
        deviceBase->getMqttClient()->subscribe(topic, [&](const String& json) { onAlarmReceived(json); });
        Serial.println("PixelMatrix subscribed to topic " + topic);
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void AlarmZonesDeviceExtension::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        topics->emplace_back(deviceBase->getBaseTopic() + "/" + deviceBase->getDeviceName() + "/" + String(deviceBase->getDeviceIndex()) + "/alarm",
                             "{ \"zone\":\"cam1\", \"level\": 1}", MessageDirection::IotZooClientOutbound);
    }

    uint AlarmZonesDeviceExtension::getAlarmLevel(const String& subject)
    {
        uint level = 0;
        if (subject.indexOf("motion") > -1) // yellow
        {
            level = 1;
        }
        else if (subject.indexOf("animal") > -1) // green
        {
            level = 2;
        }
        else if (subject.indexOf("vehicle") > -1) // blue
        {
            level = 3;
        }
        else if (subject.indexOf("person") > -1) // red
        {
            level = 4;
        }
        else if (subject.indexOf("rang") > -1) // purple
        {
            level = 5;
        }
        Serial.println("level: " + String(level));
        return level;
    }

    void AlarmZonesDeviceExtension::onAlarmReceived(const String& subject)
    {
        Serial.println("onAlarmReceived subject: " + subject);
        String subjectLocal = subject;
        subjectLocal.toLowerCase();

        u32_t        color              = 0;
        u32_t        millisUntilTurnOff = 60000;
        uint         brightness         = 4;
        PixelMatrix* pixelMatrix        = (PixelMatrix*)deviceBase;
        if (subjectLocal.indexOf("orange") > -1)
        {
            color = 0xFFA500; // orange
        }
        else if (subjectLocal.indexOf("blue") > -1)
        {
            color = 0x0000FF; // blue
        }
        else if (subjectLocal.indexOf("purple") > -1)
        {
            color = 0x800080; // purple
        }
        else if (subjectLocal.indexOf("lightblue") > -1)
        {
            color = 0xADD8E6; // light blue
        }
        else if (subjectLocal.indexOf("lemon") > -1)
        {
            color = 0xFFFFE0; // lemon yellow
        }
        else if (subjectLocal.indexOf("mint") > -1)
        {
            color = 0x98FB98; // mint
        }
        else if (subjectLocal.indexOf("green") > -1)
        {
            color = 0x00FF00; // green
        }
        else if (subjectLocal.indexOf("red") > -1)
        {
            color = 0xFF0000; // red
        }
        else if (subjectLocal.indexOf("white") > -1)
        {
            color = 0xFFFFFF; // white
        }
        else
        {
            uint level = getAlarmLevel(subjectLocal);
            if (level == 1)
            {
                color = pixelMatrix->getPixels()->Color(255, 175, 0); // motion -> yellow/orange
            }
            else if (level == 2)
            {
                color = pixelMatrix->getPixels()->Color(0, 255, 0); // animal -> green
            }
            else if (level == 3)
            {
                color = pixelMatrix->getPixels()->Color(0, 0, 255); // vehicle -> blue
            }
            else if (level == 4)
            {
                color = pixelMatrix->getPixels()->Color(255, 0, 0); // person -> red
            }
            else if (level == 5)
            {
                color = pixelMatrix->getPixels()->Color(128, 0, 128); // alarm rang -> purple
            }
            brightness = level * 4;
        }

        if (color == 0)
        {
            return;
        }

        if (subjectLocal.indexOf("all") > -1)
        {
            pixelMatrix->setPixelColor(color, 0, pixelMatrix->GetNumberOfLedsPerColumn() * pixelMatrix->GetNumberOfLedsPerRow(), brightness,
                                       millisUntilTurnOff);
            return;
        }
        // 8 x 8 LED matrix specific handling
        // 8 x 8 LED matrix specific handling
        // Row 1:   7   6   5   4   3   2   1   0
        // Row 2:   8   9  10  11  12  13  14  15
        // Row 3:  23  22  21  20  19  18  17  16
        // Row 4:  24  25  26  27  28  29  30  31
        // Row 5:  39  38  37  36  35  34  33  32
        // Row 6:  40  41  42  43  44  45  46  47
        // Row 7:  55  54  53  52  51  50  49  48
        // Row 8:  56  57  58  59  60  61  62  63
        if (pixelMatrix->GetNumberOfLedsPerColumn() == 8 && pixelMatrix->GetNumberOfLedsPerRow() == 8)
        {
            if (subjectLocal.indexOf("dachboden") > -1)
            {
                pixelMatrix->setPixelColor(color, 27, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 34, 3, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("schuppen") > -1)
            {
                pixelMatrix->setPixelColor(color, 51, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 58, 3, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("vorne") > -1)
            {
                pixelMatrix->setPixelColor(color, 0, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 13, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 16, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 29, 3, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("hinten") > -1)
            {
                pixelMatrix->setPixelColor(color, 21, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 26, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 37, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 42, 1, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("terrasse") > -1)
            {
                pixelMatrix->setPixelColor(color, 2, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 11, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 18, 3, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("parkplatz") > -1)
            {
                pixelMatrix->setPixelColor(color, 32, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 44, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 48, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 60, 4, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("garten") > -1)
            {
                pixelMatrix->setPixelColor(color, 42, 2, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 52, 2, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 58, 2, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("westen") > -1)
            {
                pixelMatrix->setPixelColor(color, 52, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 41, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 36, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 25, 3, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("osten") > -1)
            {
                pixelMatrix->setPixelColor(color, 21, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 24, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 37, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 40, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 52, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 56, 4, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("feld") > -1)
            {
                pixelMatrix->setPixelColor(color, 7, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 8, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 23, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 24, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 39, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 40, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 55, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 56, 1, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("klingel") > -1)
            {
                pixelMatrix->setPixelColor(color, 4, 3, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("haus") > -1)
            {
                pixelMatrix->setPixelColor(color, 26, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 33, 6, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 40, 2, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 46, 2, brightness, millisUntilTurnOff);
            }
        }
        // 16 x 16 LED matrix specific handling
        // Row  1:  15  14  13  12  11  10   9   8   7   6   5   4   3   2   1   0
        // Row  2:  16  17  18  19  20  21  22  23  24  25  26  27  28  29  30  31
        // Row  3:  47  46  45  44  43  42  41  40  39  38  37  36  35  34  33  32
        // Row  4:  48  49  50  51  52  53  54  55  56  57  58  59  60  61  62  63
        // Row  5:  79  78  77  76  75  74  73  72  71  70  69  68  67  66  65  64
        // Row  6:  80  81  82  83  84  85  86  87  88  89  90  91  92  93  94  95
        // Row  7: 111 110 109 108 107 106 105 104 103 102 101 100  99  98  97  96
        // Row  8: 112 113 114 115 116 117 118 119 120 121 122 123 124 125 126 127
        // Row  9: 143 142 141 140 139 138 137 136 135 134 133 132 131 130 129 128
        // Row 10: 144 145 146 147 148 149 150 151 152 153 154 155 156 157 158 159
        // Row 11: 175 174 173 172 171 170 169 168 167 166 165 164 163 162 161 160
        // Row 12: 176 177 178 179 180 181 182 183 184 185 186 187 188 189 190 191
        // Row 13: 207 206 205 204 203 202 201 200 199 198 197 196 195 194 193 192
        // Row 14: 208 209 210 211 212 213 214 215 216 217 218 219 220 221 222 223
        // Row 15: 239 238 237 236 235 234 233 232 231 230 229 228 227 226 225 224
        // Row 16: 240 241 242 243 244 245 246 247 248 249 250 251 252 253 254 255
        else if (pixelMatrix->GetNumberOfLedsPerColumn() == 16 && pixelMatrix->GetNumberOfLedsPerRow() == 16)
        {
            if (subjectLocal.indexOf("dachboden") > -1)
            {
                pixelMatrix->setPixelColor(color, 102, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 118, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 134, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 150, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 166, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 182, 4, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("schuppen") > -1)
            {
                pixelMatrix->setPixelColor(color, 93, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 96, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 125, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 128, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 157, 3, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("garten") > -1)
            {
                pixelMatrix->setPixelColor(color, 128, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 157, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 160, 3, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("vorne") > -1)
            {
                pixelMatrix->setPixelColor(color, 6, 10, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 16, 9, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 40, 8, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("hinten") > -1)
            {
                pixelMatrix->setPixelColor(color, 146, 12, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 162, 12, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 178, 12, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("terrasse") > -1)
            {
                pixelMatrix->setPixelColor(color, 77, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 80, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 109, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 112, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 141, 3, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("parkplatz") > -1)
            {
                pixelMatrix->setPixelColor(color, 0, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 28, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 32, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 60, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 64, 4, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("westen") > -1)
            {
                pixelMatrix->setPixelColor(color, 161, 8, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 183, 8, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 193, 8, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 215, 8, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 225, 8, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("osten") > -1)
            {
                pixelMatrix->setPixelColor(color, 168, 6, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 178, 6, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 200, 6, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 210, 6, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 232, 6, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 242, 6, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("feld") > -1)
            {
                pixelMatrix->setPixelColor(color, 240, 16, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("klingel") > -1)
            {
                pixelMatrix->setPixelColor(color, 6, 5, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 21, 5, brightness, millisUntilTurnOff);
            }
            else if (subjectLocal.indexOf("haus") > -1)
            {
                pixelMatrix->setPixelColor(color, 83, 10, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 97, 12, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 113, 14, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 128, 16, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 144, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 156, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 160, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 172, 4, brightness, millisUntilTurnOff);
            }
        }
        pixelMatrix->getPixels()->show();
    }
} // namespace IotZoo

#endif // USE_WS2818
#endif // USE_WS2818_PIXEL_MATRIX