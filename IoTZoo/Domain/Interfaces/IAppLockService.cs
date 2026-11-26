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

namespace Domain.Interfaces;

public record UnlockResult(bool IsUnlocked, TimeSpan RetryAfter);

public interface IAppLockService
{
    bool IsLockEnabled { get; }

    bool IsUnlocked { get; }

    bool IsLocked { get; }

    Task InitializeAsync();

    Task<UnlockResult> UnlockAsync(string unlockCode);

    void Lock();

    Task SetUnlockCodeAsync(string unlockCode);

    Task RemoveUnlockCodeAsync();
}
