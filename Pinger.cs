using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.IO;
using System.Threading;
using Serilog;
using System.Linq;

namespace PingLogger
{
	public class Pinger
	{
		public Host Host;
		public bool Running = false;
		private bool stopping = false;
		private readonly ILogger Logger;
		private Thread RunThread;
		public Pinger(Host host, bool defaultSilent = false)
		{
			Host = host;
			if (!Directory.Exists("./Logs"))
				Directory.CreateDirectory("./Logs");
			if (!Host.Silent && !defaultSilent)
			{
				Logger = new LoggerConfiguration()
					.WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Literate)
					.WriteTo.File($"./Logs/{Host.HostName}-.log", rollingInterval: RollingInterval.Day)
					.CreateLogger();
			} else
			{
				Logger = new LoggerConfiguration()
					.WriteTo.File($"./Logs/{Host.HostName}-.log", rollingInterval: RollingInterval.Day)
					.CreateLogger();
			}
			if (Host.PacketSize > 20000)
			{
				Logger.Error("Packet size too large. Resetting to 20000 bytes");
				Host.PacketSize = 20000;
			}
			if (Host.Interval < 500)
			{
				Logger.Error("Interval too short. Setting to 500ms");
				Host.Interval = 500;
			}
			Logger.Information("Verifying IP address of hostname is current.");
			IPAddress[] iPs = Dns.GetHostAddresses(Host.HostName);
			if (iPs[0].ToString() == Host.IP)
			{
				Logger.Information("IP matches. Continuing");
			}
			else
			{
				Logger.Warning("IP address does not match last stored. Saving new IP address");
				Host.IP = iPs[0].ToString();
			}
		}
		public Host UpdateHost()
		{
			return Host;
		}
		private readonly Random random = new Random();
		public string RandomString(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, length)
			  .Select(s => s[random.Next(s.Length)]).ToArray());
		}
		public void Start()
		{
			RunThread = new Thread(new ThreadStart(StartLogging));
			Logger.Information("Starting ping logging for host {0} ({1})", Host.HostName, Host.IP);
			Logger.Information("Using the following options:");
			Logger.Information("Threshold: {0}ms", Host.Threshold);
			Logger.Information("Timeout: {0}ms", Host.Timeout);
			Logger.Information("Interval: {0}ms", Host.Interval);
			Logger.Information("Packet Size: {0} bytes", Host.PacketSize);
			RunThread.Start();
		}

		private void StartLogging()
		{
			Ping pingSender = new Ping();
			pingSender.PingCompleted += new PingCompletedEventHandler(SendPing);
			PingOptions options = new PingOptions();
			AutoResetEvent waiter = new AutoResetEvent(false);
			options.DontFragment = true;

			string data = RandomString(Host.PacketSize);
			byte[] buffer = Encoding.ASCII.GetBytes(data);

			int loops = 0;
			Running = true;
			while (Running)
			{
				pingSender.SendAsync(Host.IP, Host.Timeout, buffer, options, waiter);
				waiter.WaitOne();
				Thread.Sleep(Host.Interval);
				if (stopping)
				{
					Running = false;
					pingSender.Dispose();
				}
				loops++;
			}
		}
		public void Stop()
		{
			stopping = true;
			Logger.Information("Stopping ping logger for host {0} ({1})", Host.HostName, Host.IP);
		}
		private void SendPing(object sender, PingCompletedEventArgs e)
		{
			if (e.Cancelled)
			{
				Logger.Information("Ping canceled.");
				((AutoResetEvent)e.UserState).Set();
			}
			if (e.Error != null)
			{
				Logger.Error("Ping failed: {0}", e.Error.ToString());
				((AutoResetEvent)e.UserState).Set();
			}
			var reply = e.Reply;
			switch (reply.Status)
			{
				case IPStatus.Success:
					if (reply.RoundtripTime > Host.Threshold)
					{
						Logger.Warning("Pinged {0} ({1}) RoundTrip: {2}ms TTL: {3}", Host.HostName, Host.IP.ToString(), reply.RoundtripTime, reply.Options.Ttl);
					}
					else
					{
						Logger.Information("Pinged {0} ({1}) RoundTrip: {2}ms TTL: {3}", Host.HostName, Host.IP.ToString(), reply.RoundtripTime, reply.Options.Ttl);
					}
					break;
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
					Logger.Warning("Ping timed out to host {0} ({1}). Timeout is {2}ms", Host.HostName, Host.IP.ToString(), Host.Timeout);
					break;
			}
			((AutoResetEvent)e.UserState).Set();
		}
	}
}
