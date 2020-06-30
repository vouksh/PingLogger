using PingLogger.GUI.Workers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static PingLogger.GUI.MainWindow;

namespace PingLogger.GUI
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			Logger.Info("Application start");
			Logger.Info($"Application is running from directory {AppContext.BaseDirectory}");
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			MainWindow window = new MainWindow(this);
			window.Show();
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			MessageBox.Show((e.ExceptionObject as Exception).Message, "An error has occurred", MessageBoxButton.OK, MessageBoxImage.Error);
			Logger.Log.Fatal((e.ExceptionObject as Exception), "A fatal unhandled exception occurred");
		}
	}
}
