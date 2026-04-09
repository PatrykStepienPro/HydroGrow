using Microsoft.Extensions.DependencyInjection;

namespace HydroGrow;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		AppDomain.CurrentDomain.UnhandledException += (s, e) =>
		{
			var ex = e.ExceptionObject as Exception;
			System.Diagnostics.Debug.WriteLine($"[CRASH] UnhandledException: {ex}");
			File.WriteAllText(
				Path.Combine(FileSystem.AppDataDirectory, "crash.txt"),
				ex?.ToString() ?? "unknown");
		};

		TaskScheduler.UnobservedTaskException += (s, e) =>
		{
			System.Diagnostics.Debug.WriteLine($"[CRASH] UnobservedTaskException: {e.Exception}");
			File.AppendAllText(
				Path.Combine(FileSystem.AppDataDirectory, "crash.txt"),
				$"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [UnobservedTask]{Environment.NewLine}{e.Exception}{Environment.NewLine}{Environment.NewLine}");
			e.SetObserved();
		};
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}