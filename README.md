# IoTZoo

IoTZoo connects things and microcontrollers in a simple way.

## Purpose

The purpose of IoTZoo is to make it simple to connect physical hardware (sensors, switches, displays, actuators) to automated workflows. Microcontrollers running the IoTZoo firmware act as the bridge between the physical world and the IoTZoo service: they report hardware state changes as MQTT messages and execute the commands they receive. The IoTZoo service evaluates these messages against user-defined rules, so that devices can react to each other without custom code.

## Overview

IoTZoo consists of three components that work together:

1. **IoTZoo firmware** for ESP32 microcontrollers, installed on one or more microcontrollers.
2. **IoTZoo service**, which processes rules and runs on a web server. A Raspberry Pi running a Docker container can serve as the host.
3. **MQTT broker**, installed on a web server. The IoTZoo service includes its own MQTT broker, but an external broker such as Mosquitto can also be used.


## Components

### IoTZoo firmware (ESP32)
The firmware is located in `Esp32Client\Microcontroller` and is built with PlatformIO (`platformio.ini`). It runs on ESP32 boards and provides drivers for many connected devices, such as buttons, switches, reed contacts, motion sensors, temperature sensors, displays, LED matrices, stepper motors, and GPS modules.

- Enables the connected hardware to be used.
- Publishes an MQTT message when the state of the hardware changes. Example: a motion detector detects movement.
- Receives MQTT messages to control the hardware. Example: showing the time on a display.

### IoTZoo service
- Provides a web interface for defining rules.
- Receives all MQTT messages.
- Processes MQTT messages: checks whether a rule applies, executes it, and publishes a response message. This response may trigger another rule.

### MQTT broker
The broker acts as an intermediary between publishers and subscribers. It receives all messages, filters them, and forwards them to subscribed clients.

Clients publish messages to the broker, and other clients subscribe to topics of interest to receive them. Each MQTT message contains a topic. Topics are hierarchical strings with levels separated by slashes, similar to a file path or URL.

**Example**

- Topic: `IOTZOO/TIME/EVERY_15_SECONDS`
- Payload: `{ "DateTime": "03.01.2025 15:13:45", "Time": "15:13:45", "TimeShort": "15:13" }`

The publish/subscribe model decouples publishers from subscribers. They do not need to know about each other; the broker manages the connection between them.

## Getting Started

To learn how IoTZoo works, review the included instructions and examples. Open `IoTZoo\IotZoo.sln` from the IoTZoo folder in Visual Studio.

