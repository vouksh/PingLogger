using System;

namespace PingLogger.Models
{
	public record AppOptions
	{
		public bool LoadOnSystemBoot { get; set; } = false;
		public bool StartLoggersAutomatically { get; set; } = false;
		public int DaysToKeepLogs { get; set; } = 7;
		public Theme Theme { get; set; } = Theme.Auto;
		public bool StartProgramMinimized { get; set; } = false;
		public bool WindowExpanded { get; set; } = false;
		public bool AppWasUpdated { get; set; } = false;
		public DateTime UpdateLastChecked { get; set; } = DateTime.Now.AddDays(-1);
		public bool EnableAutoUpdate { get; set; } = true;
		public int LastSelectedTab { get; set; } = 0;
		public string LastTempDir { get; set; } = string.Empty;
		public bool IsInstalled { get; set; } = false;
		public string InstallerGUID { get; set; } = string.Empty;
	}
	public enum Theme
	{
		Auto = 0,
		Light = 1,
		Dark = 2
	}
}
