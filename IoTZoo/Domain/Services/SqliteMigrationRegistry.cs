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

namespace Domain.Services;

public record SqliteMigration(string DatabaseScriptName, int DatabaseVersionId, string MigrationScriptName);

public static class SqliteMigrationRegistry
{
    // Register new migrations here. A migration is executed once, if DatabaseVersionId is greater than the current db_version.version_number.
    // Migration scripts are located in Database\Scripts and must update db_version.version_number themselves.
    // Example: new("CreateDatabaseIotZoo.txt", 2, "IotZoo_migration_to_version_2.sql"),
    public static IReadOnlyList<SqliteMigration> Migrations { get; } =
    [
        new("CreateDatabaseSettings.txt", 2, "settings_migration_to_version_2.sql")
    ];
}
