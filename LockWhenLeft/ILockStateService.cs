using System;

namespace LockWhenLeft;

/// <summary>
/// Defines the interface for the core application logic and state management.
/// </summary>
public interface ILockStateService : IDisposable
{
    /// <summary>
    /// Fired when the application's icon state should change.
    /// </summary>
    event Action<AppIconState> IconStateChanged;

    /// <summary>
    /// Fired when the UI should display the lock warning popup.
    /// </summary>
    event Action<int> ShowLockPopup;

    /// <summary>
    /// Fired to update the countdown text on the visible popup.
    /// </summary>
    event Action<int> UpdatePopupTimer;

    /// <summary>
    /// Fired when the UI should hide the lock warning popup.
    /// </summary>
    event Action CancelLockPopup;

    /// <summary>
    /// Fired when the workstation should be locked.
    /// </summary>
    event Action LockWorkstation;

    /// <summary>
    /// Fired when the screen should be woken up.
    /// </summary>
    event Action WakeScreen;

    /// <summary>
    /// Stops all timers, hooks, and background threads.
    /// </summary>
    void Stop();

    /// <summary>
    /// Informs the service of the user's manual pause selection.
    /// </summary>
    void SetManualPause(bool isPaused);

    /// <summary>
    /// Informs the service whether the system is on battery power.
    /// </summary>
    void SetBatteryState(bool isOnBattery);

    /// <summary>
    /// Informs the service whether the user session is locked.
    /// </summary>
    void SetSessionLockState(bool isLocked);

    /// <summary>
    /// Called by the UI when the user clicks "Cancel" on the popup.
    /// </summary>
    void CancelLockFromUserInput();

    /// <summary>
    /// Sets the delay in seconds after user input to re-enable the detector.
    /// </summary>
    void SetNoInputActiveDelay(int seconds); // RENAMED

    /// <summary>
    /// Sets the delay in seconds of no person detected to trigger the lock warning.
    /// </summary>
    void SetNoPersonDetectedDelay(int seconds); // RENAMED

    /// <summary>
    /// Sets the duration in seconds the lock warning popup is shown.
    /// </summary>
    void SetPopupTimeout(int seconds);
}