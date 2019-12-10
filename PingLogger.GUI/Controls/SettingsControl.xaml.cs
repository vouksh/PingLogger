using PingLogger.GUI.Workers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PingLogger.GUI.Controls
{
	/// <summary>
	/// Interaction logic for SettingsControl.xaml
	/// </summary>
	public partial class SettingsControl : UserControl
	{
		public SettingsControl()
		{
			InitializeComponent();
		}

		private void LoadOnBoot_Unchecked(object sender, RoutedEventArgs e)
		{
			Config.LoadWithWindows = false;
			DeleteStartupShortcut();
		}

		private void LoadOnBoot_Checked(object sender, RoutedEventArgs e)
		{
			Config.LoadWithWindows = true;
			CreateStartupShortcut();
		}

		private void StartAllLoggers_Checked(object sender, RoutedEventArgs e)
		{
			Config.StartLoggersAutomatically = true;
		}

		private void StartAllLoggers_Unchecked(object sender, RoutedEventArgs e)
		{
			Config.StartLoggersAutomatically = false;
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			LoadOnBoot.IsChecked = Config.LoadWithWindows;
			StartAllLoggers.IsChecked = Config.StartLoggersAutomatically;
			if(Config.LoadWithWindows)
			{
				CreateStartupShortcut();
			}
		}
		private void CreateStartupShortcut()
		{
			var batchPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\PingLogger.bat";
			if(!File.Exists(batchPath))
			{
				var exePath = Environment.CurrentDirectory + "\\";
				var exeName = AppDomain.CurrentDomain.FriendlyName + ".exe";

				var loggerDrive = exePath.Substring(0, 2);

				var batchScript = "@echo off" + Environment.NewLine;
				batchScript += loggerDrive + Environment.NewLine;
				batchScript += $"CD \"{exePath}\"" + Environment.NewLine;
				batchScript += $"START \"\" \".\\{exeName}\"";
				File.WriteAllText(batchPath, batchScript);
			}
		}
		private void DeleteStartupShortcut()
		{
			var batchPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\PingLogger.bat";
			File.Delete(batchPath);
		}

		private void StartAllLoggersBtn_Click(object sender, RoutedEventArgs e)
		{
			var parentWindow = Window.GetWindow(this) as MainWindow;
			parentWindow.StartAllLoggers();
			//MessageBox.Show("Not yet implemented.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private void StopAllLoggersBtn_Click(object sender, RoutedEventArgs e)
		{
			var parentWindow = Window.GetWindow(this) as MainWindow;
			parentWindow.StopAllLoggers();
		}
	}
}
