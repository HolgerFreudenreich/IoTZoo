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

using Domain.Pocos;

namespace Domain.Interfaces.Crud;

public interface IScriptCrudService
{
   public Task Save(Script script);

   public Task Insert(Script script);

   public Task Update(Script script);

   public Task Delete(Script script);

   public Task<Script?> LoadScript(string scriptName);

   Task<List<Script>?> LoadScripts();
}
