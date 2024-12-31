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

namespace Domain.Pocos;

public enum ScreenMode
{
   None = 0,
   MqttExplorer = 1,
   HueLights = 2,
   Projects = 3,
   PublishTopic = 6,
   Rules = 7,
   Scripts = 8,
   Settings = 9,
   SetLocation = 10,
   TopicsHistory = 12,
   KnownMicrocontrollers = 13,
   KnownTopics = 14,
   TestPage = 15,

   Boxes = 50,
   Components = 51,
   StorageAllocation = 52,

   ExampleProjects = 100,
   ExampleProject01 = 101,
   ExampleProject02 = 102,
   ExampleProject03 = 103,
   ExampleProject04 = 104,

   HowToFlashFirmware = 200,
   InstructionsRunOnDocker = 201,
   CoreConcept = 202,
   HowDoRulesWork = 203,
   InstructionsKnownTopics = 204,
   ThingsToConnect = 205,
}
