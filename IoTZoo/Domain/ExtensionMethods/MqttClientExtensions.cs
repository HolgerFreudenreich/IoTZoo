using MQTTnet;
using MQTTnet.Protocol;

namespace Domain.Services;

public static class MqttClientExtensions
{
    public static async Task<MqttClientPublishResult> PublishAsync(this IMqttClient client,
                                                                   string topic,
                                                                   string payload,
                                                                   MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos)
            .Build();
        return await client.PublishAsync(message);
    }
}
