using PingLogger.Workers;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PingLogger.Controls
{
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public partial class SettingsDialog : Window
	{
		public ICommand CloseWindowCommand { get; set; }
		public SettingsDialog()
		{
			InitializeComponent();
			CloseWindowCommand = new Command(Close);
		}
		private static readonly Regex regex = new Regex("[^0-9.-]+");

		private static bool IsNumericInput(string text)
		{
			Logger.Info($"Checking if {text} is numerical");
			return !regex.IsMatch(text);
		}

		private void LoadOnBoot_Unchecked(object sender, RoutedEventArgs e)
		{
			if (!doingInitialLoad)
			{
				Logger.Info("LoadOnBoot unchecked");
				Config.LoadWithWindows = false;
				DeleteStartupShortcut();
				StartMinimized.Visibility = Visibility.Hidden;
			}
		}

		private void LoadOnBoot_Checked(object sender, RoutedEventArgs e)
		{
			if (!doingInitialLoad)
			{
				Logger.Info("LoadOnBoot checked");
				Config.LoadWithWindows = true;
				CreateStartupShortcut();
				StartMinimized.Visibility = Visibility.Visible;
			}
		}

		private void StartAllLoggers_Checked(object sender, RoutedEventArgs e)
		{
			if (!doingInitialLoad)
			{
				Logger.Info("StartAllLoggers checked");
				Config.StartLoggersAutomatically = true;
			}
		}

		private void StartAllLoggers_Unchecked(object sender, RoutedEventArgs e)
		{
			if (!doingInitialLoad)
			{
				Logger.Info("StartAllLoggers unchecked");
				Config.StartLoggersAutomatically = false;
			}
		}

		private void CreateStartupShortcut()
		{
			if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\PingLogger.bat"))
				File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\PingLogger.bat");

			string shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\PingLogger.lnk";
			if (!File.Exists(shortcutPath))
			{
				Logger.Info("CreateStartupShortcut called");
				Logger.Info($"Saving shortcut to {shortcutPath}");

				IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
				IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);
				shortcut.Description = "Startup shortcut for PingLogger";
				shortcut.TargetPath = Environment.CurrentDirectory + "\\" + AppDomain.CurrentDomain.FriendlyName + ".exe";
				shortcut.WorkingDirectory = Environment.CurrentDirectory;
				shortcut.Save();
			}
		}

		private void DeleteStartupShortcut()
		{
			if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\PingLogger.bat"))
				File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\PingLogger.bat");

			Logger.Info("DeleteStartupShortcut called");
			string shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\PingLogger.lnk";
			if (File.Exists(shortcutPath))
			{
				Logger.Info($"Deleting shortcut file {shortcutPath}");
				File.Delete(shortcutPath);
			}
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
			if (!doingInitialLoad)
			{
				try
				{
					var input = Convert.ToInt32(daysToKeep.Text);
					if (input > 0)
					{
						Config.DaysToKeepLogs = input;
					}
					else
					{
						MessageBox.Show("Input can not be less than 1", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
						daysToKeep.Text = "1";
						Config.DaysToKeepLogs = 1;
					}
				}
				catch (FormatException)
				{
					MessageBox.Show("Input must be a number.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!doingInitialLoad)
			{
				switch (ThemeBox.SelectedIndex)
				{
					case 0:
						Config.Theme = Models.Theme.Auto;
						break;
					case 1:
						Config.Theme = Models.Theme.Light;
						break;
					case 2:
						Config.Theme = Models.Theme.Dark;
						break;
					default:
						Config.Theme = Models.Theme.Auto;
						break;
				}
				Logger.Info($"Theme changed to {Config.Theme}.");
				MainWindow.SetTheme();
			}
		}
		bool doingInitialLoad = false;
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			doingInitialLoad = true;
			LoadOnBoot.IsChecked = Config.LoadWithWindows;
			StartAllLoggers.IsChecked = Config.StartLoggersAutomatically;
			daysToKeep.Text = Config.DaysToKeepLogs.ToString();
			StartMinimized.IsChecked = Config.StartApplicationMinimized;
			AutoUpdateToggle.IsChecked = Config.EnableAutoUpdate;
			if (Config.LoadWithWindows)
			{
				StartMinimized.Visibility = Visibility.Visible;
			}
			else
			{
				StartMinimized.Visibility = Visibility.Hidden;
			}
			if (Config.LoadWithWindows)
			{
				CreateStartupShortcut();
			}
			switch (Config.Theme)
			{
				case Models.Theme.Auto:
					ThemeBox.SelectedIndex = 0;
					break;
				case Models.Theme.Light:
					ThemeBox.SelectedIndex = 1;
					break;
				case Models.Theme.Dark:
					ThemeBox.SelectedIndex = 2;
					break;
			}
			doingInitialLoad = false;
			Logger.Info("SettingsControl loaded");
		}

		private void StartMinimized_Checked(object sender, RoutedEventArgs e)
		{
			if (!doingInitialLoad)
			{
				Logger.Info("StartMinimized Checked");
				Config.StartApplicationMinimized = true;
			}
		}

		private void StartMinimized_Unchecked(object sender, RoutedEventArgs e)
		{
			if (!doingInitialLoad)
			{
				Logger.Info("StartMinimized Unchecked");
				Config.StartApplicationMinimized = false;
			}
		}

		private void AutoUpdateToggle_Checked(object sender, RoutedEventArgs e)
		{
			if(!doingInitialLoad)
			{
				Logger.Info("AutoUpdateToggle checked");
				Config.EnableAutoUpdate = true;
			}
		}

		private void AutoUpdateToggle_Unchecked(object sender, RoutedEventArgs e)
		{
			if (!doingInitialLoad)
			{
				Logger.Info("AutoUpdateToggle unchecked");
				Config.EnableAutoUpdate = false;
			}
		}
	}
}
