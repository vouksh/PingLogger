using PingLogger.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
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
				Logger.Info("Options.Theme changed");
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
				Logger.Info("Options.DaysToKeepLogs changed");
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
				Logger.Info("Options.LoadWithWindows changed");
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
				Logger.Info("Options.StartLoggersAutomatically changed");
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
				Logger.Info("Options.StartProgramMinimized changed");
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
				Options.AppWasUpdated = value;
				SaveConfig();
			}
		}

		public static DateTime LastUpdated
		{
			get
			{
				return Options.LastUpdated;
			}
			set
			{
				Options.LastUpdated = value;
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
				Logger.Info("Options.EnableAutoUpdate was changed");
				Options.EnableAutoUpdate = value;
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
				lock (fileLock)
				{
					string dataPath = "./config.dat";
					if (File.Exists("./config.dat"))
					{
						Logger.Info("Found existing config.dat, reading file");
						InitialLoad = true;
						using var archive = ZipFile.OpenRead(dataPath);
						foreach (var entry in archive.Entries)
						{
							using StreamReader streamReader = new StreamReader(entry.Open());
							var fileContents = streamReader.ReadToEnd();
							switch (entry.FullName)
							{
								case "hosts.json":
									Logger.Info("Reading hosts configuration");
									Hosts = JsonSerializer.Deserialize<ObservableCollection<Host>>(fileContents);
									break;
								case "config.json":
									Logger.Info("Reading application configuration");
									Options = JsonSerializer.Deserialize<AppOptions>(fileContents);
									break;
							}
						}
						InitialLoad = false;
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
		}
		private static async void SaveConfig()
		{
			await Task.Run(() =>
			{
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
						hostEntry.Delete();
						hostEntry = archive.CreateEntry("hosts.json");
						using StreamWriter hostWriter = new StreamWriter(hostEntry.Open());
						foreach (var line in hostData.Split(Environment.NewLine))
						{
							hostWriter.WriteLine(line);
						}

						Logger.Info("Saving application configuration");
						var configEntry = archive.GetEntry("config.json");
						configEntry.Delete();
						configEntry = archive.CreateEntry("config.json");
						using StreamWriter configWriter = new StreamWriter(configEntry.Open());
						foreach (var line in configData.Split(Environment.NewLine))
						{
							configWriter.WriteLine(line);
						}
					}
					else
					{
						Logger.Info("Saving host configuration");
						var hostEntry = archive.CreateEntry("hosts.json");
						using StreamWriter hostWriter = new StreamWriter(hostEntry.Open());
						foreach (var line in hostData.Split(Environment.NewLine))
						{
							hostWriter.WriteLine(line);
						}

						Logger.Info("Saving application configuration");
						var configEntry = archive.CreateEntry("config.json");
						using StreamWriter configWriter = new StreamWriter(configEntry.Open());
						foreach (var line in configData.Split(Environment.NewLine))
						{
							configWriter.WriteLine(line);
						}
					}
					Logger.Info("Done saving config.dat");
				}
			});
		}
	}
}
