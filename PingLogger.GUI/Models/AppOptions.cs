using PingLogger.GUI.Workers;
using System;
using System.Collections.Generic;
using System.Text;

namespace PingLogger.GUI.Models
{
	public class AppOptions
	{
		public bool LoadOnSystemBoot { get; set; } = false;
		public bool StartLoggersAutomatically { get; set; } = false;
		public int DaysToKeepLogs { get; set; } = 7;
	}
}
