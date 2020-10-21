using System;

namespace PingLogger.Models
{
	public class AppOptions
	{
		public bool LoadOnSystemBoot { get; set; } = false;
		public bool StartLoggersAutomatically { get; set; } = false;
		public int DaysToKeepLogs { get; set; } = 7;
		public Theme Theme { get; set; } = Theme.Auto;
		public bool StartProgramMinimized { get; set; } = false;
		public bool WindowExpanded { get; set; } = false;
		public bool AppWasUpdated { get; set; } = false;
		public DateTime LastUpdated { get; set; } = DateTime.Now.AddDays(-1);
		public bool EnableAutoUpdate { get; set; } = true;
	}
	public enum Theme
	{
		Auto = 0,
		Light = 1,
		Dark = 2
	}
}
