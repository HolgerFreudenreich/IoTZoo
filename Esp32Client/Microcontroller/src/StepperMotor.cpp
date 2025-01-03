// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect stepper motor 28byj-48 with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_STEPPER_MOTOR

#ifndef __STEPPER_MOTOR_HPP__
#include "StepperMotor.hpp"
#endif

namespace IotZoo
{
    StepperMotor::StepperMotor(MqttClient *mqttClient, int deviceIndex, const String &baseTopic,
                               u_int8_t pin1, u_int8_t pin2, u_int8_t pin3, u_int8_t pin4) : DeviceBase(deviceIndex, mqttClient, baseTopic)
    {
        Serial.println("Constructor StepperMotor");
        stepperControl = new StepperControl(StepperControl::DefaultStepCount, pin1, pin2, pin3, pin4);
        stepperControl->SetStepType(StepperControl::FullStep);
        topicActionDone = getBaseTopic() + "/stepper/" + String(deviceIndex) + "/action_done";
    }

    StepperMotor::~StepperMotor()
    {
        Serial.println("Destructor StepperMotor");
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void StepperMotor::addMqttTopicsToRegister(std::vector<Topic> *const topics) const
    {
        topics->push_back(*new Topic(getBaseTopic() + "/stepper/" + String(getDeviceIndex()) + "/actions",
                                     "{{ 'degrees': -300, 'rpm': 10 }, { 'degrees': 300, 'rpm': 16 }}",
                                     MessageDirection::IotZooClientOutbound));

        topics->push_back(*new Topic(topicActionDone,
                                     "The action_id of completed action.",
                                     MessageDirection::IotZooClientOutbound));

        topics->push_back(*new Topic(getBaseTopic() + "/stepper/" + String(getDeviceIndex()) + "/abort",
                                     "Abort all actions.",
                                     MessageDirection::IotZooClientOutbound));
    }

    // Example: "actions":[{"degrees": -300, "rpm": 10 }]
    void StepperMotor::onReceivedActionsForStepper(const String &json)
    {
        Serial.println("*** onReceivedActionsForStepper: " + json);
        if (!json.startsWith("["))
        {
            publishError("wrong data");
            return;
        }
        if (json.length() > 4000)
        {
            publishError("to many actions, aborting...");
            return;
        }
        StaticJsonDocument<4096> jsonDocument;

        DeserializationError error = deserializeJson(jsonDocument, json);
        if (error)
        {
            publishError("deserializeJson() failed: " + String(error.c_str()));
        }

        JsonArray arrActions = jsonDocument.as<JsonArray>();

        for (JsonVariant value : arrActions)
        {
            StepperAction *stepperAction = new StepperAction(value["id"].as<int>(),
                                                             value["degrees"].as<double>(),
                                                             value["rpm"].as<double>(),
                                                             value["start_delay"].as<double>());
            stepperActions.push_back(*stepperAction);
        }
    }

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void StepperMotor::onMqttConnectionEstablished()
    {
        Serial.println("StepperMotor::onMqttConnectionEstablished");
        if (mqttCallbacksAreRegistered)
        {
            Serial.println("Reconnection -> nothing to do.");
            return;
        }
        String topicStepperActions = getBaseTopic() + "/stepper/" + String(getDeviceIndex()) + "/actions";

        mqttClient->subscribe(topicStepperActions, [&](const String &json)
                              { onReceivedActionsForStepper(json); });

        String topicStepperActionsAbort = getBaseTopic() + "/stepper/" + String(getDeviceIndex()) + "/abort";

        mqttClient->subscribe(topicStepperActionsAbort, [&](const String &json)
                              { stop(); });
    }

    void StepperMotor::loop()
    {
        if (stepperActions.size() == 0)
        {
            return;
        }

        auto stepperAction = stepperActions.begin();

        int batches = abs(stepperAction->getDegrees()) / stepperAction->getRpm() / 4;
        if (batches < 1)
        {
            batches = 1;
        }

        Serial.println("Batches: " + String(batches));

        double degrees = stepperAction->getDegrees() / batches;

        StepperControl::StepperAction action;

        bool isForwardDirection = true;

        degrees < 0 ? isForwardDirection = false : isForwardDirection = true;

        if (isForwardDirection)
        {
            Serial.println("Forward");
            action.Direction = StepperControl::Forward;
        }
        else
        {
            Serial.println("Backward");
            action.Direction = StepperControl::Backward;
        }
        degrees = std::abs(degrees);

        action.Type = StepperControl::StepType::HalfStep;
        action.Rpm = stepperAction->getRpm();
        action.StartDelay = stepperAction->getStartDelay();
        action.Steps = stepperControl->GetStepsFromDegrees(degrees);
        action.EndDelay = 0;
        action.DidEndCallback = &actionEnded;

        Serial.println("processing action: degrees: " + String(degrees) + ", rpm: " + String(action.Rpm) + ", steps: " +
                       String(action.Steps) + ", Direction: " + String(action.Direction) + ", Start delay ms: " +
                       String(action.StartDelay) + ", CompletedBatches: " + String(completedBatches) + ", totalBatches: " + String(batches));
        stepperControl->AddStepperAction(action);
        stepperControl->StartAction();

        stepperControl->RemoveAllActions();
        completedBatches++;
        if (completedBatches >= batches)
        {
            Serial.println("Batch done!");
            stepperActions.erase(stepperActions.begin());
            mqttClient->publish(topicActionDone, String(stepperAction->getActionId()));
        }
    }
}

#endif // USE_STEPPER_MOTOR