using PingLogger.Workers;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace PingLogger
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private async void Application_Startup(object sender, StartupEventArgs e)
		{
			Thread.CurrentThread.Name = "PrimaryUIThread";
			Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			Logger.Info($"Application start, version {version}");
			Logger.Info($"Application is running from directory {AppContext.BaseDirectory}");
			Logger.Info($"Using {Util.FileBasePath} to save config and logs");
			if (e.Args.Length > 0)
			{
				if(e.Args.Contains("--installerGUID"))
				{
					Config.InstallerGUID = e.Args[Array.IndexOf(e.Args, "--installerGUID") + 1];
					Config.IsInstalled = true;
				}
			}
			Util.SetTheme();
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			if (Config.EnableAutoUpdate)
				await Util.CheckForUpdates();

			MainWindow window = new MainWindow();
			window.Show();
			Util.CloseSplashScreen();
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			MessageBox.Show((e.ExceptionObject as Exception).Message, "An error has occurred", MessageBoxButton.OK, MessageBoxImage.Error);
			Logger.Log.Fatal(e.ExceptionObject as Exception, "A fatal unhandled exception occurred");
		}
	}
}
