using PingLogger.Models;
using Serilog;
using System;

namespace PingLogger.Workers
{
	/*
public static class Logger
{
	public static readonly ILogger Log;
	static Logger()
	{
		Log = new LoggerConfiguration()
#if DEBUG
			.MinimumLevel.Verbose()
#endif
			.Enrich.With(new ThreadIdEnricher())
			.WriteTo.File(
			"./PingLogger-.log",
			restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
			shared: true,
			outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level}] ({ThreadId}) {Message:lj}{NewLine}{Exception}",
			flushToDiskInterval: TimeSpan.FromSeconds(1),
			rollingInterval: RollingInterval.Day,
			retainedFileCountLimit: 2
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
	
}
	*/
}
