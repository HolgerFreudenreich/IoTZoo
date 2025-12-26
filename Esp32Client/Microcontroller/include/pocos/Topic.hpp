// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------

#ifndef __TOPIC_HPP__
#define __TOPIC_HPP__

#include <WString.h>

namespace IotZoo
{
   enum class MessageDirection
   {
      IotZooClientInbound = 0,
      IotZooClientOutbound = 1
   };

   struct Topic
   {
      Topic(const String &topicName,
            const String &description,
            MessageDirection messageDirection,
            bool persist = false)
      {
         TopicName = topicName;
         Description = description;
         Direction = static_cast<int>(messageDirection);
         Persist = persist;
      }

      String TopicName;
      String Description;
      int Direction;
      bool Persist;
   };

} // namespace IotZoo
#endif // __TOPIC_HPP__