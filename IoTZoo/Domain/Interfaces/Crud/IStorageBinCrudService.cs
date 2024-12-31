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
using Component = Domain.Pocos.Component;

namespace Domain.Interfaces.Crud;

/// <summary>
/// A storage bin.
/// </summary>
public interface IStorageBinCrudService
{
   public Task AddStorageBin(StorageBin storageBin);

   public Task RemoveStorageBin(StorageBin storageBin);

   public Task UpdateStorageBin(StorageBin storageBin);

   public Task DeleteStorageBin(StorageBin storageBin);

   public Task SaveStorageBin(StorageBin storageBin);

   public Task<List<StorageBin>?> GetStorageBins();

   public Task<List<StorageBin>?> GetStorageBins(Component component);

}