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
