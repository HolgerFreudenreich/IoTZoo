// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \\/ /       / / / __ \\/ __ \\
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\\____/_/       /____|____/\\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Pocos;

namespace Domain.Interfaces.Crud;

public interface IDashboardItemsCrudService
{
   public Task<List<DashboardItem>> GetDashboardItems();

   public Task<List<DashboardItem>> GetDashboardItemsByDashboardId(int dashboardId);

   public Task<List<DashboardItem>> GetDashboardItemsByKnownTopicId(int knownTopicId);

   public Task SaveDashboardsForKnownTopic(int knownTopicId, IReadOnlyDictionary<int, DashboardItem> dashboardItems);

   public Task<DashboardItem?> GetDashboardItemById(int dashboardItemId);

   public Task<int> Insert(DashboardItem dashboardItem);

   public Task<bool> Update(DashboardItem dashboardItem);

   public Task<bool> Delete(DashboardItem dashboardItem);
}
