// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// Provides an interface to configure things/devices on the microcontroller (mapping to the correct gpio pins, etc). 
// --------------------------------------------------------------------------------------------------------------------

using Domain.Pocos;

namespace Domain.Interfaces.Crud;

public interface IKnownMicrocontrollerCrudService
{
   /// <summary>
   /// Saves a microcontroller (insert or update).
   /// </summary>
   /// <param name="microcontroller"></param>
   public Task Save(KnownMicrocontroller microcontroller, bool pushToMicrocontroller);

   public Task Insert(KnownMicrocontroller microcontroller);

   public Task Update(KnownMicrocontroller microcontroller);

   public Task<bool> Delete(KnownMicrocontroller microcontroller);

   public Task<List<KnownMicrocontroller>> GetMicrocontrollers();

   public Task<List<KnownMicrocontroller>> GetMicrocontrollers(Project project);

   public Task<KnownMicrocontroller?> GetMicrocontroller(string macAddress);
}
