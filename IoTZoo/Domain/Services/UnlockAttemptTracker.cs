using Domain.Interfaces;

namespace Domain.Services;

public class UnlockAttemptTracker : IUnlockAttemptTracker
{
    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(2);
    private const int MaxExponent = 16;

    private readonly object _lock = new();
    private int _failedAttempts;
    private DateTime _blockedUntilUtc = DateTime.MinValue;

    public TimeSpan RemainingDelay
    {
        get
        {
            lock (_lock)
            {
                TimeSpan remaining = _blockedUntilUtc - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
    }

    public TimeSpan RegisterFailure()
    {
        lock (_lock)
        {
            _failedAttempts++;
            int exponent = Math.Min(_failedAttempts - 1, MaxExponent);
            TimeSpan delay = TimeSpan.FromSeconds(BaseDelay.TotalSeconds * Math.Pow(2, exponent));
            _blockedUntilUtc = DateTime.UtcNow + delay;
            return delay;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _failedAttempts = 0;
            _blockedUntilUtc = DateTime.MinValue;
        }
    }
}
