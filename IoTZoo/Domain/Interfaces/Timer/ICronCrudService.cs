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

namespace Domain.Interfaces.Timer;

public interface ICronCrudService
{
   public Task Save(CronJob cronJob);
   public Task Insert(CronJob cronJob);
   public Task Update(CronJob cronJob);
   public Task Delete(CronJob cronJob);
   public Task<List<CronJob>> Load(bool onlyEnabledJobs = true);
   public Task<List<CronJob>> LoadByProject(Project project, bool onlyEnabledJobs = true);
}
