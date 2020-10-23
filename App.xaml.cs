using Microsoft.Win32;
using PingLogger.Models;
using PingLogger.Workers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Net;

namespace PingLogger
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private async void Application_Startup(object sender, StartupEventArgs e)
		{
			Util.SetTheme();
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			Logger.Info($"Application start, version {version}");
			Logger.Info($"Application is running from directory {AppContext.BaseDirectory}");

			if (Config.EnableAutoUpdate)
				await Util.CheckForUpdates();

			MainWindow window = new MainWindow();
			window.Show();
			Util.CloseSplashScreen();
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			MessageBox.Show((e.ExceptionObject as Exception).Message, "An error has occurred", MessageBoxButton.OK, MessageBoxImage.Error);
			Logger.Log.Fatal((e.ExceptionObject as Exception), "A fatal unhandled exception occurred");
		}
	}
}
