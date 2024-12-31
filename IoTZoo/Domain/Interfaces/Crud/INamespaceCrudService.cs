// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// Known topics are stored in a table. You can save the history of known topics if you set the property
// KnownTopic.Persist to true.
// --------------------------------------------------------------------------------------------------------------------

namespace Domain.Interfaces.Crud;

public interface INamespaceCrudService
{
   public Task Update(string namespaceName);

   public string GetNamespaceName();
}
