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
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            SetTheme(Config.Theme);
			MainWindow window = new MainWindow(this);
			window.Show();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show((e.ExceptionObject as Exception).Message, "An error has occurred", MessageBoxButton.OK, MessageBoxImage.Error);
            System.IO.File.AppendAllText("./AppErrors.log", (e.ExceptionObject as Exception).Message);
        }

        public void SetTheme(Theme theme)
        {
            string themeName = null;
            switch (theme)
            {
                case Theme.Dark: themeName = "DarkTheme"; break;
                case Theme.Light: themeName = "LightTheme"; break;
            }

            if (!string.IsNullOrEmpty(themeName))
            {
                this.Resources.MergedDictionaries[0].Source = new Uri($"/Themes/{themeName}.xaml", UriKind.Relative);
            }
        }
    }
}
