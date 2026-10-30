// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Pocos;
using static System.Net.Mime.MediaTypeNames;

namespace Domain.Interfaces;

public interface IDatabaseVersionService
{
    int? GetVersion(string connectionString);

    IReadOnlyDictionary<string, int?> GetAllVersions();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="connectionString"></param>
    /// <returns>All database migrations that must have been executed before the application starts</returns>
    IReadOnlyList<DatabaseMigration> GetMigrations(string connectionString);

    void AddMigration(string connectionString, int databaseVersionId, string migrationScriptName);
}
