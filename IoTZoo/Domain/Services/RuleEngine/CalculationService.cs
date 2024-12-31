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

using Dapper;
using DataAccess.Services;
using Domain.Interfaces.RuleEngine;
using Domain.Pocos;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Domain.Services.RuleEngine;

public class CalculationService : DataServiceBase, ICalculationService
{
   public CalculationService(ILogger<CalculationService> logger, IOptions<AppSettings> options) : base(options, logger)
   {
      SetConnectionString(string.Empty);
      Db = GetNewOpenedConnection();
   }

   public object? Calculate(string expression)
   {
      if (string.IsNullOrEmpty(expression))
      {
         return null;
      }
      try
      {
         expression = expression.Replace("&&", "and");
         expression = expression.Replace("||", "or");
         expression = expression.Replace(FunctionNames.Calc, " ", StringComparison.OrdinalIgnoreCase);
         // Let SQLite do the calculation because it is fast!
         return Db.ExecuteScalar($"select {expression};");
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return expression; // return the original expression if an exception occurs
   }
}
