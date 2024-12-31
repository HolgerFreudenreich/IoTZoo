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

using Microsoft.AspNetCore.Mvc;

namespace IotZoo.Controller;

[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
  [HttpGet]
  public string Get()
  {
    return Version;
  }

   public static string Version { get; set; } = "0.1.4";
}