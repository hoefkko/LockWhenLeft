## LockWhenLeft

This is the initial public release of **_LockWhenLeft_**, a smart utility that automatically locks your Windows PC when you walk away from it.

Using your webcam, the application uses AI-powered person detection to see if you are at your desk. If you leave, it will automatically trigger a countdown and lock your workstation, keeping your computer secure.

### ✨ Key Features

* **Automatic Presence Detection:** Uses a local AI model (YOLO) to detect if a person is in front of the camera. No data ever leaves your computer.
* **Smart & Efficient Pausing:**
    * Automatically pauses detection when you are actively using your **keyboard or mouse** to save CPU resources.
    * Automatically pauses when your **laptop is unplugged** to save battery life.
    * Automatically shows the login page when the PC is locked and a person is detected.
* **Full Customization:** Double-click the tray icon to access the settings panel, where you can:
    * **Live Camera Feed:** See what the AI sees.
    * **Confidence & Sensitivity:** Fine-tune the AI's detection thresholds.
    * **Input Pause Delay:** Set how many seconds of inactivity (no keyboard/mouse) before detection resumes.
    * **Person Lock Delay:** Set how many seconds to wait after you leave before starting the lock countdown.
    * **Popup Timeout:** Set how long the "_Locking in..._" warning popup appears before locking.
* **System Tray Integration:**
    * Runs quietly in your system tray.
    * **Dynamic Icon** shows the app's status at a glance:
        * **🟢 Green (Standby):** Paused due to recent user input.
        * **🟠 Orange (Active):** Actively monitoring and a person is present.
        * **🔴 Red (Warning):** Actively monitoring and no person is present (approaching lock).
        * **⚪ White (Paused):** Manually paused by the user or on battery power.
    * Right-click to manually **Pause** or **Exit** the application.
* **Start with Windows:** A simple checkbox to have the app launch automatically when you log in.

### 🚀 How to Use

1.  Download the <ins>LockWhenLeft-v1.0.0.zip</ins> file attached below.
2.  Unzip the folder to a permanent location (e.g., <ins>C:\Program Files\LockWhenLeft</ins>).
3.  Run <ins>LockWhenLeft.exe</ins>.
4.  Double-click the new icon in your system tray to open the settings window.
5.  Adjust the **Sensitivity** and **Delay** timers to match your environment.
6.  Check **"Start with Windows"** for convenience.

### To do
Clean-up code
Refactor
Add unit tests
...
