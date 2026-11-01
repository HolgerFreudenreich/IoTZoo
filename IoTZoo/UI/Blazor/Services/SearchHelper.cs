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
// (c) 2025 - 2026 Holger Freudenreich under MIT license
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
                return poco.GetType().GetProperties()
                            .Select(property => property.GetValue(poco))
                            .Where(propertyValue => propertyValue != null)
                            .Select(propertyValue => Convert.ToString(propertyValue))
                            .Any(value => !string.IsNullOrEmpty(value) &&
                                          value.Contains(searchString, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
                return false;
            }
        }

        public bool Search(BasePoco basePoco)
        {
            return Search(basePoco, SearchString);
        }
    }
}
