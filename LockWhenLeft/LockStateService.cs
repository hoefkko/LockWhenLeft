using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
// using Gma.System.MouseKeyHook;
using Timer = System.Windows.Forms.Timer;

namespace LockWhenLeft;

// ... (AppIconState enum remains the same) ...
public enum AppIconState
{
    Standby_InputActive,
    Active_PersonPresent,
    Active_PersonAbsent,
    PausedByUser
}

public class LockStateService : ILockStateService
{
    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelHookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;

    // Hook type constant
    public const int WH_MOUSE_LL = 14;

    // Mouse message constants
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP = 0x0202;
    public const int WM_MOUSEMOVE = 0x0200;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_RBUTTONUP = 0x0205;
    public const int WM_MBUTTONDOWN = 0x0207;
    public const int WM_MOUSEWHEEL = 0x020A;
    public const int WM_XBUTTONDOWN = 0x020B; // X Button 1 or 2 pressed
    public const int WM_XBUTTONUP = 0x020C;   // X Button 1 or 2 released
    public const int WM_MOUSEHWHEEL = 0x020E; // Horizontal scroll
    private delegate IntPtr LowLevelHookProc(int nCode, IntPtr wParam, IntPtr lParam);
    private LowLevelHookProc _keyboardHookProc;
    private LowLevelHookProc _mouseHookProc;
    private IntPtr _keyboardHookId = IntPtr.Zero;
    private IntPtr _mouseHookId = IntPtr.Zero;

    #region Constants & Fields
    private const int BackgroundDelay = 2;

    // Configurable delays (defaults)
    private int _noInputActiveDelay = 5; // RENAMED
    private int _noPersonDetectedDelay = 5; // RENAMED
    private int _popupTimeout = 5;

    private readonly IPersonDetector _detector;
    private readonly Thread _detectorThread;
    // private IKeyboardMouseEvents _globalHook;

    private readonly Timer _enableDetectorTimer;
    private readonly Timer _inactivityTimer;

    // State
    private DateTime _lastMotion;
    private bool _motionDetected;
    private bool _popupVisible;
    private DateTime _popupStartTime;

    private bool _isManuallyPaused;
    private bool _isOnBattery;
    private bool _isSessionLocked;
    #endregion

    #region Events
    public event Action<AppIconState> IconStateChanged;
    public event Action<int> ShowLockPopup;
    public event Action<int> UpdatePopupTimer;
    public event Action CancelLockPopup;
    public event Action LockWorkstation;
    public event Action WakeScreen;
    #endregion

    #region Initialization & Teardown
    public LockStateService(IPersonDetector detector)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _lastMotion = DateTime.Now;

        _detector.PersonDetected += OnMotionDetected;
        _detector.NoPersonDetected += OnNoMotionDetected;
        _detector.NoPersonButChairDetected += OnNoMotionDetected;
        _detector.OnErrorOccurred += errMsg => Debug.WriteLine($"Detector Error: {errMsg}");

        _detectorThread = new Thread(_detector.Start) { IsBackground = true };
        _detectorThread.Start();

        SetupGlobalInputHook();

        _inactivityTimer = new Timer { Interval = 1000 };
        _inactivityTimer.Tick += (s, e) => CheckInactivity();
        _inactivityTimer.Start();

        _enableDetectorTimer = new Timer { Interval = _noInputActiveDelay * 1000 }; // Use renamed field
        _enableDetectorTimer.Tick += (s, e) =>
        {
            if (!_isManuallyPaused && !_isOnBattery && _detector.Paused)
            {
                _lastMotion = DateTime.Now;
                _detector.Paused = false;
                Debug.WriteLine("Input inactivity timer expired. Resuming detection.");
                UpdateIconState();
            }
            _enableDetectorTimer.Stop();
        };
    }

    public void Stop()
    {
        _detector?.Stop();
        // _globalHook?.Dispose();
        _inactivityTimer?.Stop();
        _enableDetectorTimer?.Stop();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Public State Setters
    public void SetManualPause(bool isPaused)
    {
        _isManuallyPaused = isPaused;
        UpdateDetectorPauseState();
    }

    public void SetBatteryState(bool isOnBattery)
    {
        _isOnBattery = isOnBattery;
        UpdateDetectorPauseState();
    }

    public void SetSessionLockState(bool isLocked)
    {
        _isSessionLocked = isLocked;
        Debug.WriteLine($"Locked {isLocked}");
        if (isLocked && !_isOnBattery)
        {
            _detector.Paused = false;
        }
        else
        {
            _lastMotion = DateTime.Now;
            UpdateDetectorPauseState();
        }
    }

    public void CancelLockFromUserInput()
    {
        _popupVisible = false;
        _inactivityTimer.Interval = 1000;
        _lastMotion = DateTime.Now;
        UpdateIconState();
        Debug.WriteLine("Lock cancelled by user.");
    }

    public void SetNoInputActiveDelay(int seconds) // RENAMED
    {
        _noInputActiveDelay = seconds;
        _enableDetectorTimer.Interval = _noInputActiveDelay * 1000;
    }

    public void SetNoPersonDetectedDelay(int seconds) // RENAMED
    {
        _noPersonDetectedDelay = seconds;
    }

    public void SetPopupTimeout(int seconds)
    {
        _popupTimeout = seconds;
    }
    #endregion

    #region Core Logic: Detector & Input
    private void OnMotionDetected()
    {
        _motionDetected = true;
        _lastMotion = DateTime.Now;

        if (_popupVisible)
        {
            _popupVisible = false;
            _inactivityTimer.Interval = 1000;
            CancelLockPopup?.Invoke();
            Debug.WriteLine("Lock cancelled due to motion.");
        }

        UpdateIconState();

        Debug.WriteLine($"Person detected at {DateTime.Now} session is {_isSessionLocked}");
        if (_isSessionLocked)
        {
            Debug.WriteLine($"Person detected at {DateTime.Now} try to wake screen");
            WakeScreen?.Invoke();
        }
    }

    private void OnNoMotionDetected()
    {
        _motionDetected = false;
    }

    private void OnGlobalInput(object sender, EventArgs e)
    {
        HandleGlobalInput();
    }

    private void HandleGlobalInput()
    {
        if (!_isManuallyPaused && !_isOnBattery)
        {
            _enableDetectorTimer.Stop();
            _enableDetectorTimer.Start();
            if (!_detector.Paused) _detector.Paused = true;
        }

        _lastMotion = DateTime.Now;

        if (_popupVisible)
        {
            _popupVisible = false;
            _inactivityTimer.Interval = 1000;
            CancelLockPopup?.Invoke();
            Debug.WriteLine("Lock cancelled due to input.");
        }

        UpdateIconState();
    }

    #endregion

    #region Core Logic: Timers & State

    private void CheckInactivity()
    {
        if (_popupVisible)
        {
            if (_detector.Paused || _isSessionLocked || _motionDetected)
            {
                _popupVisible = false;
                _inactivityTimer.Interval = 1000;
                CancelLockPopup?.Invoke();
                Debug.WriteLine("Lock cancelled due to state change (motion/pause/lock).");
                return;
            }

            var elapsed = (DateTime.Now - _popupStartTime).TotalSeconds;
            if (elapsed >= _popupTimeout)
            {
                _popupVisible = false;
                _inactivityTimer.Interval = 1000;
                CancelLockPopup?.Invoke();

                if (!_isSessionLocked && !_detector.Paused && !_motionDetected)
                {
                    LockWorkstation?.Invoke();
                }
            }
            else
            {
                UpdatePopupTimer?.Invoke(Math.Max(0, _popupTimeout - (int)elapsed));
            }
        }
        else if (!_detector.Paused && !_isSessionLocked)
        {
            var secondsSinceLastMotion = (DateTime.Now - _lastMotion).TotalSeconds;

            if (secondsSinceLastMotion >= _noPersonDetectedDelay) // Use renamed field
            {
                _popupVisible = true;
                _popupStartTime = DateTime.Now;
                _inactivityTimer.Interval = 200;
                ShowLockPopup?.Invoke(_popupTimeout);
                Debug.WriteLine($"Inactivity detected ({secondsSinceLastMotion}s). Showing lock popup.");
            }
        }

        UpdateIconState();
    }

    #endregion

    #region Helper Methods
    private void SetupGlobalInputHook()
    {
        _keyboardHookProc = KeyboardHookCallback;
        _mouseHookProc = MouseHookCallback;
        StartHook();

        // _globalHook = Hook.GlobalEvents();
        // _globalHook.MouseMove += OnGlobalInput;
        // _globalHook.MouseClick += OnGlobalInput;
        // _globalHook.KeyDown += OnGlobalInput;
        // _globalHook.MouseWheel += OnGlobalInput;
    }

    private void UpdateDetectorPauseState()
    {
        var isForcedPause = _isManuallyPaused || _isOnBattery;

        _detector.Paused = isForcedPause;

        _enableDetectorTimer.Enabled = !isForcedPause && !_isSessionLocked;

        if (isForcedPause && _popupVisible)
        {
            _popupVisible = false;
            _inactivityTimer.Interval = 1000;
            CancelLockPopup?.Invoke();
        }

        UpdateIconState();
    }

    private void UpdateIconState()
    {
        var currentIcon = AppIconState.Standby_InputActive;

        if (_isManuallyPaused || _isOnBattery)
        {
            currentIcon = AppIconState.PausedByUser;
        }
        else if (_detector.Paused)
        {
            currentIcon = AppIconState.Standby_InputActive;
        }
        else
        {
            var secondsSinceLastMotion = (DateTime.Now - _lastMotion).TotalSeconds;
            if (secondsSinceLastMotion < BackgroundDelay)
                currentIcon = AppIconState.Active_PersonPresent;
            else
                currentIcon = AppIconState.Active_PersonAbsent;
        }

        IconStateChanged?.Invoke(currentIcon);
    }

    private void StartHook()
    {
        _keyboardHookId = SetKeyboardHook(_keyboardHookProc);
        _mouseHookId = SetMouseHook(_mouseHookProc);
    }

    private void StopHook()
    {
        if (_keyboardHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }
        if (_mouseHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }
    }

    private IntPtr SetKeyboardHook(LowLevelHookProc proc)
    {
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    private IntPtr SetMouseHook(LowLevelHookProc proc)
    {
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        var sw = Stopwatch.StartNew();
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            HandleGlobalInput();
            // int vkCode = Marshal.ReadInt32(lParam);
            // Debug.WriteLine($"keyboard {vkCode}");
        }
        sw.Stop();
        Debug.WriteLine($"Keyboard hook took {sw.ElapsedMilliseconds}");
        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        var sw = Stopwatch.StartNew();
        if (nCode >= 0 && (wParam == (IntPtr) WM_LBUTTONDOWN || wParam == (IntPtr) WM_RBUTTONDOWN ||
                           wParam == (IntPtr) WM_LBUTTONUP || wParam == (IntPtr) WM_MOUSEMOVE ||
                           wParam == (IntPtr) WM_RBUTTONUP || wParam == (IntPtr) WM_MBUTTONDOWN ||
                           wParam == (IntPtr) WM_MOUSEHWHEEL || wParam == (IntPtr) WM_XBUTTONDOWN ||
                           wParam == (IntPtr) WM_XBUTTONUP || wParam == (IntPtr) WM_MOUSEWHEEL))
        {
            HandleGlobalInput();
            Debug.WriteLine($"Mouse hook HandleGlobalInput took {sw.ElapsedMilliseconds}");
            // int vkCode = Marshal.ReadInt32(lParam);
            // Debug.WriteLine($"mouse {vkCode}");
        }
        sw.Stop();
        Debug.WriteLine($"Mouse hook took {sw.ElapsedMilliseconds}");
        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private void ReleaseUnmanagedResources()
    {
        StopHook();
    }

    private void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing)
        {
            // _globalHook.Dispose();
            this.Stop();
            _enableDetectorTimer.Dispose();
            _inactivityTimer.Dispose();
        }
    }

    ~LockStateService()
    {
        Dispose(false);
    }

    #endregion
}