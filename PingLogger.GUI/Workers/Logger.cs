using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace PingLogger.GUI.Workers
{
	public static class Logger
	{
		public static readonly ILogger Log;
		static Logger()
		{
			Log = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.WriteTo.File("PingLogger.log", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose, shared: true)
				.CreateLogger();
		}

		public static void Debug(string text)
		{
			Log.Debug(text);
		}
	}
}
