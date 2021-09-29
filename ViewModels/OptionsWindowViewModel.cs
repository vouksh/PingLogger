using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using PingLogger.Workers;
using ReactiveUI;
using System;
using System.Reactive;

namespace PingLogger.ViewModels
{
	public class OptionsWindowViewModel : ViewModelBase
	{
		public delegate void ThemeChangedHandler(object sender);
		public event ThemeChangedHandler ThemeChanged;
		public ReactiveCommand<Unit, Unit> FindLogFolderCommand { get; }

		public OptionsWindowViewModel()
		{
			daysToKeepLogs = Config.DaysToKeepLogs;
			loadWithSystemBoot = Config.LoadWithSystemBoot;
			if (OperatingSystem.IsWindows())
				AutoUpdateAllowed = true;
			else
				AutoUpdateAllowed = false;

			FindLogFolderCommand = ReactiveCommand.Create(FindLogFolder);
		}

		private async void FindLogFolder()
		{

			if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				var dialog = new Avalonia.Controls.OpenFolderDialog()
				{
					Directory = LogFolderPath,
					Title = "Find new log folder path"
				};
				var newPath = await dialog.ShowAsync(desktop.MainWindow);
				if (!string.IsNullOrEmpty(newPath))
					LogFolderPath = newPath;
			}
		}

		private int daysToKeepLogs = 7;
		public int DaysToKeepLogs
		{
			get => daysToKeepLogs;
			set
			{
				this.RaiseAndSetIfChanged(ref daysToKeepLogs, value);
				Config.DaysToKeepLogs = value;
			}
		}

		private bool autoUpdateAllowed = true;
		public bool AutoUpdateAllowed
		{
			get => autoUpdateAllowed;
			set => this.RaiseAndSetIfChanged(ref autoUpdateAllowed, value);
		}

		private bool loadWithSystemBoot = false;
		public bool LoadWithSystemBoot
		{
			get => loadWithSystemBoot;
			set
			{
				this.RaiseAndSetIfChanged(ref loadWithSystemBoot, value);
				Config.LoadWithSystemBoot = value;
				ToggleStartupShortcut();
			}
		}

		private bool startLoggersAutomatically = Config.StartLoggersAutomatically;
		public bool StartLoggersAutomatically
		{
			get => startLoggersAutomatically;
			set
			{
				this.RaiseAndSetIfChanged(ref startLoggersAutomatically, value);
				Config.StartLoggersAutomatically = value;
			}
		}

		private bool startApplicationMinimized = Config.StartApplicationMinimized;
		public bool StartApplicationMinimized
		{
			get => startApplicationMinimized;
			set
			{
				this.RaiseAndSetIfChanged(ref startApplicationMinimized, value);
				Config.StartApplicationMinimized = value;
			}
		}

		private bool enableAutoUpdate = Config.EnableAutoUpdate;
		public bool EnableAutoUpdate
		{
			get => enableAutoUpdate;
			set
			{
				this.RaiseAndSetIfChanged(ref enableAutoUpdate, value);
				Config.EnableAutoUpdate = value;
			}
		}

		private int selectedTheme = (int)Config.Theme;
		public int SelectedTheme
		{
			get => selectedTheme;
			set
			{
				this.RaiseAndSetIfChanged(ref selectedTheme, value);
				Config.Theme = (Models.Theme)value;
				ThemeChanged?.Invoke(this);
			}
		}

		private string logFolderPath = Config.LogSavePath;
		public string LogFolderPath
		{
			get => logFolderPath;
			set
			{
				this.RaiseAndSetIfChanged(ref logFolderPath, value);
				Config.LogSavePath = value;
			}
		}

		private void ToggleStartupShortcut()
		{
			if (LoadWithSystemBoot)
				Utils.CreateShortcut();
			else
				Utils.DeleteShortcut();
		}
	}
}
