using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using PingLogger.GUI.Models;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Newtonsoft.Json;

namespace PingLogger.GUI.Workers
{
	public static class Config
	{
		private static string hostsPath = "./hosts.json";
		private static string configPath = "./config.json";
		public static ObservableCollection<Host> Hosts { get; set; } = new ObservableCollection<Host>();
		private static bool InitialLoad = false;
		private static AppOptions Options { get; set; }
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
			if(!InitialLoad)
				SaveConfig();
		}
		private static void ReadConfig()
		{
			if(File.Exists(hostsPath))
			{
				InitialLoad = true;
				try
				{
					var fileContents = File.ReadAllText(hostsPath);
					Hosts = JsonConvert.DeserializeObject<ObservableCollection<Host>>(fileContents);
					InitialLoad = false;
				} catch
				{

				}
			} else
			{
				Hosts = new ObservableCollection<Host>();
			}
			if(File.Exists(configPath))
			{
				InitialLoad = true;
				try
				{
					var fileContents = File.ReadAllText(configPath);
					Options = JsonConvert.DeserializeObject<AppOptions>(fileContents);
				}
				catch {
					Options = new AppOptions();
				}
				InitialLoad = false;
			} else
			{
				Options = new AppOptions();
			}
		}
		private static void SaveConfig()
		{
			File.WriteAllText(hostsPath, JsonConvert.SerializeObject(Hosts, Formatting.Indented));
			File.WriteAllText(configPath, JsonConvert.SerializeObject(Options, Formatting.Indented));
		}
	}
}
