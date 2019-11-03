using PingLogger.Misc;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace PingLogger
{
	public class Pinger
	{
		private readonly Host Host;
		public bool Running = false;
		private bool stopping = false;
		private readonly ILogger Logger;
		private Thread RunThread;
		/// <summary>
		/// This class is where all of the actual pinging work is done.
		/// I creates a thread that loops until canceled.
		/// The thread fires an event to do the actual pinging. 
		/// </summary>
		/// <param name="host">The host that will be pinged.</param>
		/// <param name="defaultSilent">Set this to true to prevent Serilog from printing to the console.</param>
		public Pinger(Host host, bool defaultSilent = false)
		{
			Host = host;
			if (!Directory.Exists("./Logs"))
				Directory.CreateDirectory("./Logs");
			//Check to see if just this is supposed to be silent, or if it's app-wide setting
			var outputTemp = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
			var filePath = "./Logs/" + Host.HostName + "-{Date}.log";
			//if (!Host.Silent && !defaultSilent)
			if (!defaultSilent)
			{
				Logger = new LoggerConfiguration()
					.WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Literate)
					.WriteTo.RollingFile(
						filePath,
						shared: true,
						outputTemplate: outputTemp)
					.CreateLogger();
			}
			else
			{
				Logger = new LoggerConfiguration()
					.WriteTo.RollingFile(
						filePath,
						shared: true,
						outputTemplate: outputTemp)
					.CreateLogger();
			}
			//Check to make sure the packet size isn't too large. Don't want to abuse this.
			if (Host.PacketSize > 65500)
			{
				Logger.Error("Packet size too large. Resetting to 20000 bytes");
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
			RunThread = new Thread(new ThreadStart(StartLogging));
			Logger.Information("Starting ping logging for host {0} ({1})", Host.HostName, Host.IP);
			Logger.Information("Using the following options:");
			Logger.Information("Threshold: {0}ms", Host.Threshold);
			Logger.Information("Timeout: {0}ms", Host.Timeout);
			Logger.Information("Interval: {0}ms", Host.Interval);
			Logger.Information("Packet Size: {0} bytes", Host.PacketSize);
			//Logger.Information("Silent Output: {0}", Host.Silent);

			RunThread.Start();
		}
		/// <summary>
		/// Primary thread that is spun up. Doesn't do the actual pinging, that's handled by an event.
		/// </summary>
		private void StartLogging()
		{
			Ping pingSender = new Ping();
			pingSender.PingCompleted += new PingCompletedEventHandler(SendPing);
			PingOptions options = new PingOptions();
			AutoResetEvent waiter = new AutoResetEvent(false);
			options.DontFragment = false;

			//Generate a string that's as long as the packet size. 
			//This is outside of the loop, so it's going to be the same while the thread is running.
			//If it's restarted, we generate a new string. 
			string data = RandomString(Host.PacketSize);
			
			byte[] buffer = Encoding.ASCII.GetBytes(data);

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
			}
		}
		/// <summary>
		/// Change the 'stopping' variable to true so that the thread can dispose of the pingSender properly, then allows the thread to exit safely.
		/// </summary>
		public void Stop()
		{
			stopping = true;
			Logger.Information("Stopping ping logger for host {0} ({1})", Host.HostName, Host.IP);
		}
		/// <summary>
		/// The main workhorse of the class. 
		/// This event is called with every ping 
		/// Most of the options will never be hit, but I included them as a just-in-case measure.
		/// </summary>
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
					//Ping was successful. Check to see if the round trip time was greater than the threshold.
					//If it is, then we change the output to be a warning, making it easy to track down in the log files.
					if (reply.RoundtripTime >= Host.Threshold)
					{
						Logger.Warning("Pinged {0} ({1}) RoundTrip: {2}ms TTL: {3}", Host.HostName, Host.IP.ToString(), reply.RoundtripTime, reply.Options.Ttl);
					}
					else
					{
						Logger.Information("Pinged {0} ({1}) RoundTrip: {2}ms TTL: {3}", Host.HostName, Host.IP.ToString(), reply.RoundtripTime, reply.Options.Ttl);
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
					Logger.Warning("Ping timed out to host {0} ({1}). Timeout is {2}ms", Host.HostName, Host.IP.ToString(), Host.Timeout);
					break;
			}
			((AutoResetEvent)e.UserState).Set();
		}
	}
}
