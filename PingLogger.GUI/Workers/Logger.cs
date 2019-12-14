using PingLogger.GUI.Models;
using Serilog;

namespace PingLogger.GUI.Workers
{
	public static class Logger
	{
		public static readonly ILogger Log;
		static Logger()
		{
#if DEBUG
			Log = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.Enrich.With(new ThreadIdEnricher())
				.WriteTo.File(
				"PingLogger.log", 
				restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose, 
				shared: true,
				outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level}] ({ThreadId}) {Message:lj}{NewLine}{Exception}")
				.CreateLogger();
#endif
		}

		public static void Debug(string text)
		{
#if DEBUG
			Log.Debug(text);
#endif
		}
	}
}
