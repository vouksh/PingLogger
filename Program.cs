using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using System;
using Serilog;
using PingLogger.Models;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using System.Linq;

namespace PingLogger
{
	class Program
	{
		// Initialization code. Don't use any Avalonia, third-party APIs or any
		// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
		// yet and stuff might break.
		public static void Main(string[] args)
		{
			if (args.Any() && args[0] == "--installed")
				Workers.Config.IsInstalled = true;

			Log.Logger = new LoggerConfiguration()
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
				.WriteTo.Console(Serilog.Events.LogEventLevel.Verbose)
				.CreateLogger();
			try
			{
				BuildAvaloniaApp()
					.StartWithClassicDesktopLifetime(args);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Application-level error occurred.");
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		private static void AfterSetupCallback(AppBuilder appBuilder)
		{
			// Register icon provider(s)
			IconProvider.Register<FontAwesomeIconProvider>();
		}

		// Avalonia configuration, don't remove; also used by visual designer.
		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<App>()
				.AfterSetup(AfterSetupCallback)
				.UsePlatformDetect()
				.LogToTrace()
				.UseReactiveUI();
	}
}
