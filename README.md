# IoTZoo
Connect Things with microcontrollers in a simple way.


IOTZOO works through the interaction of

1. The IOTZOO firmware for (ESP32) microcontrollers, which is installed on 1..n microcontrollers.

2. The IOTZOO service, which can process rules. This service runs on a web server. A Raspberry PI running a Docker container can serve as hardware.

3. An MQTT broker that is installed on a web server. The IOTZOO Service includes its own MQTT broker, but an external broker such as Mosquitto can also be used.


Main Tasks of the IOTZOO firmware
- Enable the connected hardware to be used.

- Publishing a message via MQTT when the status of the hardware changes. Example: A motion detector has detected a movement.

- Receiving messages via MQTT to control the hardware. Example: The time is to be shown on a display.



Main Tasks of the IOTZOO service
- Provision of a web interface for entering rules

- Receiving off all MQTT messages

- Processing of all MQTT messages: Check whether a rule needs to be applied, execute the rule and then send a response message. This message could trigger another rule.



Main Tasks of the MQTT broker
The broker acts as intermediate between senders (publishers) and receivers (subscribers). It is responsible for receiving all messages, filtering them, and sending them to subscribed clients.


Clients publish messages to the broker. Other clients subscribe to specific topics to receive messages. Each MQTT message includes a topic, and clients subscribe to topics of their interest.

Topics are strings that messages are published to and subscribed to. Topics are hierarchical and can contain multiple levels separated by slashes, like a file path or URL.


Example:

Topic: IOTZOO/TIME/EVERY_15_SECONDS

Message/Payload: { "DateTime": "03.01.2025 15:13:45", "Time": "15:13:45", "TimeShort": "15:13" }


The publish/subcribe model decouples the publisher of the message from the subscribers. The publisher and subscriber are unaware that the other exists. The broker handles the connection between them.


To become familiar with how it works, it is good to look at the included instructions and examples. Open Solution IotZoo.sln from the IotZoo folder.

