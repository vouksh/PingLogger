using System;
using System.Text;
using Serilog;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using PingLogger.GUI.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PingLogger.GUI.Workers
{
	public class Pinger : IDisposable
	{
		private readonly Host Host;
		public bool Running = false;
		private bool stopping = false;
		private readonly ILogger Logger;
		private Thread RunThread;
		private readonly Ping pingSender = new Ping();
		public BlockingCollection<Reply> Replies = new BlockingCollection<Reply>();
		private bool DontFragment = true;
		/// <summary>
		/// This class is where all of the actual pinging work is done.
		/// I creates a thread that loops until canceled.
		/// The thread fires an event to do the actual pinging. 
		/// </summary>
		/// <param name="host">The host that will be pinged.</param>
		/// <param name="defaultSilent">Set this to true to prevent Serilog from printing to the console.</param>
		public Pinger(Host host)
		{
			Host = host;
			if (!Directory.Exists("./Logs"))
				Directory.CreateDirectory("./Logs");
			if (!Directory.Exists($"./Logs/{Host.HostName}/"))
				Directory.CreateDirectory($"./Logs/{Host.HostName}/");
			//Check to see if just this is supposed to be silent, or if it's app-wide setting
			var outputTemp = "[{Timestamp:HH:mm:ss} {Level:u4}] {Message:lj}{NewLine}{Exception}";
			var errorOutputTemp = "[{Timestamp:HH:mm:ss} {Level:u5}] {Message:lj}{NewLine}{Exception}";

			var filePath = "./Logs/" + Host.HostName + "/" + Host.HostName + "-{Date}.log";
			var errorPathName = "./Logs/" + Host.HostName + "/" + Host.HostName + "-Errors-{Date}.log";
			var warnPathName = "./Logs/" + Host.HostName + "/" + Host.HostName + "-Warnings-{Date}.log";

#if DEBUG
			var debugOutputTemp = "[{Timestamp:HH:mm:ss.fff} {Level}] ({ThreadId}) {Message:lj}{NewLine}{Exception}";
			var debugPathName = "./Logs/" + Host.HostName + "/" + Host.HostName + "-Debug-{Date}.log";
#endif
			Logger = new LoggerConfiguration()
#if DEBUG
				.Enrich.With(new ThreadIdEnricher())
				.MinimumLevel.Verbose()
#endif
				.WriteTo.Logger(
					l => l.Filter.ByIncludingOnly(e => e.Level == Serilog.Events.LogEventLevel.Error)
					.WriteTo.RollingFile(
						errorPathName,
						restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
						retainedFileCountLimit: Config.DaysToKeepLogs,
						shared: true,
						outputTemplate: errorOutputTemp,
						flushToDiskInterval: TimeSpan.FromSeconds(2) //Added this because I noticed that it wasn't consistently flushing to disk at a good interval.
						)
					)
				.WriteTo.Logger(
					l => l.Filter.ByIncludingOnly(e => e.Level == Serilog.Events.LogEventLevel.Warning)
					.WriteTo.RollingFile(
						warnPathName,
						shared: true,
						retainedFileCountLimit: Config.DaysToKeepLogs,
						outputTemplate: outputTemp,
						flushToDiskInterval: TimeSpan.FromSeconds(2) 
						)
					)
#if DEBUG
					.WriteTo.Logger(
					l => l.Filter.ByIncludingOnly(e => e.Level == Serilog.Events.LogEventLevel.Debug)
					.WriteTo.RollingFile(
						debugPathName,
						shared: true,
						retainedFileCountLimit: Config.DaysToKeepLogs,
						restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug,
						outputTemplate: debugOutputTemp,
						flushToDiskInterval: TimeSpan.FromSeconds(2)
						)
					)
#endif
				.WriteTo.RollingFile(
						filePath,
						shared: true,
						outputTemplate: outputTemp,
						retainedFileCountLimit: Config.DaysToKeepLogs,
						restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
						flushToDiskInterval: TimeSpan.FromSeconds(2)
					)
				.CreateLogger();

			//Check to make sure the packet size isn't too large. Don't want to abuse this.
			if (Host.PacketSize > 65500)
			{
				Logger.Error("Packet size too large. Resetting to 65500 bytes");
				Host.PacketSize = 65500;
			}
			//Make sure that the interval isn't too short. If you set it to be too frequent, it might get flagged as DDoS attack.
			if (Host.Interval < 500)
			{
				Logger.Error("Interval too short. Setting to 500ms");
				Host.Interval = 500;
			}
			//Verify that the IP stored in the settings file matches what it currently resolves to.
			//Mostly in cases of local network and DHCP
			Logger.Information("Verifying IP address of hostname is current.");
			foreach (var ip in Dns.GetHostAddresses(Host.HostName))
			{
				Logger.Debug($"IP: {ip}");
				if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
				{
					if (ip.ToString() == Host.IP)
					{
						Logger.Information("IP matches. Continuing");
					}
					else
					{
						Logger.Warning("IP address does not match last stored. Saving new IP address");
						Host.IP = ip.ToString();
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
			Logger.Debug("UpdateHost() Requested");
			return Host;
		}

		private readonly Random random = new Random();
		/// <summary>
		/// Generates a random string with the specified length.
		/// </summary>
		/// <param name="length">Number of characters in the string</param>
		/// <returns>Random string of letters and numbers</returns>
		private string RandomString(int length)
		{
			Logger.Debug($"Random string of {length} characters requested.");
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, length)
			  .Select(s => s[random.Next(s.Length)]).ToArray());
		}
		/// <summary>
		/// Starts the primary thread where all the work is done. 
		/// Outputs all of the hosts info so that if we're starting it up, the user can see if anything is wrong, allowing them time to make changes.
		/// </summary>
		public void Start()
		{
			Logger.Debug("Start()");
			RunThread = new Thread(new ThreadStart(StartLogging))
			{
				Name = $"{Host.HostName}-MainThread"
			};
			Logger.Information("Starting ping logging for host {0} ({1})", Host.HostName, Host.IP);
			Logger.Information("Using the following options:");
			Logger.Information("Threshold: {0}ms", Host.Threshold);
			Logger.Information("Timeout: {0}ms", Host.Timeout);
			Logger.Information("Interval: {0}ms", Host.Interval);
			Logger.Information("Packet Size: {0} bytes", Host.PacketSize);

			Logger.Debug("RunThread.Start()");
			RunThread.Start();
		}
		/// <summary>
		/// Primary thread that is spun up. Doesn't do the actual pinging, that's handled by an event.
		/// </summary>
		private void StartLogging()
		{
			Logger.Debug("StartLogging() Called.");
			pingSender.PingCompleted += new PingCompletedEventHandler(SendPing);
			AutoResetEvent waiter = new AutoResetEvent(false);

			//Generate a string that's as long as the packet size. 
			//This is outside of the loop, so it's going to be the same while the thread is running.
			//If it's restarted, we generate a new string. 
			string data = RandomString(Host.PacketSize);
			Logger.Debug($"Data string: {data}");

			byte[] buffer = Encoding.ASCII.GetBytes(data);

			Running = true;
			while (Running)
			{
				PingOptions options = new PingOptions
				{
					DontFragment = DontFragment,
					Ttl = 128
				};
				Logger.Debug($"Running: {Running}");
				Logger.Debug($"stopping: {stopping}");
				if (stopping)
				{
					Running = false;
				}
				else
				{
					Logger.Debug("Sending Async Ping");
					try
					{
						var sw = new Stopwatch();
						sw.Start();
						Logger.Debug("Stopwatch Started");
						pingSender.SendAsync(Host.IP, Host.Timeout, buffer, options, waiter);
						waiter.WaitOne();
						sw.Stop();
						Logger.Debug($"Stopwatch.ElapsesedMilliseconds: {sw.ElapsedMilliseconds}ms");
						Thread.Sleep(Host.Interval);
						Logger.Debug($"Waited {Host.Interval}ms");
					}
					catch
					{
						Logger.Debug("Thread Interrupted");
					}
				}
			}
			Logger.Debug("PingSender.Dispose()");
			pingSender.Dispose();
			Logger.Debug("SendPing() Ended");
		}
		/// <summary>
		/// Change the 'stopping' variable to true so that the thread can dispose of the pingSender properly, then allows the thread to exit safely.
		/// </summary>
		public void Stop()
		{
			if (Running)
			{
				stopping = true;
				Logger.Information("Stopping ping logger for host {0} ({1})", Host.HostName, Host.IP);
				Logger.Debug("SendAsyncCancel()");
				pingSender.SendAsyncCancel();
				try
				{
					RunThread.Interrupt();
				}
				catch
				{
					Logger.Debug("Thread Interrupted");
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
			Thread.CurrentThread.Name = $"{Host.HostName}-PingThread-{Thread.CurrentThread.ManagedThreadId}";
			Logger.Debug("SendPing() called");
			if (e.Cancelled)
			{
				Logger.Information("Ping canceled.");
				((AutoResetEvent)e.UserState).Set();
				return;
			}
			if (e.Error != null)
			{
				Logger.Information("Ping canceled.");
				//Logger.Debug(e.Error.ToString());
				((AutoResetEvent)e.UserState).Set();
				return;
			}
			var reply = e.Reply;
			bool timedOut = false;
			bool success = false;
			List<IPAddress> traceIPs = new List<IPAddress>();
			switch (reply.Status)
			{
				case IPStatus.Success:
					Logger.Debug("Ping Success");
					//This check is because of a bug/problem with Ping where, if using a small timeout threshold, the ping reply can still be received.
					//See https://docs.microsoft.com/en-us/dotnet/api/system.net.networkinformation.ping.send?redirectedfrom=MSDN&view=netcore-3.1#System_Net_NetworkInformation_Ping_Send_System_String_System_Int32_System_Byte___
					if (reply.RoundtripTime < Host.Timeout)
					{
						//Ping was successful. Check to see if the round trip time was greater than the threshold.
						//If it is, then we change the output to be a warning, making it easy to track down in the log files.
						if (reply.RoundtripTime >= Host.Threshold)
						{
							Logger.Warning("Pinged {0} ({1}) RoundTrip: {2}ms (Over Threshold) TTL: {3}", Host.HostName, Host.IP.ToString(), reply.RoundtripTime, reply.Options.Ttl);
							success = true;
						}
						else
						{
							Logger.Information("Pinged {0} ({1}) RoundTrip: {2}ms TTL: {3}", Host.HostName, Host.IP.ToString(), reply.RoundtripTime, reply.Options.Ttl);
							success = true;
						}
					} else
					{
						Logger.Debug("Ping Reply Success, but roundtrip time exceeds timeout. Marking it as a timeout.");
						Logger.Error("Ping timed out to host {0} ({1}). Timeout is {2}ms", Host.HostName, Host.IP.ToString(), Host.Timeout);
						timedOut = true;
					}
					break;
				//These indicate that there was a problem somewhere along the way. 
				case IPStatus.DestinationHostUnreachable:
					Logger.Error("Destination host unreachable.");
					break;
				case IPStatus.DestinationNetworkUnreachable:
					Logger.Error("Destination network unreachable.");
					break;
				case IPStatus.DestinationUnreachable:
					Logger.Error("Destination unreachable, cause unknown.");
					break;
				case IPStatus.HardwareError:
					Logger.Error("Ping failed due to hardware.");
					break;
				case IPStatus.TimedOut:
					Logger.Debug("Ping Timed Out");
					Logger.Error("Ping timed out to host {0} ({1}). Timeout is {2}ms", Host.HostName, Host.IP.ToString(), Host.Timeout);
					timedOut = true;
					break;
				case IPStatus.PacketTooBig:
					Logger.Debug("Packet too large. Turning on fragmentation");
					Logger.Error("Packet size too large, turning on fragmentation.");
					timedOut = true;
					DontFragment = false;
					break;
			}
			var LogReply = new Reply
			{
				Host = Host,
				DateTime = DateTime.Now,
				Ttl = reply.Options?.Ttl,
				RoundTrip = reply.RoundtripTime,
				TimedOut = timedOut,
				Succeeded = success
			};
			Replies.Add(LogReply);
			Logger.Debug("Ping Ended");
			((AutoResetEvent)e.UserState).Set();
		}

		/// <summary>
		/// Performs a route trace on the host. Cannot be run if the ping logging thread is running.
		/// </summary>
		/// <param name="maxTTL">Max number of jumps</param>
		/// <returns></returns>
		public async IAsyncEnumerable<TraceReply> GetTraceRoute(int maxTTL = 30)
		{
			if(Running)
				throw new Exception("Ping logger cannot be running while performing a route trace!");

			var result = new List<TraceReply>();
			string data = RandomString(Host.PacketSize);
			Logger.Debug($"Data string: {data}");

			byte[] buffer = Encoding.ASCII.GetBytes(data);
			using var pinger = new Ping();
			for(int ttl = 1; ttl <= maxTTL; ttl++)
			{
				var pingOpts = new PingOptions(ttl, true);
				var reply = await pinger.SendPingAsync(Host.HostName, Host.Timeout, buffer, pingOpts);
				if(reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired)
				{
					yield return new TraceReply { IPAddress = reply.Address.ToString(), RoundTrip = await GetSingleRoundTrip(reply.Address, ttl), Ttl = ttl };
				}
				if(reply.Status != IPStatus.TtlExpired && reply.Status != IPStatus.TimedOut)
				{
					break;
				}
			}
			//return result;
		}

		private async Task<long> GetSingleRoundTrip(IPAddress address, int ttl)
		{
			string data = RandomString(Host.PacketSize);
			byte[] buffer = Encoding.ASCII.GetBytes(data);
			using var pinger = new Ping();
			var pingOpts = new PingOptions(ttl, true);
			var reply = await pinger.SendPingAsync(address, Host.Timeout, buffer, pingOpts);
			return reply.RoundtripTime;
		}

		public static IEnumerable<IPAddress> GetTraceRoute(string hostName, int timeout = 1000, int maxTTL = 30)
		{
			var result = new List<IPAddress>();
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			string data = new string(Enumerable.Repeat(chars, 32)
			  .Select(s => s[new Random().Next(s.Length)]).ToArray());

			byte[] buffer = Encoding.ASCII.GetBytes(data);
			using var pinger = new Ping();
			for (int ttl = 1; ttl <= maxTTL; ttl++)
			{
				var pingOpts = new PingOptions(ttl, true);
				var reply = pinger.Send(hostName, timeout, buffer, pingOpts);
				if (reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired)
				{
					yield return reply.Address;
				}
				if (reply.Status != IPStatus.TtlExpired && reply.Status != IPStatus.TimedOut)
				{
					break;
				}
			}
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			Logger.Debug("Dispose() called");
			if (!disposedValue)
			{
				if (disposing)
				{
					Stop();
					Log.CloseAndFlush();
					RunThread.Join();
				}
				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}
		#endregion
	}
}
