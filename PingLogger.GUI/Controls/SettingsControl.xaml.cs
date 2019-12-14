using PingLogger.GUI.Workers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
		private static readonly Regex regex = new Regex("[^0-9.-]+");
		private static bool IsNumericInput(string text)
		{
			return !regex.IsMatch(text);
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
			daysToKeep.Text = Config.DaysToKeepLogs.ToString();
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
		}

		private void StopAllLoggersBtn_Click(object sender, RoutedEventArgs e)
		{
			var parentWindow = Window.GetWindow(this) as MainWindow;
			parentWindow.StopAllLoggers();
		}

		private void daysToKeep_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !IsNumericInput(e.Text);
		}

		private void daysToKeep_TextChanged(object sender, TextChangedEventArgs e)
		{
			try
			{
				Config.DaysToKeepLogs = Convert.ToInt32(daysToKeep.Text);
			}
			catch (FormatException)
			{
				MessageBox.Show("Input must be a number.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}
