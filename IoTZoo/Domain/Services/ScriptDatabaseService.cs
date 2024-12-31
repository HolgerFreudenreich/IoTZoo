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
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using Script = Domain.Pocos.Script;

namespace Domain.Services;

public class ScriptDatabaseService : DataServiceBase, IScriptCrudService
{
   public ScriptDatabaseService(IOptions<AppSettings> options,
                                ILogger<DataServiceBase> logger) : base(options, logger)
   {
      Initialize(typeof(Script), "script", "script");
   }

   public async Task Delete(Script script)
   {
      try
      {
         int createdScriptId = await Db.QuerySingleAsync<int>(DeleteSql, script);
         script.ScriptId = createdScriptId;
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task<Script?> LoadScript(string scriptName)
   {
      Script? script = null;
      try
      {
         script = await Db.QueryFirstOrDefaultAsync<Script>($"select {FieldListSelect} from {FullQualifiedTableName} where script_name = @ScriptName;",
                                   new { ScriptName = scriptName });
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return script;
   }

   public async Task<List<Script>?> LoadScripts()
   {
      IEnumerable<Script>? scripts = null!;
      try
      {
         scripts = await Db.QueryAsync<Script>($"select {FieldListSelect} from {FullQualifiedTableName} order by script_id;");
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return scripts.AsList();
   }

   public async Task Insert(Script script)
   {
      try
      {
         int createdScriptId = await Db.QuerySingleAsync<int>(InsertSql, script);
         script.ScriptId = createdScriptId;
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task Save(Script script)
   {
      if (script.ScriptId > 0)
      {
         await Update(script);
      }
      else
      {
         await Insert(script);
      }
   }

   public async Task Update(Script script)
   {
      try
      {
         int createdScriptId = await Db.QuerySingleAsync<int>(UpdateSql, script);
         script.ScriptId = createdScriptId;
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }
}
