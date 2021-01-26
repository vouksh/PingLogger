using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PingLogger.Workers;
using ReactiveUI;

namespace PingLogger.ViewModels
{
	public class OptionsWindowViewModel : ViewModelBase
	{
		public delegate void ThemeChangedHandler(object sender);
		public event ThemeChangedHandler ThemeChanged;

		public OptionsWindowViewModel()
		{
			daysToKeepLogs = Config.DaysToKeepLogs;
			loadWithSystemBoot = Config.LoadWithSystemBoot;
		}
		private int daysToKeepLogs = 7;
		public int DaysToKeepLogs
		{
			get => daysToKeepLogs;
			set {
				this.RaiseAndSetIfChanged(ref daysToKeepLogs, value);
				Config.DaysToKeepLogs = value;
			}
		}


		private bool loadWithSystemBoot = false;
		public bool LoadWithSystemBoot
		{
			get => loadWithSystemBoot;
			set
			{
				this.RaiseAndSetIfChanged(ref loadWithSystemBoot, value);
				Config.LoadWithSystemBoot = value;
			}
		}

		private bool startLoggersAutomatically = Config.StartLoggersAutomatically;
		public bool StartLoggersAutomatically
		{
			get => startLoggersAutomatically;
			set {
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
	}
}
