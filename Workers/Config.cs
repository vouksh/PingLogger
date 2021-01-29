using PingLogger.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Timers;
using Serilog;

namespace PingLogger.Workers
{
	public static class Config
	{
		private static readonly object _fileLock = new();
		public static ObservableCollection<Host> Hosts { get; set; } = new();
		private static bool _initialLoad;
		private static AppOptions Options { get; set; }
		private static readonly Timer _saveTimer;

		public static Theme Theme
		{
			get => Options.Theme;
			set
			{
				Log.Information($"Options.Theme was changed from {Options.Theme} to {value}");
				Options.Theme = value;
				SaveConfig();
			}
		}

		public static int DaysToKeepLogs
		{
			get => Options.DaysToKeepLogs;
			set
			{
				Log.Information($"Options.DaysToKeepLogs was changed from {Options.DaysToKeepLogs} to {value}");
				Options.DaysToKeepLogs = value;
				SaveConfig();
			}
		}
		public static bool LoadWithSystemBoot
		{
			get => Options.LoadOnSystemBoot;
			set
			{
				Log.Information($"Options.LoadOnSystemBoot was changed from {Options.LoadOnSystemBoot} to {value}");
				Options.LoadOnSystemBoot = value;
				SaveConfig();
			}
		}
		public static bool StartLoggersAutomatically
		{
			get => Options.StartLoggersAutomatically;
			set
			{
				Log.Information($"Options.StartLoggersAutomatically was changed from {Options.StartLoggersAutomatically} to {value}");
				Options.StartLoggersAutomatically = value;
				SaveConfig();
			}
		}
		public static bool StartApplicationMinimized
		{
			get => Options.StartProgramMinimized;
			set
			{
				Log.Information($"Options.StartProgramMinimized was changed from {Options.StartProgramMinimized} to {value}");
				Options.StartProgramMinimized = value;
				SaveConfig();
			}
		}

		public static bool WindowExpanded
		{
			get => Options.WindowExpanded;
			set
			{
				Log.Information($"Options.WindowExpanded was changed from {Options.WindowExpanded} to {value}");
				Options.WindowExpanded = value;
				SaveConfig();
			}
		}

		public static bool AppWasUpdated
		{
			get => Options.AppWasUpdated;
			set
			{
				Log.Information($"Options.AppWasUpdated was changed from {Options.AppWasUpdated} to {value}");
				Options.AppWasUpdated = value;
				SaveConfig();
			}
		}

		public static DateTime UpdateLastChecked
		{
			get => Options.UpdateLastChecked;
			set
			{
				Log.Information($"Options.UpdateLastChecked was changed from {Options.UpdateLastChecked} to {value}");
				Options.UpdateLastChecked = value;
				SaveConfig();
			}
		}

		public static bool EnableAutoUpdate
		{
			get => Options.EnableAutoUpdate;
			set
			{
				Log.Information($"Options.EnableAutoUpdate was changed from {Options.EnableAutoUpdate} to {value}");
				Options.EnableAutoUpdate = value;
				SaveConfig();
			}
		}

		public static int LastSelectedTab
		{
			get => Options.LastSelectedTab;
			set
			{
				Log.Information($"Options.LastSelectedTab was changed from {Options.LastSelectedTab} to {value}");
				Options.LastSelectedTab = value;
				SaveConfig();
			}
		}

		public static string LastTempDir
		{
			get => Options.LastTempDir;
			set
			{
				Log.Information($"Options.LastTempDir was changed from {Options.LastTempDir} to {value}");
				Options.LastTempDir = value;
				SaveConfig();
			}
		}

		public static bool IsInstalled
		{
			get => Options.IsInstalled;
			set
			{
				Log.Information($"Options.IsInstalled was changed from {Options.IsInstalled} to {value}");
				Options.IsInstalled = value;
				SaveConfig();
			}
		}

		// ReSharper disable once InconsistentNaming
		public static string InstallerGUID
		{
			get => Options.InstallerGUID;
			set
			{
				Log.Information($"Options.InstallerGUID was changed from {Options.InstallerGUID} to {value}");
				Options.InstallerGUID = value;
				SaveConfig();
			}
		}

		public static string LogSavePath
		{
			get
			{
				if (!Options.LogSavePath.EndsWith(Path.DirectorySeparatorChar))
				{
					Options.LogSavePath += Path.DirectorySeparatorChar;
				}
				return Options.LogSavePath;
			}
			set
			{
				if (!value.EndsWith(Path.DirectorySeparatorChar))
				{
					Options.LogSavePath = value + Path.DirectorySeparatorChar;
				}
				else
				{
					Options.LogSavePath = value;
				}
				Log.Information($"Options.LogSavePath was changed from {Options.LogSavePath} to {value}");
				SaveConfig();
			}
		}

		static Config()
		{
			if (_saveTimer is null)
			{
				_saveTimer = new Timer(1500)
				{
					AutoReset = false,
					Enabled = false
				};
				_saveTimer.Elapsed += SaveTimer_Elapsed;
			}
			ReadConfig();
			Hosts.CollectionChanged += OptionsChanged;
		}

		private static void SaveTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			Log.Debug("Waiting for config file lock..");
			lock (_fileLock)
			{
				string dataPath = $"{Utils.FileBasePath}/config.dat";
				Log.Information("SaveConfig() Called");
				var hostData = JsonSerializer.Serialize(Hosts, new JsonSerializerOptions { WriteIndented = true });
				var configData = JsonSerializer.Serialize(Options, new JsonSerializerOptions { WriteIndented = true });
				var fileStream = File.Open(dataPath, FileMode.OpenOrCreate);
				Log.Information("config.dat opened");
				using var archive = new ZipArchive(fileStream, ZipArchiveMode.Update);
				if (archive.Entries.Count > 0)
				{
					Log.Information("Existing configuration found, overwriting");
					Log.Information("Saving host configuration");
					var hostEntry = archive.GetEntry("hosts.json");

					if (hostEntry != null)
					{
						using var hostEntryStream = hostEntry.Open();
						hostEntryStream.SetLength(hostData.Length);
						using StreamWriter hostWriter = new StreamWriter(hostEntryStream);
						hostWriter.Write(hostData);
					}

					Log.Information("Saving application configuration");
					var configEntry = archive.GetEntry("config.json");

					if (configEntry != null)
					{
						using var configStream = configEntry.Open();
						configStream.SetLength(configData.Length);
						using StreamWriter configWriter = new StreamWriter(configStream);
						configWriter.Write(configData);
					}
				}
				else
				{
					Log.Information("Saving host configuration");
					var hostEntry = archive.CreateEntry("hosts.json");
					using StreamWriter hostWriter = new StreamWriter(hostEntry.Open());
					hostWriter.Write(hostData);

					Log.Information("Saving application configuration");
					var configEntry = archive.CreateEntry("config.json");
					using StreamWriter configWriter = new StreamWriter(configEntry.Open());
					configWriter.Write(configData);
				}
				Log.Information("Done saving config.dat");
			}
			_saveTimer.Stop();
		}

		private static void OptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (!_initialLoad)
				SaveConfig();
		}

		// Put this in for backward compatibility.
		private static bool CheckForOldConfig()
		{
			Log.Information("Checking for old configuration files");
			bool oldConfigExists = false;
			if (File.Exists("./config.json"))
			{
				Log.Information("config.json found, pulling data and deleting it");
				oldConfigExists = true;
				var fileContents = File.ReadAllText("./config.json");
				Options = JsonSerializer.Deserialize<AppOptions>(fileContents);
				File.Delete("./config.json");
			}
			if (File.Exists("./hosts.json"))
			{
				Log.Information("hosts.json found, pulling data and deleting it");
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
				string dataPath = $"{Utils.FileBasePath}/config.dat";
				if (File.Exists(dataPath))
				{
					Log.Debug("Waiting for config file lock..");
					lock (_fileLock)
					{
						Log.Information("Found existing config.dat, reading file");
						_initialLoad = true;

						using var archive = ZipFile.OpenRead(dataPath);

						Log.Information("Reading hosts configuration");
						using StreamReader hostReader = new StreamReader(archive.GetEntry("hosts.json")?.Open()!);
						Hosts = JsonSerializer.Deserialize<ObservableCollection<Host>>(hostReader.ReadToEnd());
						hostReader.Close();

						Log.Information("Reading application configuration");
						using var configReader = new StreamReader(archive.GetEntry("config.json")?.Open()!);
						Options = JsonSerializer.Deserialize<AppOptions>(configReader.ReadToEnd());
						configReader.Close();

						_initialLoad = false;
					}
				}
				else
				{
					Log.Information("Did not find existing config.dat");
					if (!CheckForOldConfig())
					{
						Log.Information("Old configuration not found, starting out fresh");
						Hosts = new ObservableCollection<Host>();
						Options = new AppOptions()
						{
#if Windows
							EnableAutoUpdate = !Utils.Win.AppIsClickOnce,
#endif
							LogSavePath = Utils.FileBasePath + Path.DirectorySeparatorChar + "Logs"
						};
					}
					SaveConfig();
				}
			}
		}
		private static void SaveConfig()
		{
			_saveTimer.Stop();
			_saveTimer.Start();
		}
	}
}
