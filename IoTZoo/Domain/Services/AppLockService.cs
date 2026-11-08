using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using System.Security.Cryptography;

namespace Domain.Services;

public class AppLockService : IAppLockService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private const char Separator = ':';

    private readonly ISettingsCrudService _settingsService;
    private readonly IUnlockAttemptTracker _attemptTracker;
    private bool _initialized;

    public AppLockService(ISettingsCrudService settingsService, IUnlockAttemptTracker attemptTracker)
    {
        _settingsService = settingsService;
        _attemptTracker = attemptTracker;
    }

    public bool IsLockEnabled { get; private set; }

    public bool IsUnlocked { get; private set; }

    public bool IsLocked => IsLockEnabled && !IsUnlocked;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;
        IsLockEnabled = !string.IsNullOrEmpty(await GetStoredUnlockCodeAsync());
        IsUnlocked = !IsLockEnabled;
    }

    public async Task<UnlockResult> UnlockAsync(string unlockCode)
    {
        await InitializeAsync();
        if (!IsLockEnabled)
        {
            IsUnlocked = true;
            return new UnlockResult(true, TimeSpan.Zero);
        }

        TimeSpan remaining = _attemptTracker.RemainingDelay;
        if (remaining > TimeSpan.Zero)
        {
            return new UnlockResult(false, remaining);
        }

        string? stored = await GetStoredUnlockCodeAsync();
        if (string.IsNullOrEmpty(stored) || !Verify(unlockCode, stored))
        {
            return new UnlockResult(false, _attemptTracker.RegisterFailure());
        }

        _attemptTracker.Reset();
        IsUnlocked = true;
        return new UnlockResult(true, TimeSpan.Zero);
    }

    public void Lock()
    {
        if (IsLockEnabled)
        {
            IsUnlocked = false;
        }
    }

    public async Task SetUnlockCodeAsync(string unlockCode)
    {
        string value = Hash(unlockCode);
        if (await _settingsService.Update(SettingCategory.Security, SettingKey.UnlockCode, value) == 0)
        {
            await _settingsService.Insert(new Setting
            {
                Category = SettingCategory.Security.ToString(),
                SettingKey = SettingKey.UnlockCode.ToString(),
                SettingValue = value
            });
        }
        IsLockEnabled = true;
        IsUnlocked = true;
    }

    public async Task RemoveUnlockCodeAsync()
    {
        Setting? existing = await _settingsService.GetSetting(SettingCategory.Security.ToString(), SettingKey.UnlockCode.ToString());
        if (null != existing)
        {
            await _settingsService.Delete(existing);
        }
        IsLockEnabled = false;
        IsUnlocked = true;
    }

    private async Task<string?> GetStoredUnlockCodeAsync()
    {
        Setting? setting = await _settingsService.GetSetting(SettingCategory.Security.ToString(), SettingKey.UnlockCode.ToString());
        return setting?.SettingValue;
    }

    private static string Hash(string unlockCode)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(unlockCode, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Convert.ToBase64String(salt)}{Separator}{Convert.ToBase64String(hash)}";
    }

    private static bool Verify(string unlockCode, string stored)
    {
        string[] parts = stored.Split(Separator);
        if (parts.Length != 2)
        {
            return false;
        }

        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] expected = Convert.FromBase64String(parts[1]);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(unlockCode, salt, Iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
