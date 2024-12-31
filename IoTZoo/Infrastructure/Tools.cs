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

using System.Net;
using System.Net.Sockets;

namespace Infrastructure
{
   public class Tools
   {
      public static string GetLocalIpAddress()
      {
         // return Dns.GetHostName();
         IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
         IPAddress? ipAddress = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
         if (ipAddress == null)
         {
            return string.Empty;
         }
         return ipAddress.ToString();
      }

      public static T DeepCopyReflection<T>(T input)
      {
         if (input == null)
         {
            throw new ArgumentNullException("input must not be null!");
         }
         var type = input.GetType();
         var properties = type.GetProperties();
         T clonedObj = (T)Activator.CreateInstance(type)!;
         foreach (var property in properties)
         {
            if (property.CanWrite)
            {
               object? value = property.GetValue(input)!;
               if (value != null &&
                   value.GetType().IsClass &&
                   !value.GetType().FullName!.StartsWith("System."))
               {
                  property.SetValue(clonedObj,
                                    DeepCopyReflection(value));
               }
               else
               {
                  property.SetValue(clonedObj, value);
               }
            }
         }
         return clonedObj;
      }
   }
}
