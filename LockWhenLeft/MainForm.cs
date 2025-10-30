using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using LockWhenLeft.Properties;
using Microsoft.Win32;

namespace LockWhenLeft;

public partial class MainForm : Form
{
    // ... (P/Invoke, Constants, Fields remain the same) ...
    #region P/Invoke & Constants
    [DllImport("user32.dll")]
    public static extern bool LockWorkStation();

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    private const byte VK_CONTROL = 0x11;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupRegistryValueName = "LockWhenLeft";
    private const string EventLogSource = "LockWhenLeft";
    private const string EventLogName = "Application";
    private const int LockEventId = 1001;
    private const int PersonDetectedEventId = 1002;
    private const int NoPersonDetectedLockEventId = 1003;
    private const int NoPersonButChairDetectedLockEventId = 1004;
    #endregion

    #region Fields
    private readonly IPersonDetector _detector;
    private readonly ILockStateService _lockService;

    private Icon _greenIcon;
    private Icon _orangeIcon;
    private Icon _redIcon;
    private Icon _whiteIcon;
    private ToolStripMenuItem _pauseItem;
    private NotifyIcon _trayIcon;
    private ContextMenuStrip _trayMenu;
    private List<Form> _popups = new List<Form>();

    private bool _pauseWasEnabledByAC;
    private readonly AutoStart _autoStart;

    #endregion

    #region Constructor & Form Events

    public MainForm(IPersonDetector detector, ILockStateService lockService)
    {
        InitializeComponent();

        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _lockService = lockService ?? throw new ArgumentNullException(nameof(lockService));

        LoadIcons();
        RegisterEventLogSource();
        SetupTray();

        // Subscribe to Service Events
        _lockService.IconStateChanged += OnIconStateChanged;
        _lockService.ShowLockPopup += OnShowLockPopup;
        _lockService.UpdatePopupTimer += OnUpdatePopupTimer;
        _lockService.CancelLockPopup += OnCancelLockPopup;
        _lockService.LockWorkstation += OnLockWorkstation;
        _lockService.WakeScreen += OnWakeScreen;

        _detector.NewFrameAvailable += OnNewFrameAvailable;

        // Setup UI Controls
        SetupDetectionSettings();
        SetupDelaySettings();

        chkForceCamera.CheckedChanged += ChkForceCamera_CheckedChanged;
        _autoStart = new AutoStart("LockWhenLeft");

        LoadStartupCheckState();
        chkStartup.CheckedChanged += chkStartup_CheckedChanged;

        // Subscribe to system events
        SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

        UpdatePowerStatus();
        this.Shown += HandleShown;
    }

    private void HandleShown(object? sender, EventArgs e)
    {
        this.Hide();
        this.Shown -= HandleShown;
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            ShowInTaskbar = false;
        }
        else
        {
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            _lockService?.Dispose();
            _trayIcon?.Dispose();
        }
    }
    #endregion

    #region Logic Service Event Handlers (UI Reactions)

    private void OnIconStateChanged(AppIconState state)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<AppIconState>(OnIconStateChanged), state);
            return;
        }

        Icon newIcon = _greenIcon;
        switch (state)
        {
            case AppIconState.Standby_InputActive: newIcon = _greenIcon; break;
            case AppIconState.Active_PersonPresent: newIcon = _orangeIcon; break;
            case AppIconState.Active_PersonAbsent: newIcon = _redIcon; break;
            case AppIconState.PausedByUser: newIcon = _whiteIcon; break;
        }

        if (_trayIcon != null && _trayIcon.Icon != newIcon) _trayIcon.Icon = newIcon;
        if (this.Icon != null && this.Icon != newIcon) this.Icon = newIcon;
    }

    private void OnShowLockPopup(int timeout)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<int>(OnShowLockPopup), timeout);
            return;
        }

        OnCancelLockPopup(); // Close any existing

        foreach (var screen in Screen.AllScreens)
        {
            var popup = new Form
            {
                Width = 700,
                Height = 300,
                Text = "Inactivity Detected",
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ControlBox = false
            };
            var label = new Label
            {
                Name = "countdownLabel",
                Text = $"No motion detected.\nLocking in {timeout} seconds...",
                Font = new Font("Segoe UI", 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            var cancelButton = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 16),
                Width = 150,
                Height = 60
            };

            cancelButton.Click += (s, e) =>
            {
                _lockService.CancelLockFromUserInput();
                OnCancelLockPopup();
            };

            popup.Controls.Add(label);
            popup.Controls.Add(cancelButton);
            cancelButton.Location = new Point((popup.ClientSize.Width - cancelButton.Width) / 2, 200);

            var bounds = screen.WorkingArea;
            var x = bounds.Left + (bounds.Width - popup.Width) / 2;
            var y = bounds.Top + (bounds.Height - popup.Height) / 2;
            popup.Location = new Point(x, y);

            _popups.Add(popup);
            popup.Show();
        }
    }

    private void OnUpdatePopupTimer(int secondsLeft)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<int>(OnUpdatePopupTimer), secondsLeft);
            return;
        }

        var newText = $"No motion detected.\nLocking in {secondsLeft} seconds...";
        foreach (var p in _popups)
            if (p.Controls["countdownLabel"] is Label lbl)
                lbl.Text = newText;
    }

    private void OnCancelLockPopup()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(OnCancelLockPopup));
            return;
        }

        foreach (var p in _popups)
        {
            p.Close();
            p.Dispose();
        }
        _popups.Clear();
    }

    private void OnLockWorkstation()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(OnLockWorkstation));
            return;
        }

        WriteToEventLog("MotionLockApp is locking the workstation due to inactivity.", LockEventId);
        LockWorkStation();
    }

    private void OnWakeScreen()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(OnWakeScreen));
            return;
        }

        keybd_event(VK_CONTROL, 0, 0, 0);
        Thread.Sleep(50);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
        Debug.WriteLine("WakeScreen: Sent CTRL key press.");
    }

    #endregion

    #region System & UI Event Handlers (Inputs to Service)

    private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.StatusChange)
            BeginInvoke(new Action(UpdatePowerStatus));
    }

    private void UpdatePowerStatus()
    {
        var status = SystemInformation.PowerStatus;
        var isOnAC = status.PowerLineStatus == PowerLineStatus.Online;

        if (isOnAC)
        {
            _pauseItem.Checked = _pauseWasEnabledByAC;
        }
        else
        {
            _pauseWasEnabledByAC = _pauseItem.Checked;
            _pauseItem.Checked = true;
        }

        _lockService.SetBatteryState(!isOnAC);
    }

    private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        BeginInvoke(new Action(() =>
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                Debug.WriteLine($"PC locked at {DateTime.Now}");
                _lockService.SetSessionLockState(true);
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                Debug.WriteLine($"PC unlocked at {DateTime.Now}");
                _lockService.SetSessionLockState(false);
            }
        }));
    }

    private void OnNewFrameAvailable(Bitmap frame)
    {
        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action<Bitmap>(OnNewFrameAvailable), frame);
            }
            catch (ObjectDisposedException) { }
        }
        else
        {
            if (cameraFeedBox.IsDisposed) return;
            var old = cameraFeedBox.Image as Bitmap;
            cameraFeedBox.Image = frame;
            cameraFeedBox.Invalidate();
            old?.Dispose();
        }
    }

    private void ConfidenceTresholdTrackBarValueChanged(object sender, EventArgs e)
    {
        var newValue = confidenceTresholdTrackBar.Value;
        _detector.ConfidenceTreshold = newValue;
        Settings.Default.ConfidenceTreshold = newValue;
        Settings.Default.Save();
    }

    private void SensitivityTresholdTrackBarValueChanged(object sender, EventArgs e)
    {
        var newValue = sensitivityTrackBar.Value;
        _detector.Sensitivity = newValue;
        Settings.Default.Sensitivity = newValue;
        Settings.Default.Save();
    }

    private void ChkForceCamera_CheckedChanged(object sender, EventArgs e)
    {
        _detector.ForceCameraFeed = chkForceCamera.Checked;
        if (chkForceCamera.Checked)
        {
            _detector.Paused = true;
            _detector.Paused = false;
        }
    }

    private void chkStartup_CheckedChanged(object sender, EventArgs e)
    {
        _autoStart.IsEnabled = !_autoStart.IsEnabled;
    }

    // *** UPDATED Event Handlers ***
    private void NoInputActiveDelayNumericUpDown_ValueChanged(object sender, EventArgs e)
    {
        var newValue = (int)noInputActiveDelayNumericUpDown.Value;
        _lockService.SetNoInputActiveDelay(newValue);
        Settings.Default.NoInputActiveDelay = newValue;
        Settings.Default.Save();
    }

    private void NoPersonDetectedDelayNumericUpDown_ValueChanged(object sender, EventArgs e)
    {
        var newValue = (int)noPersonDetectedDelayNumericUpDown.Value;
        _lockService.SetNoPersonDetectedDelay(newValue);
        Settings.Default.NoPersonDetectedDelay = newValue;
        Settings.Default.Save();
    }

    private void PopupTimeoutNumericUpDown_ValueChanged(object sender, EventArgs e)
    {
        var newValue = (int)popupTimeoutNumericUpDown.Value;
        _lockService.SetPopupTimeout(newValue);
        Settings.Default.PopupTimeout = newValue;
        Settings.Default.Save();
    }

    #endregion

    #region UI Setup Helpers

    private void LoadIcons()
    {
        try
        {
            var path = new FileInfo(Environment.ProcessPath).DirectoryName;
            _greenIcon = new Icon(Path.Combine(path, "green.ico"));
            _orangeIcon = new Icon(Path.Combine(path, "orange.ico"));
            _redIcon = new Icon(Path.Combine(path, "red.ico"));
            _whiteIcon = new Icon(Path.Combine(path, "white.ico"));
        }
        catch (FileNotFoundException ex)
        {
            MessageBox.Show($"Icon file not found: {ex.FileName}. Using system defaults.", "Icon Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _greenIcon = SystemIcons.Information;
            _orangeIcon = SystemIcons.Warning;
            _redIcon = SystemIcons.Error;
            _whiteIcon = SystemIcons.Application;
        }
    }

    private void SetupTray()
    {
        _trayMenu = new ContextMenuStrip();
        _pauseItem = new ToolStripMenuItem("Pause") { Checked = false, CheckOnClick = true };

        _pauseItem.CheckedChanged += (s, e) =>
        {
            _lockService.SetManualPause(_pauseItem.Checked);
        };

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) =>
        {
            _lockService?.Dispose();
            Application.Exit();
        };
        _trayMenu.Items.Add(_pauseItem);
        _trayMenu.Items.Add(exitItem);

        _trayIcon = new NotifyIcon
        {
            Text = "Motion Lock App",
            ContextMenuStrip = _trayMenu,
            Visible = true,
            Icon = _greenIcon
        };

        _trayIcon.DoubleClick += (s, e) =>
        {
            Show();
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            BringToFront();
        };
    }

    private void SetupDetectionSettings()
    {
        var initialConfidenceSliderValue = Settings.Default.ConfidenceTreshold;
        if (initialConfidenceSliderValue < confidenceTresholdTrackBar.Minimum)
            initialConfidenceSliderValue = confidenceTresholdTrackBar.Minimum;
        if (initialConfidenceSliderValue > confidenceTresholdTrackBar.Maximum)
            initialConfidenceSliderValue = confidenceTresholdTrackBar.Maximum;
        confidenceTresholdTrackBar.Value = initialConfidenceSliderValue;
        _detector.ConfidenceTreshold = confidenceTresholdTrackBar.Value;

        var initialSensitivitySliderValue = Settings.Default.Sensitivity;
        if (initialSensitivitySliderValue < sensitivityTrackBar.Minimum)
            initialSensitivitySliderValue = sensitivityTrackBar.Minimum;
        if (initialSensitivitySliderValue > sensitivityTrackBar.Maximum)
            initialSensitivitySliderValue = sensitivityTrackBar.Maximum;
        sensitivityTrackBar.Value = initialSensitivitySliderValue;
        _detector.Sensitivity = sensitivityTrackBar.Value;

        confidenceTresholdTrackBar.ValueChanged += ConfidenceTresholdTrackBarValueChanged;
        sensitivityTrackBar.ValueChanged += SensitivityTresholdTrackBarValueChanged;
    }

    // *** UPDATED Setup Method ***
    private void SetupDelaySettings()
    {
        // 1. Load No Input Active Delay
        var inputDelay = Settings.Default.NoInputActiveDelay;
        if (inputDelay < noInputActiveDelayNumericUpDown.Minimum) inputDelay = (int)noInputActiveDelayNumericUpDown.Minimum;
        if (inputDelay > noInputActiveDelayNumericUpDown.Maximum) inputDelay = (int)noInputActiveDelayNumericUpDown.Maximum;
        noInputActiveDelayNumericUpDown.Value = inputDelay;
        _lockService.SetNoInputActiveDelay(inputDelay);

        // 2. Load No Person Detected Delay
        var noPersonDelay = Settings.Default.NoPersonDetectedDelay;
        if (noPersonDelay < noPersonDetectedDelayNumericUpDown.Minimum) noPersonDelay = (int)noPersonDetectedDelayNumericUpDown.Minimum;
        if (noPersonDelay > noPersonDetectedDelayNumericUpDown.Maximum) noPersonDelay = (int)noPersonDetectedDelayNumericUpDown.Maximum;
        noPersonDetectedDelayNumericUpDown.Value = noPersonDelay;
        _lockService.SetNoPersonDetectedDelay(noPersonDelay);

        // 3. Load Popup Timeout
        var popupTimeout = Settings.Default.PopupTimeout;
        if (popupTimeout < popupTimeoutNumericUpDown.Minimum) popupTimeout = (int)popupTimeoutNumericUpDown.Minimum;
        if (popupTimeout > popupTimeoutNumericUpDown.Maximum) popupTimeout = (int)popupTimeoutNumericUpDown.Maximum;
        popupTimeoutNumericUpDown.Value = popupTimeout;
        _lockService.SetPopupTimeout(popupTimeout);

        // 4. Hook up event handlers
        noInputActiveDelayNumericUpDown.ValueChanged += NoInputActiveDelayNumericUpDown_ValueChanged;
        noPersonDetectedDelayNumericUpDown.ValueChanged += NoPersonDetectedDelayNumericUpDown_ValueChanged;
        popupTimeoutNumericUpDown.ValueChanged += PopupTimeoutNumericUpDown_ValueChanged;
    }

    private void LoadStartupCheckState()
    {
        chkStartup.Checked = _autoStart.IsEnabled;
    }

    private void RegisterEventLogSource()
    {
        try
        {
            if (!EventLog.SourceExists(EventLogSource))
            {
                EventLog.CreateEventSource(EventLogSource, EventLogName);
                Debug.WriteLine("Event Log Source registered successfully.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating Event Log source: {ex.Message}");
        }
    }

    private static void WriteToEventLog(string message, int id)
    {
        try
        {
            EventLog.WriteEntry(EventLogSource, message, EventLogEntryType.Information, id);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to write to Event Log: {ex.Message}");
        }
    }
    #endregion
}