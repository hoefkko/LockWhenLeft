using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32.TaskScheduler;

namespace LockWhenLeft;

// Create a shortcut file in the current users start up folder
// Based on this answer on Stackoverflow:
// http://stackoverflow.com/a/19914018/198065
public class AutoStart(string applicationName)
{
    string taskName = $"Start {applicationName}";

    public bool IsEnabled
    {
        get
        {
            using (TaskService ts = new TaskService())
                return ts.GetTask(taskName) != null;
        }
        set
        {
            if (value)
            {
                ScheduleAppOnLogon();
            }
            else if (IsEnabled)
            {
                using (TaskService ts = new TaskService())
                    ts.RootFolder.DeleteTask(taskName);
            }
        }
    }

    public void ScheduleAppOnLogon()
    {
        try
        {
            using (TaskService ts = new TaskService())
            {
                // **Check if the task already exists**
                if (ts.GetTask(taskName) != null)
                {
                    Console.WriteLine($"Task '{taskName}' already exists. Skipping creation.");
                    return;
                }

                // --- Task does not exist, so we create it ---

                Console.WriteLine($"Task '{taskName}' not found. Creating...");

                // Get the path to your application's executable
                string appPath = Environment.ProcessPath;

                // Create a new task definition
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = taskName;
                td.Principal.RunLevel = TaskRunLevel.Highest;
                td.Settings.DisallowStartIfOnBatteries = false;
                td.Settings.StopIfGoingOnBatteries = false;
                // Create the "On Logon" trigger
                LogonTrigger logonTrigger = new LogonTrigger();
                logonTrigger.Delay = TimeSpan.FromSeconds(0);
                td.Triggers.Add(logonTrigger);

                // Create the action to start your application
                td.Actions.Add(new ExecAction(appPath));

                // Register the task
                ts.RootFolder.RegisterTaskDefinition(taskName, td);

                Console.WriteLine($"Task '{taskName}' scheduled successfully!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error scheduling task: {ex.Message}");
        }
    }

}