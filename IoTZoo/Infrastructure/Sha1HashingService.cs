// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

namespace Infrastructure;

using System.Security.Cryptography;
using System.Text;


public interface IHashingService
{
  string AsBase64(string input);
}

public class Sha1HashingService : IHashingService
{
  public string AsBase64(string input)
  {
    input += "Salt for IotZoo"; // do not change!
    byte[] hashedBytes = SHA1.HashData(Encoding.ASCII.GetBytes(input));
    return Convert.ToBase64String(hashedBytes);
  }
}