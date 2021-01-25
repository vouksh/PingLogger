using FontAwesome.WPF;
using PingLogger.Workers;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Ookii.Dialogs.Wpf;

namespace PingLogger.Controls
{
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public partial class SettingsDialog : Window
	{
		public ICommand CloseWindowCommand { get; set; }
		public SettingsDialog(MainWindow ownerWindow)
		{
			this.Owner = ownerWindow;
			InitializeComponent();
			CloseWindowCommand = new Command(Close);
		}
		private static readonly Regex _regex = new("[^0-9.-]+");

		private static bool IsNumericInput(string text)
		{
			Logger.Info($"Checking if {text} is numerical");
			return !_regex.IsMatch(text);
		}

		private void LoadOnBoot_Unchecked(object sender, RoutedEventArgs e)
		{
			if (!_doingInitialLoad)
			{
				Logger.Info("LoadOnBoot unchecked");
				Config.LoadWithWindows = false;
				DeleteStartupShortcut();
				StartMinimized.Visibility = Visibility.Hidden;
			}
		}

		private void LoadOnBoot_Checked(object sender, RoutedEventArgs e)
		{
			if (!_doingInitialLoad)
			{
				Logger.Info("LoadOnBoot checked");
				Config.LoadWithWindows = true;
				CreateStartupShortcut();
				StartMinimized.Visibility = Visibility.Visible;
			}
		}

		private void StartAllLoggers_Checked(object sender, RoutedEventArgs e)
		{
			if (!_doingInitialLoad)
			{
				Logger.Info("StartAllLoggers checked");
				Config.StartLoggersAutomatically = true;
			}
		}

		private void StartAllLoggers_Unchecked(object sender, RoutedEventArgs e)
		{
			if (!_doingInitialLoad)
			{
				Logger.Info("StartAllLoggers unchecked");
				Config.StartLoggersAutomatically = false;
			}
		}

		private static void CreateStartupShortcut()
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

		private static void DeleteStartupShortcut()
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

		private void StartAllLoggersBtn_Click(object sender, RoutedEventArgs e) => (Owner as MainWindow)?.StartAllLoggers();

		private void StopAllLoggersBtn_Click(object sender, RoutedEventArgs e) => (Owner as MainWindow)?.StopAllLoggers();

		private void DaysToKeep_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !IsNumericInput(e.Text);
		}

		private void DaysToKeep_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!_doingInitialLoad)
			{
				if (int.TryParse(DaysToKeep.Text, out int input))
				{
					if (input > 0)
					{
						Config.DaysToKeepLogs = input;
					}
					else
					{
						MessageBox.Show("Input can not be less than 1", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
						DaysToKeep.Text = "1";
						Config.DaysToKeepLogs = 1;
					}
				} else
				{
					MessageBox.Show("Input must be a number.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					DaysToKeep.Text = Config.DaysToKeepLogs.ToString();
				}
			}
		}

		private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!_doingInitialLoad)
			{
				Config.Theme = ThemeBox.SelectedIndex switch
				{
					0 => Models.Theme.Auto,
					1 => Models.Theme.Light,
					2 => Models.Theme.Dark,
					_ => Models.Theme.Auto,
				};
				Logger.Info($"Theme changed to {Config.Theme}.");
				Util.SetTheme();
				(Owner as MainWindow)?.UpdateGraphStyles();
			}
		}

		private bool _doingInitialLoad;
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			_doingInitialLoad = true;
			CustomPathBtn.Content = new ImageAwesome()
			{
				Icon = FontAwesomeIcon.FolderOpen,
				SpinDuration = 2,
				Foreground = Util.IsLightTheme ? Brushes.Black : Brushes.White,
				Width = 14,
				Height = 14,
				ToolTip = "Browse"
			};
			CustomPathBox.Text = Config.LogSavePath;
			AutoUpdateToggle.Visibility = Util.AppIsClickOnce ? Visibility.Hidden : Visibility.Visible;
			LoadOnBoot.IsChecked = Config.LoadWithWindows;
			StartAllLoggers.IsChecked = Config.StartLoggersAutomatically;
			DaysToKeep.Text = Config.DaysToKeepLogs.ToString();
			StartMinimized.IsChecked = Config.StartApplicationMinimized;
			AutoUpdateToggle.IsChecked = Config.EnableAutoUpdate;
			StartMinimized.Visibility = Config.LoadWithWindows ? Visibility.Visible : Visibility.Hidden;
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
			_doingInitialLoad = false;
			Logger.Info("SettingsControl loaded");
		}

		private void StartMinimized_Checked(object sender, RoutedEventArgs e)
		{
			if (!_doingInitialLoad)
			{
				Logger.Info("StartMinimized Checked");
				Config.StartApplicationMinimized = true;
			}
		}

		private void StartMinimized_Unchecked(object sender, RoutedEventArgs e)
		{
			if (!_doingInitialLoad)
			{
				Logger.Info("StartMinimized Unchecked");
				Config.StartApplicationMinimized = false;
			}
		}

		private void AutoUpdateToggle_Checked(object sender, RoutedEventArgs e)
		{
			if (!_doingInitialLoad)
			{
				Logger.Info("AutoUpdateToggle checked");
				Config.EnableAutoUpdate = true;
			}
		}

		private void AutoUpdateToggle_Unchecked(object sender, RoutedEventArgs e)
		{
			if (!_doingInitialLoad)
			{
				Logger.Info("AutoUpdateToggle unchecked");
				Config.EnableAutoUpdate = false;
			}
		}

		private void customPathBtn_Click(object sender, RoutedEventArgs e)
		{
			var continueDialog = new TaskDialog()
			{
				WindowTitle = "Are you sure?",
				MainInstruction = "Changing this setting has many consequences.",
				Content = "If you modify this setting, all currently running pingers will be stopped. Once you change the folder path, the existing files will NOT be moved and you will start with fresh logs. By clicking Yes, you acknowledge this, and will be prompted for the new path."
			};
			TaskDialogButton yesButton = new TaskDialogButton(ButtonType.Yes);
			TaskDialogButton noButton = new TaskDialogButton(ButtonType.No);
			continueDialog.Buttons.Add(yesButton);
			continueDialog.Buttons.Add(noButton);
			var resultBtn = continueDialog.ShowDialog();
			if(resultBtn.ButtonType == ButtonType.No)
			{
				return;
			}

			var folderDialog = new VistaFolderBrowserDialog {SelectedPath = Path.GetFullPath(Config.LogSavePath)};

			if(folderDialog.ShowDialog() == true)
			{
				Config.LogSavePath = folderDialog.SelectedPath;
				CustomPathBox.Text = Config.LogSavePath;
				(Owner as MainWindow)?.StopAllLoggers();
			}
		}
	}
}
