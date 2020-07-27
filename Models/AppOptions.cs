namespace PingLogger.GUI.Models
{
	public class AppOptions
	{
		public bool LoadOnSystemBoot { get; set; } = false;
		public bool StartLoggersAutomatically { get; set; } = false;
		public int DaysToKeepLogs { get; set; } = 7;
		public Theme Theme { get; set; } = Theme.Auto;
	}
	public enum Theme
	{
		Auto = 0,
		Light = 1,
		Dark = 2
	}
}
