using PingLogger.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PingLogger.Workers
{
	public static class Config
	{
		private static readonly object fileLock = new object();
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
				Logger.Info($"Options.Theme was changed from {Options.Theme} to {value}");
				Options.Theme = value;
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
				Logger.Info($"Options.DaysToKeepLogs was changed from {Options.DaysToKeepLogs} to {value}");
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
				Logger.Info($"Options.LoadOnSystemBoot was changed from {Options.LoadOnSystemBoot} to {value}");
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
				Logger.Info($"Options.StartLoggersAutomatically was changed from {Options.StartLoggersAutomatically} to {value}");
				Options.StartLoggersAutomatically = value;
				SaveConfig();
			}
		}
		public static bool StartApplicationMinimized
		{
			get
			{
				return Options.StartProgramMinimized;
			}
			set
			{
				Logger.Info($"Options.StartProgramMinimized was changed from {Options.StartProgramMinimized} to {value}");
				Options.StartProgramMinimized = value;
				SaveConfig();
			}
		}

		public static bool WindowExpanded
		{
			get
			{
				return Options.WindowExpanded;
			}
			set
			{
				Logger.Info($"Options.WindowExpanded was changed from {Options.WindowExpanded} to {value}");
				Options.WindowExpanded = value;
				SaveConfig();
			}
		}

		public static bool AppWasUpdated
		{
			get
			{
				return Options.AppWasUpdated;
			}
			set
			{
				Logger.Info($"Options.AppWasUpdated was changed from {Options.AppWasUpdated} to {value}");
				Options.AppWasUpdated = value;
				SaveConfig();
			}
		}

		public static DateTime UpdateLastChecked
		{
			get
			{
				return Options.UpdateLastChecked;
			}
			set
			{
				Logger.Info($"Options.UpdateLastChecked was changed from {Options.UpdateLastChecked} to {value}");
				Options.UpdateLastChecked = value;
				SaveConfig();
			}
		}

		public static bool EnableAutoUpdate
		{
			get
			{
				return Options.EnableAutoUpdate;
			}
			set
			{
				Logger.Info($"Options.EnableAutoUpdate was changed from {Options.EnableAutoUpdate} to {value}");
				Options.EnableAutoUpdate = value;
				SaveConfig();
			}
		}

		public static int LastSelectedTab
		{
			get
			{
				return Options.LastSelectedTab;
			}
			set
			{
				Logger.Info($"Options.LastSelectedTab was changed from {Options.LastSelectedTab} to {value}");
				Options.LastSelectedTab = value;
				SaveConfig();
			}
		}

		static Config()
		{
			ReadConfig();
			Hosts.CollectionChanged += OptionsChanged;
		}
		private static void OptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (!InitialLoad)
				SaveConfig();
		}

		// Put this in for backward compatibility.
		private static bool CheckForOldConfig()
		{
			Logger.Info("Checking for old configuration files");
			bool oldConfigExists = false;
			if (File.Exists("./config.json"))
			{
				Logger.Info("config.json found, pulling data and deleting it");
				oldConfigExists = true;
				var fileContents = File.ReadAllText("./config.json");
				Options = JsonSerializer.Deserialize<AppOptions>(fileContents);
				File.Delete("./config.json");
			}
			if (File.Exists("./hosts.json"))
			{
				Logger.Info("hosts.json found, pulling data and deleting it");
				oldConfigExists = true;
				var fileContents = File.ReadAllText("./hosts.json");
				Hosts = JsonSerializer.Deserialize<ObservableCollection<Host>>(fileContents);
				File.Delete("./hosts.json");
			}
			return oldConfigExists;
		}

		private static void ReadConfig()
		{
			if (Options == null)
			{
				string dataPath = "./config.dat";
				if (File.Exists("./config.dat"))
				{
					Logger.Debug("Waiting for config file lock..");
					lock (fileLock)
					{
						Logger.Info("Found existing config.dat, reading file");
						InitialLoad = true;

						using var archive = ZipFile.OpenRead(dataPath);

						Logger.Info("Reading hosts configuration");
						using StreamReader hostReader = new StreamReader(archive.GetEntry("hosts.json").Open());
						Hosts = JsonSerializer.Deserialize<ObservableCollection<Host>>(hostReader.ReadToEnd());
						hostReader.Close();

						Logger.Info("Reading application configuration");
						using var configReader = new StreamReader(archive.GetEntry("config.json").Open());
						Options = JsonSerializer.Deserialize<AppOptions>(configReader.ReadToEnd());
						configReader.Close();

						InitialLoad = false;
					}
				}
				else
				{
					Logger.Info("Did not find existing config.dat");
					if (!CheckForOldConfig())
					{
						Logger.Info("Old configuration not found, starting out fresh");
						Hosts = new ObservableCollection<Host>();
						Options = new AppOptions();
					}
					SaveConfig();
				}
			}
		}
		private static async void SaveConfig()
		{
			await Task.Run(() =>
			{
				Logger.Debug("Waiting for config file lock..");
				lock (fileLock)
				{
					Logger.Info("SaveConfig() Called");
					var hostData = JsonSerializer.Serialize(Hosts, new JsonSerializerOptions { WriteIndented = true });
					var configData = JsonSerializer.Serialize(Options, new JsonSerializerOptions { WriteIndented = true });
					var fileStream = File.Open("./config.dat", FileMode.OpenOrCreate);
					Logger.Info("config.dat opened");
					using var archive = new ZipArchive(fileStream, ZipArchiveMode.Update);
					if (archive.Entries.Count > 0)
					{
						Logger.Info("Existing configuration found, overwriting");
						Logger.Info("Saving host configuration");
						var hostEntry = archive.GetEntry("hosts.json");
						using var hostEntryStream = hostEntry.Open();
						hostEntryStream.SetLength(hostData.Length);
						using StreamWriter hostWriter = new StreamWriter(hostEntryStream);
						hostWriter.Write(hostData);

						Logger.Info("Saving application configuration");
						var configEntry = archive.GetEntry("config.json");
						using var configStream = configEntry.Open();
						configStream.SetLength(configData.Length);
						using StreamWriter configWriter = new StreamWriter(configStream);
						configWriter.Write(configData);
					}
					else
					{
						Logger.Info("Saving host configuration");
						var hostEntry = archive.CreateEntry("hosts.json");
						using StreamWriter hostWriter = new StreamWriter(hostEntry.Open());
						hostWriter.Write(hostData);

						Logger.Info("Saving application configuration");
						var configEntry = archive.CreateEntry("config.json");
						using StreamWriter configWriter = new StreamWriter(configEntry.Open());
						configWriter.Write(configData);
					}
					Logger.Info("Done saving config.dat");
				}
			});
		}
	}
}
