using Microsoft.Win32;
using PingLogger.GUI.Models;
using PingLogger.GUI.Workers;
using System;
using System.Windows;

namespace PingLogger.GUI
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			Logger.Info($"Application start, version {version}");
			Logger.Info($"Application is running from directory {AppContext.BaseDirectory}");
			SetTheme();
			MainWindow window = new MainWindow(this);
			window.Show();
		}

		public void SetTheme()
		{
			bool useLightTheme = false;
			switch (Config.Theme)
			{
				case Theme.Auto:
					int lightTheme = Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", 1));
					if (lightTheme == 1)
						useLightTheme = true;
					else
						useLightTheme = false;
					break;
				case Theme.Light:
					useLightTheme = true;
					break;
				case Theme.Dark:
					useLightTheme = false;
					break;
			}
			if (useLightTheme)
			{
				this.Resources.MergedDictionaries[0].Source = new Uri("/Themes/LightTheme.xaml", UriKind.Relative);
			}
			else
			{
				this.Resources.MergedDictionaries[0].Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative);
			}
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			MessageBox.Show((e.ExceptionObject as Exception).Message, "An error has occurred", MessageBoxButton.OK, MessageBoxImage.Error);
			Logger.Log.Fatal((e.ExceptionObject as Exception), "A fatal unhandled exception occurred");
		}
	}
}
