using PingLogger.GUI.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text.Json;

namespace PingLogger.GUI.Workers
{
	public static class Config
	{
		private static readonly string hostsPath = "./hosts.json";
		private static readonly string configPath = "./config.json";
		public static ObservableCollection<Host> Hosts { get; set; } = new ObservableCollection<Host>();
		private static bool InitialLoad = false;
		private static AppOptions Options { get; set; }
		public static Theme Theme
		{
			get
			{
				return Options.Theme;
			}
			set
			{
				Options.Theme = value;
				MainWindow.CurrentApp.SetTheme(value);
				SaveConfig();
			}
		}
		public static int DaysToKeepLogs
		{
			get
			{
				return Options.DaysToKeepLogs;
			}
			set
			{
				Options.DaysToKeepLogs = value;
				SaveConfig();
			}
		}
		public static bool LoadWithWindows
		{
			get
			{
				return Options.LoadOnSystemBoot;
			}
			set
			{
				Options.LoadOnSystemBoot = value;
				SaveConfig();
			}
		}
		public static bool StartLoggersAutomatically
		{
			get
			{
				return Options.StartLoggersAutomatically;
			}
			set
			{
				Options.StartLoggersAutomatically = value;
				SaveConfig();
			}
		}
		static Config()
		{
			ReadConfig();
			Hosts.CollectionChanged += optionsChanged;
		}
		private static void optionsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (!InitialLoad)
				SaveConfig();
		}
		private static void ReadConfig()
		{
			if (File.Exists(hostsPath))
			{
				InitialLoad = true;
				var fileContents = File.ReadAllText(hostsPath);
				Hosts = JsonSerializer.Deserialize<ObservableCollection<Host>>(fileContents);
				InitialLoad = false;
			}
			else
			{
				Hosts = new ObservableCollection<Host>();
			}
			if (File.Exists(configPath))
			{
				InitialLoad = true;
				try
				{
					var fileContents = File.ReadAllText(configPath);
					Options = JsonSerializer.Deserialize<AppOptions>(fileContents);
				}
				catch
				{
					Options = new AppOptions();
				}
				InitialLoad = false;
			}
			else
			{
				Options = new AppOptions();
			}
		}
		private static void SaveConfig()
		{
			File.WriteAllText(hostsPath, JsonSerializer.Serialize(Hosts, new JsonSerializerOptions { WriteIndented = true }));
			File.WriteAllText(configPath, JsonSerializer.Serialize(Options, new JsonSerializerOptions { WriteIndented = true }));
		}
	}

	public enum Theme
	{
		Dark, Light
	}
}
