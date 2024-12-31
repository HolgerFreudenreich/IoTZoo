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
    byte[] byteArray = Encoding.ASCII.GetBytes(input);
    byte[] hashedBytes;
    using (SHA1 sha = SHA1.Create())
    {
      hashedBytes = sha.ComputeHash(byteArray);
    }
    string base64String = Convert.ToBase64String(hashedBytes);
    return base64String;
  }
}