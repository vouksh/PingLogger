using PingLogger.Models;
using Serilog;
using System;
using System.IO;

namespace PingLogger.Workers
{
	public static class Logger
	{
		public static readonly ILogger Log;
		static Logger()
		{
			if (!Directory.Exists("./Logs"))
				Directory.CreateDirectory("./Logs");

			Log = new LoggerConfiguration()
#if DEBUG
				.MinimumLevel.Verbose()
#endif
				.Enrich.With(new ThreadIdEnricher())
				.WriteTo.RollingFile(
				"./Logs/PingLogger-{Date}.log",
				restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
				shared: true,
				outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level}] ({ThreadId}) {Message:lj}{NewLine}{Exception}",
				flushToDiskInterval: TimeSpan.FromSeconds(1)
				)
				.CreateLogger();
		}

		public static void Debug(string text)
		{
#if DEBUG
			Log.Debug(text);
#endif
		}

		public static void Info(string text) => Log.Information(text);
		public static void Error(string text) => Log.Error(text);
		public static void Fatal(string text) => Log.Fatal(text);
	}
}
