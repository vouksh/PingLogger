using PingLogger.Models;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PingLogger.Workers
{
	public class Pinger : IDisposable
	{
		private readonly Host _host;
		public bool Running;
		private bool _stopping;
		private readonly ILogger _logger;
		private Thread _runThread;
		private readonly Ping _pingSender = new();
		public BlockingCollection<Reply> Replies = new();
		private bool _dontFragment = true;
		/// <summary>
		/// This class is where all of the actual pinging work is done.
		/// I creates a thread that loops until canceled.
		/// The thread fires an event to do the actual pinging. 
		/// </summary>
		/// <param name="host">The host that will be pinged.</param>
		public Pinger(Host host)
		{
			_host = host;
			var hostLogPath = $"{Config.LogSavePath}{_host.HostName}";
			if (!Directory.Exists(Config.LogSavePath))
				Directory.CreateDirectory(Config.LogSavePath);
			if (!Directory.Exists(hostLogPath))
				Directory.CreateDirectory(hostLogPath);
			const string outputTemp = "[{Timestamp:HH:mm:ss} {Level:u4}] {Message:lj}{NewLine}{Exception}";
			const string errorOutputTemp = "[{Timestamp:HH:mm:ss} {Level:u5}] {Message:lj}{NewLine}{Exception}";
			var initialPath = $"{hostLogPath}{Path.DirectorySeparatorChar}{_host.HostName}";
			var filePath = $"{initialPath}-.log";
			var errorPathName = $"{initialPath}-Errors-.log";
			var warnPathName = $"{initialPath}-Warnings-.log";

#if DEBUG
			var debugOutputTemp = "[{Timestamp:HH:mm:ss.fff} {Level}] ({ThreadId}) {Message:lj}{NewLine}{Exception}";
			var debugPathName = $"{initialPath}-Debug-.log";
#endif
			_logger = new LoggerConfiguration()
#if DEBUG
				.Enrich.With(new ThreadIdEnricher())
				.MinimumLevel.Verbose()
#endif
				.WriteTo.Logger(
					l => l.Filter.ByIncludingOnly(e => e.Level == Serilog.Events.LogEventLevel.Error)
					.WriteTo.File(
						errorPathName,
						restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
						retainedFileCountLimit: Config.DaysToKeepLogs,
						shared: true,
						outputTemplate: errorOutputTemp,
						flushToDiskInterval: TimeSpan.FromSeconds(2), //Added this because I noticed that it wasn't consistently flushing to disk at a good interval.
						rollingInterval: RollingInterval.Day
						)
					)
				.WriteTo.Logger(
					l => l.Filter.ByIncludingOnly(e => e.Level == Serilog.Events.LogEventLevel.Warning)
					.WriteTo.File(
						warnPathName,
						shared: true,
						retainedFileCountLimit: Config.DaysToKeepLogs,
						outputTemplate: outputTemp,
						flushToDiskInterval: TimeSpan.FromSeconds(2),
						rollingInterval: RollingInterval.Day
						)
					)
#if DEBUG
					.WriteTo.Logger(
					l => l.Filter.ByIncludingOnly(e => e.Level == Serilog.Events.LogEventLevel.Debug)
					.WriteTo.File(
						debugPathName,
						shared: true,
						retainedFileCountLimit: Config.DaysToKeepLogs,
						restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug,
						outputTemplate: debugOutputTemp,
						flushToDiskInterval: TimeSpan.FromSeconds(2),
						rollingInterval: RollingInterval.Day
						)
					)
#endif
				.WriteTo.File(
						filePath,
						shared: true,
						outputTemplate: outputTemp,
						retainedFileCountLimit: Config.DaysToKeepLogs,
						restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
						flushToDiskInterval: TimeSpan.FromSeconds(2),
						rollingInterval: RollingInterval.Day
					)
				.CreateLogger();

			//Check to make sure the packet size isn't too large. Don't want to abuse this.
			if (_host.PacketSize > 65500)
			{
				_logger.Error("Packet size too large. Resetting to 65500 bytes");
				_host.PacketSize = 65500;
			}
			//Make sure that the interval isn't too short. If you set it to be too frequent, it might get flagged as DDoS attack.
			if (_host.Interval < 500)
			{
				_logger.Error("Interval too short. Setting to 500ms");
				_host.Interval = 500;
			}
			//Verify that the IP stored in the settings file matches what it currently resolves to.
			//Mostly in cases of local network and DHCP
			_logger.Information("Verifying IP address of hostname is current.");
			foreach (var ip in Dns.GetHostAddresses(_host.HostName))
			{
				_logger.Debug($"IP: {ip}");
				if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
				{
					if (ip.ToString() == _host.IP)
					{
						_logger.Information("IP matches. Continuing");
					}
					else
					{
						_logger.Warning("IP address does not match last stored. Saving new IP address");
						_host.IP = ip.ToString();
					}
					break;
				}
			}
		}
		/// <summary>
		/// Get the host info from this class. In case we had to make modifications to it on startup.
		/// </summary>
		/// <returns>Most recent Host class</returns>
		public Host UpdateHost()
		{
			_logger.Debug("UpdateHost() Requested");
			return _host;
		}
		/// <summary>
		/// Starts the primary thread where all the work is done. 
		/// Outputs all of the hosts info so that if we're starting it up, the user can see if anything is wrong, allowing them time to make changes.
		/// </summary>
		public void Start()
		{
			_logger.Debug("Start()");
			_runThread = new Thread(StartLogging)
			{
				Name = $"{_host.HostName}-MainThread"
			};
			_logger.Information($"Starting ping logging for host with settings:\n{_host}");
			_logger.Debug("RunThread.Start()");
			_runThread.Start();
		}
		/// <summary>
		/// Primary thread that is spun up. Doesn't do the actual pinging, that's handled by an event.
		/// </summary>
		private void StartLogging()
		{
			_logger.Debug("StartLogging() Called.");
			_pingSender.PingCompleted += SendPing;
			AutoResetEvent waiter = new AutoResetEvent(false);

			//Generate a string that's as long as the packet size. 
			//This is outside of the loop, so it's going to be the same while the thread is running.
			//If it's restarted, we generate a new string. 
			string data = Utils.RandomString(_host.PacketSize);
			_logger.Debug($"Data string: {data}");

			byte[] buffer = Encoding.ASCII.GetBytes(data);

			Running = true;
			while (Running)
			{
				PingOptions options = new PingOptions
				{
					DontFragment = _dontFragment,
					Ttl = 128
				};
				_logger.Debug($"Running: {Running}");
				_logger.Debug($"stopping: {_stopping}");
				if (_stopping)
				{
					Running = false;
				}
				else
				{
					_logger.Debug("Sending Async Ping");
					try
					{
						var sw = new Stopwatch();
						sw.Start();
						_logger.Debug("Stopwatch Started");
						_pingSender.SendAsync(_host.IP, _host.Timeout, buffer, options, waiter);
						waiter.WaitOne();
						sw.Stop();
						_logger.Debug($"Stopwatch.ElapsedMilliseconds: {sw.ElapsedMilliseconds}ms");
						Thread.Sleep(_host.Interval);
						_logger.Debug($"Waited {_host.Interval}ms");
					}
					catch
					{
						_logger.Debug("Thread Interrupted");
					}
				}
			}
			_logger.Debug("PingSender.Dispose()");
			_pingSender.Dispose();
			_logger.Debug("SendPing() Ended");
		}
		/// <summary>
		/// Change the 'stopping' variable to true so that the thread can dispose of the pingSender properly, then allows the thread to exit safely.
		/// </summary>
		public void Stop()
		{
			if (Running)
			{
				_stopping = true;
				_logger.Information("Stopping ping logger for host {0} ({1})", _host.HostName, _host.IP);
				_logger.Debug("SendAsyncCancel()");
				_pingSender.SendAsyncCancel();
				try
				{
					_runThread.Interrupt();
				}
				catch
				{
					_logger.Debug("Thread Interrupted");
				}
			}
		}
		/// <summary>
		/// The main workhorse of the class. 
		/// This event is called with every ping 
		/// Most of the options will never be hit, but I included them as a just-in-case measure.
		/// </summary>
		private void SendPing(object sender, PingCompletedEventArgs e)
		{
			Thread.CurrentThread.Name = $"{_host.HostName}-PingThread-{Thread.CurrentThread.ManagedThreadId}";
			_logger.Debug("SendPing() called");
			if (e.Cancelled)
			{
				_logger.Information("Ping canceled.");
				((AutoResetEvent)e.UserState)?.Set();
				return;
			}
			if (e.Error != null)
			{
				_logger.Information("Ping canceled.");
				//Logger.Debug(e.Error.ToString());
				((AutoResetEvent)e.UserState)?.Set();
				return;
			}
			var reply = e.Reply;
			bool timedOut = false;
			bool success = false;

			if (reply != null)
			{
				int ttl = 0;
				if (reply.Options is not null)
					ttl = reply.Options.Ttl;
				switch (reply.Status)
				{
					case IPStatus.Success:
						_logger.Debug("Ping Success");

						//This check is because of a bug/problem with Ping where, if using a small timeout threshold, the ping reply can still be received.
						//See https://docs.microsoft.com/en-us/dotnet/api/system.net.networkinformation.ping.send?redirectedfrom=MSDN&view=netcore-3.1#System_Net_NetworkInformation_Ping_Send_System_String_System_Int32_System_Byte___
						if (reply.RoundtripTime < _host.Timeout)
						{
							//Ping was successful. Check to see if the round trip time was greater than the threshold.
							//If it is, then we change the output to be a warning, making it easy to track down in the log files.
							if (reply.RoundtripTime >= _host.Threshold)
							{
								_logger.Warning($"Pinged {_host.HostName} ({_host.IP}) RoundTrip: {reply.RoundtripTime}ms (Over Threshold) TTL: {ttl}");
								success = true;
							}
							else
							{
								_logger.Information($"Pinged {_host.HostName} ({_host.IP}) RoundTrip: {reply.RoundtripTime}ms TTL: {ttl}");
								success = true;
							}
						}
						else
						{
							_logger.Debug("Ping Reply Success, but roundtrip time exceeds timeout. Marking it as a timeout.");

							_logger.Error($"Ping timed out to host {_host.HostName} ({_host.IP}). Timeout is {_host.Timeout}ms");
							timedOut = true;
						}

						break;
					//These indicate that there was a problem somewhere along the way. 
					case IPStatus.DestinationHostUnreachable:
						_logger.Error("Destination host unreachable.");

						break;
					case IPStatus.DestinationNetworkUnreachable:
						_logger.Error("Destination network unreachable.");

						break;
					case IPStatus.DestinationUnreachable:
						_logger.Error("Destination unreachable, cause unknown.");

						break;
					case IPStatus.HardwareError:
						_logger.Error("Ping failed due to hardware.");

						break;
					case IPStatus.TimedOut:
						_logger.Debug("Ping Timed Out");

						_logger.Error($"Ping timed out to host {_host.HostName} ({_host.IP}). Timeout is {_host.Timeout}ms");
						timedOut = true;

						break;
					case IPStatus.PacketTooBig:
						_logger.Debug("Packet too large. Turning on fragmentation");
						_logger.Error("Packet size too large, turning on fragmentation.");
						timedOut = true;
						_dontFragment = false;

						break;
				}

				var logReply = new Reply
				{
					Host = _host,
					DateTime = DateTime.Now,
					Ttl = ttl,
					RoundTrip = reply.RoundtripTime,
					TimedOut = timedOut,
					Succeeded = success
				};
				Replies.Add(logReply);
			}

			_logger.Debug("Ping Ended");
			((AutoResetEvent)e.UserState)?.Set();
		}

		public async Task<(long RoundTrip, IPStatus Status)> GetSingleRoundTrip(IPAddress address, int ttl)
		{
			string data = Utils.RandomString(_host.PacketSize);
			byte[] buffer = Encoding.ASCII.GetBytes(data);
			using var pinger = new Ping();
			var pingOpts = new PingOptions(ttl, true);
			_logger.Information($"Single Ping sent to {address}");
			var reply = await pinger.SendPingAsync(address, _host.Timeout, buffer, pingOpts);
			_logger.Information($"Single Ping Reply Status: {reply.Status}");
			_logger.Information($"Single Ping Reply RoundTrip: {reply.RoundtripTime}ms");
			return (reply.RoundtripTime, reply.Status);
		}

		#region IDisposable Support
		private bool _disposedValue; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			_logger.Debug("Dispose() called");
			if (!_disposedValue)
			{
				if (disposing)
				{
					Stop();
					Log.CloseAndFlush();
					_runThread.Join();
				}
				_disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			GC.SuppressFinalize(this);
			Dispose(true);
		}
		#endregion
	}
}
