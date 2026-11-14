namespace Domain.Interfaces;

public interface IUnlockAttemptTracker
{
    TimeSpan RemainingDelay { get; }

    TimeSpan RegisterFailure();

    void Reset();
}
