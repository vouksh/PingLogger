using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Threading;
using Serilog;
using System.Text.Json;
using System.Linq;

namespace PingLogger
{
	public class Pinger
	{
		public Host Host;
		public bool Running = false;
		public Pinger(Host host)
		{
			Host = host;
			if(Host.WriteFile)
			{
				Log.Logger = new LoggerConfiguration()
					.WriteTo.Console()
					.WriteTo.File(Host.HostName + ".log", rollingInterval: RollingInterval.Day)
					.CreateLogger();
			} else
			{
				Log.Logger = new LoggerConfiguration()
					.WriteTo.Console()
					.CreateLogger();
			}
			if(Host.PacketSize > 20000)
			{
				Log.Error("Packet size too large. Resetting to 20000 bytes");
				Host.PacketSize = 20000;
			}
			if(Host.Interval < 500)
			{
				Log.Error("Interval too short. Setting to 500ms");
				Host.Interval = 500;
			}
			Log.Information("Verifying IP address of hostname is current.");
			IPAddress[] iPs = Dns.GetHostAddresses(Host.HostName);
			if(iPs[0].ToString() == Host.IP)
			{
				Log.Information("IP matches. Continuing");
			} else
			{
				Log.Warning("IP address does not match last stored. Saving new IP address");
				Host.IP = iPs[0].ToString();
			}
		}
		public Host UpdateHost()
		{
			return Host;
		}
		private Random random = new Random();
		public string RandomString(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, length)
			  .Select(s => s[random.Next(s.Length)]).ToArray());
		}
		public async Task Start()
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
				pingSender.SendAsync(Host.IP, Host.Threshold, buffer, options, waiter);
				waiter.WaitOne();
				await Task.Delay(Host.Interval);
				if (Host.Count > 0 && loops >= Host.Count)
					Running = false;
				//pingSender.Dispose();
				loops++;
			}
		}
		public void Stop()
		{
			Running = false;
		}
		private void SendPing(object sender, PingCompletedEventArgs e)
		{
			if (e.Cancelled)
			{
				Log.Information("Ping canceled.");
				((AutoResetEvent)e.UserState).Set();
			}
			if (e.Error != null)
			{
				Log.Error("Ping failed: {0}", e.Error.ToString());
				((AutoResetEvent)e.UserState).Set();
			}
			var reply = e.Reply;
			switch (reply.Status)
			{
				case IPStatus.Success:
					if (reply.RoundtripTime > Host.Threshold)
					{
						Log.Warning("Pinged {0} RoundTrip: {1}ms TTL: {2}", Host.IP.ToString(), reply.RoundtripTime, reply.Options.Ttl);
					} else
					{
						Log.Information("Pinged {0} RoundTrip: {1}ms TTL: {2}", Host.IP.ToString(), reply.RoundtripTime, reply.Options.Ttl);
					}
					break;
				case IPStatus.DestinationHostUnreachable:
					Log.Error("Destination host unreachable.");
					break;
				case IPStatus.DestinationNetworkUnreachable:
					Log.Error("Destination network unreachable.");
					break;
				case IPStatus.DestinationUnreachable:
					Log.Error("Destination unreachable, cause unknown.");
					break;
				case IPStatus.HardwareError:
					Log.Error("Ping failed due to hardware.");
					break;
				case IPStatus.TimedOut:
					Log.Warning("Ping timed out to host {0}", Host.IP.ToString());
					break;
			}
			((AutoResetEvent)e.UserState).Set();
		}
	}
}
