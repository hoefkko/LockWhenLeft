using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LockWhenLeft;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var host = CreateHostBuilder().Build();
        var serviceProvider = host.Services;

        Application.Run(serviceProvider.GetRequiredService<MainForm>());
    }

    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<MainForm>();
                services.AddSingleton<IPersonDetector, PersonDetectorAI>();

                // *** REGISTER THE INTERFACE ***
                services.AddSingleton<ILockStateService, LockStateService>();
            });
    }
}