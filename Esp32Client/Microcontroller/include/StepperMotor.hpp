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
#define __STEPPER_MOTOR_HPP__

#include <StepperControl.h>
#include "DeviceBase.hpp"
#include "ArduinoJson.h"

namespace IotZoo
{
  class StepperAction
  {
  public:
    StepperAction(int actionId, double degrees, double rpm, double startDelay)
    {
      Serial.println("Constructor StepperAction. actionId: " + String(actionId) + ", degrees: " + String(degrees) + ", rpm: " + String(rpm));
      this->actionId = actionId;
      this->degrees = degrees;
      if (rpm > 16)
      {
        this->rpm = 16;
      }
      else if (rpm < 0.1)
      {
        this->rpm = 0.1;
      }
      else
      {
        this->rpm = rpm;
      }
      this->startDelay = startDelay;
    }

    ~StepperAction()
    {
      Serial.println("Destructor StepperAction. actionId: " + String(actionId) + ", degrees: " + String(degrees) + ", rpm: " + String(rpm));
    }

    double getDegrees() const
    {
      return degrees;
    }

    double getRpm() const
    {
      return rpm;
    }

    int getActionId() const
    {
      return actionId;
    }

    double getStartDelay() const
    {
      return startDelay;
    }

  protected:
    double degrees;
    double rpm;
    double startDelay;
    int actionId;
  };

  class StepperMotor : DeviceBase
  {
  public:
    StepperMotor(MqttClient *mqttClient, int deviceIndex, const String &baseTopic,
                 u_int8_t pin1, u_int8_t pin2, u_int8_t pin3, u_int8_t pin4);

    virtual ~StepperMotor();

    void stop()
    {
      Serial.println("aborting stepper " + getBaseTopic());
      stepperControl->RemoveAllActions();
      stepperActions.clear();
    }

    /// @brief The IotZooMqtt client is not available, so tell this this user. Providing false information is worse than not providing any information.
    ///        This method is a suitable point to erase a display or stop something.
    virtual void onIotZooClientUnavailable() override
    {
      stop();
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    virtual void addMqttTopicsToRegister(std::vector<Topic> *const topics) const override;

    // Example: "actions":[{"degrees": -300, "rpm": 10 }]
    void onReceivedActionsForStepper(const String &json);

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    virtual void onMqttConnectionEstablished();

    static void actionEnded()
    {
      Serial.println("ended");
    }

    void loop();

  protected:
    StepperControl *stepperControl;
    StepperCallback *actionEndedCallback;

    vector<StepperAction> stepperActions;

    int completedBatches = 0;
    String topicActionDone;
  };
}

#endif // __STEPPER_MOTOR_HPP__
#endif // USE_STEPPER_MOTOR