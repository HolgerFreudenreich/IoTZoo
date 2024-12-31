// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers without programming knowledge.
// MIT License
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Pocos;
using System.Reflection;

namespace IotZoo.Services
{
   public interface ISearchHelper
   {
      public string SearchString { get; set; }

      public bool Search(BasePoco basePoco);

      public bool Search(BasePoco poco,
                  string searchString);
   }

   public class SearchHelper : ISearchHelper
   {
      public string SearchString { get; set; } = string.Empty;

      protected ILogger<SearchHelper> Logger { get; }
      public SearchHelper(ILogger<SearchHelper> logger)
      {
         this.Logger = logger;
      }

      public bool Search(BasePoco poco,
                         string searchString)
      {
         try
         {
            if (string.IsNullOrEmpty(searchString))
            {
               return true;
            }
            var properties = poco.GetType().GetProperties();

            foreach (var property in properties)
            {
               var propertyValue = property.GetValue(poco);
               if (propertyValue == null)
               {
                  continue;
               }

               string? value = Convert.ToString(propertyValue);
               if (!string.IsNullOrEmpty(value))
               {
                  if (value.Contains(searchString,
                                     StringComparison.OrdinalIgnoreCase))
                  {
                     return true;
                  }
               }
            }
         }
         catch (Exception exception)
         {
            Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
            return false;
         }
         return false;
      }

      public bool Search(BasePoco basePoco)
      {
         return Search(basePoco, SearchString);
      }
   }
}
