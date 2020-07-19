using PingLogger.GUI.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace PingLogger.GUI.Workers
{
	public static class Config
	{
		public static ObservableCollection<Host> Hosts { get; set; } = new ObservableCollection<Host>();
		private static bool InitialLoad = false;
		private static AppOptions Options { get; set; }

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

		// Put this in for backward compatibility.
		private static bool CheckForOldConfig()
		{
			bool oldConfigExists = false;
			if (File.Exists("./config.json"))
			{
				oldConfigExists = true;
				var fileContents = File.ReadAllText("./config.json");
				Options = JsonSerializer.Deserialize<AppOptions>(fileContents);
				File.Delete("./config.json");
			}
			if (File.Exists("./hosts.json"))
			{
				oldConfigExists = true;
				var fileContents = File.ReadAllText("./hosts.json");
				Hosts = JsonSerializer.Deserialize<ObservableCollection<Host>>(fileContents);
				File.Delete("./hosts.json");
			}
			return oldConfigExists;
		}

		private static void ReadConfig()
		{
			string dataPath = "./config.dat";
			if (File.Exists("./config.dat"))
			{
				InitialLoad = true;
				using var archive = ZipFile.OpenRead(dataPath);
				foreach (var entry in archive.Entries)
				{
					using StreamReader streamReader = new StreamReader(entry.Open());
					var fileContents = streamReader.ReadToEnd();
					switch (entry.FullName)
					{
						case "hosts.json":
							Hosts = JsonSerializer.Deserialize<ObservableCollection<Host>>(fileContents);
							break;
						case "config.json":
							Options = JsonSerializer.Deserialize<AppOptions>(fileContents);
							break;
					}
				}
				InitialLoad = false;
			}
			else
			{
				if (!CheckForOldConfig())
				{
					Hosts = new ObservableCollection<Host>();
					Options = new AppOptions();
				}
				SaveConfig();
			}
		}
		private static void SaveConfig()
		{
			var hostData = JsonSerializer.Serialize(Hosts, new JsonSerializerOptions { WriteIndented = true });
			var configData = JsonSerializer.Serialize(Options, new JsonSerializerOptions { WriteIndented = true });
			var fileStream = File.Open("./config.dat", FileMode.OpenOrCreate);
			using var archive = new ZipArchive(fileStream, ZipArchiveMode.Update);
			if (archive.Entries.Count > 0)
			{
				var hostEntry = archive.GetEntry("hosts.json");
				hostEntry.Delete();
				hostEntry = archive.CreateEntry("hosts.json");
				using StreamWriter hostWriter = new StreamWriter(hostEntry.Open());
				foreach (var line in hostData.Split(Environment.NewLine))
				{
					hostWriter.WriteLine(line);
				}

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
				var hostEntry = archive.CreateEntry("hosts.json");
				using StreamWriter hostWriter = new StreamWriter(hostEntry.Open());
				foreach (var line in hostData.Split(Environment.NewLine))
				{
					hostWriter.WriteLine(line);
				}

				var configEntry = archive.CreateEntry("config.json");
				using StreamWriter configWriter = new StreamWriter(configEntry.Open());
				foreach (var line in configData.Split(Environment.NewLine))
				{
					configWriter.WriteLine(line);
				}
			}
		}
	}

	public enum Theme
	{
		Dark, Light
	}
}
