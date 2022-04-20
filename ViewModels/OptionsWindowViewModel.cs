using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using PingLogger.Workers;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reflection;
using Avalonia.Media;
using Microsoft.AppCenter.Analytics;
using PingLogger.Extensions;
using PingLogger.Models;

namespace PingLogger.ViewModels
{
	public class OptionsWindowViewModel : ViewModelBase
	{
		public delegate void ThemeChangedHandler(object sender);
		public event ThemeChangedHandler ThemeChanged;
		public ReactiveCommand<Unit, Unit> FindLogFolderCommand { get; }

		public List<string> AvaloniaColors { get; set; } = new();

		public OptionsWindowViewModel()
		{
			_daysToKeepLogs = Config.DaysToKeepLogs;
			_loadWithSystemBoot = Config.LoadWithSystemBoot;
			if (OperatingSystem.IsWindows())
				AutoUpdateAllowed = true;
			else
				AutoUpdateAllowed = false;
			
			GetColors();

			FindLogFolderCommand = ReactiveCommand.Create(FindLogFolder);
		}

		private void GetColors()
		{
			var colorProps = typeof(Colors).GetProperties().OrderBy(p => p.Name);
			foreach (var colorProperty in colorProps)
			{
				AvaloniaColors.Add(colorProperty.Name.SplitCamelCase());
			}
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

		private int _daysToKeepLogs = 7;
		public int DaysToKeepLogs
		{
			get => _daysToKeepLogs;
			set
			{
				this.RaiseAndSetIfChanged(ref _daysToKeepLogs, value);
				Config.DaysToKeepLogs = value;
			}
		}

		private bool _autoUpdateAllowed = true;
		public bool AutoUpdateAllowed
		{
			get => _autoUpdateAllowed;
			set
			{
				this.RaiseAndSetIfChanged(ref _autoUpdateAllowed, value);
				Config.EnableAutoUpdate = value;
			}
		}

		private bool _allowAnalytics = Config.AllowAnalytics;
		public bool AllowAnalytics
		{
			get => _allowAnalytics;
			set
			{
				this.RaiseAndSetIfChanged(ref _allowAnalytics, value);
				Config.AllowAnalytics = value;
				Analytics.SetEnabledAsync(value);
			} 
		}

		private bool _loadWithSystemBoot = false;
		public bool LoadWithSystemBoot
		{
			get => _loadWithSystemBoot;
			set
			{
				this.RaiseAndSetIfChanged(ref _loadWithSystemBoot, value);
				Config.LoadWithSystemBoot = value;
				ToggleStartupShortcut();
			}
		}

		private bool _startLoggersAutomatically = Config.StartLoggersAutomatically;
		public bool StartLoggersAutomatically
		{
			get => _startLoggersAutomatically;
			set
			{
				this.RaiseAndSetIfChanged(ref _startLoggersAutomatically, value);
				Config.StartLoggersAutomatically = value;
			}
		}

		private bool _startApplicationMinimized = Config.StartApplicationMinimized;
		public bool StartApplicationMinimized
		{
			get => _startApplicationMinimized;
			set
			{
				this.RaiseAndSetIfChanged(ref _startApplicationMinimized, value);
				Config.StartApplicationMinimized = value;
			}
		}

		private bool _enableAutoUpdate = Config.EnableAutoUpdate;
		public bool EnableAutoUpdate
		{
			get => _enableAutoUpdate;
			set
			{
				this.RaiseAndSetIfChanged(ref _enableAutoUpdate, value);
				Config.EnableAutoUpdate = value;
			}
		}

		private int _selectedTheme = (int)Config.Theme;
		public int SelectedTheme
		{
			get => _selectedTheme;
			set
			{
				this.RaiseAndSetIfChanged(ref _selectedTheme, value);
				Config.Theme = (Models.Theme)value;
				ThemeChanged?.Invoke(this);
			}
		}

		private string _logFolderPath = Config.LogSavePath;
		public string LogFolderPath
		{
			get => _logFolderPath;
			set
			{
				this.RaiseAndSetIfChanged(ref _logFolderPath, value);
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

		private int _primaryColor = Config.PrimaryColor;
		public int PrimaryColor
		{
			get => _primaryColor;
			set
			{
				this.RaiseAndSetIfChanged(ref _primaryColor, value);
				Config.PrimaryColor = value;
				ThemeChanged?.Invoke(this);
			}
		}
		
		private int _secondaryColor = Config.SecondaryColor;
		public int SecondaryColor
		{
			get => _secondaryColor;
			set
			{
				this.RaiseAndSetIfChanged(ref _secondaryColor, value);
				Config.SecondaryColor = value;
				ThemeChanged?.Invoke(this);
			}
		}
	}
}
