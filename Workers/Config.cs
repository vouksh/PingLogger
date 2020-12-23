﻿using PingLogger.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace PingLogger.Workers
{
	public static class Config
	{
		private static readonly object fileLock = new object();
		public static ObservableCollection<Host> Hosts { get; set; } = new ObservableCollection<Host>();
		private static bool InitialLoad = false;
		private static AppOptions Options { get; set; }
		private static readonly Timer saveTimer;

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

		public static string LastTempDir
		{
			get
			{
				return Options.LastTempDir;
			}
			set
			{
				Logger.Info($"Options.LastTempDir was changed from {Options.LastTempDir} to {value}");
				Options.LastTempDir = value;
				SaveConfig();
			}
		}

		public static bool IsInstalled
		{
			get 
			{
				return Options.IsInstalled;
			}
			set
			{
				Logger.Info($"Options.IsInstalled was changed from {Options.IsInstalled} to {value}");
				Options.IsInstalled = value;
				SaveConfig();
			}
		}
		
		public static string InstallerGUID
		{
			get
			{
				return Options.InstallerGUID;
			}
			set
			{
				Logger.Info($"Options.InstallerGUID was changed from {Options.InstallerGUID} to {value}");
				Options.InstallerGUID = value;
				SaveConfig();
			}
		}

		public static string LogSavePath
		{
			get
			{
				if(!Options.LogSavePath.EndsWith(Path.DirectorySeparatorChar))
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
				Logger.Info($"Options.LogSavePath was changed from {Options.LogSavePath} to {value}");
				SaveConfig();
			}
		}

		static Config()
		{
			if(saveTimer is null)
			{
				saveTimer = new Timer(1500)
				{
					AutoReset = false,
					Enabled = false
				};
				saveTimer.Elapsed += SaveTimer_Elapsed;
			}
			ReadConfig();
			Hosts.CollectionChanged += OptionsChanged;
		}

		private static void SaveTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			Logger.Debug("Waiting for config file lock..");
			lock (fileLock)
			{
				string dataPath = $"{Util.FileBasePath}/config.dat";
				Logger.Info("SaveConfig() Called");
				var hostData = JsonSerializer.Serialize(Hosts, new JsonSerializerOptions { WriteIndented = true });
				var configData = JsonSerializer.Serialize(Options, new JsonSerializerOptions { WriteIndented = true });
				var fileStream = File.Open(dataPath, FileMode.OpenOrCreate);
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
			saveTimer.Stop();
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
				string dataPath = $"{Util.FileBasePath}/config.dat";
				if (File.Exists(dataPath))
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
						Options = new AppOptions()
						{
							EnableAutoUpdate = !Util.AppIsClickOnce,
							LogSavePath = Util.FileBasePath + Path.DirectorySeparatorChar + "Logs"
						};
					}
					SaveConfig();
				}
			}
		}
		private static void SaveConfig()
		{
			saveTimer.Stop();
			saveTimer.Start();
		}
	}
}
