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
using System.Collections.Generic;

namespace PingLogger
{
	class Program
	{
		private static Opts Options;
		private static List<Pinger> Pingers = new List<Pinger>();
		static void Main(string[] args)
		{
			Log.Logger = new LoggerConfiguration()
				.WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
				.WriteTo.File("PingLogger-.log", rollingInterval: RollingInterval.Day)
				.CreateLogger();
			Log.Information("PingLogger v0.2 by Jack Butler");

			var configured = ReadJsonConfig();

			if(configured)
			{
				if(Options.Hosts.Count > 0)
				{
					Log.Information("Existing hosts detected.");
					try
					{
						Console.Write("Do you want to add another host? (y/N) ");
						string resp = WaitForInput.ReadLine(5000);
						if(resp.ToLower() == "y")
						{
							AddNewHosts();
						}
					} catch(TimeoutException)
					{
						Console.WriteLine();
						Log.Information("No input detected. Skipping addition of new hosts");
					}
				} else
				{
					AddNewHosts();
				}
			} else
			{
				Log.Information("No hosts configured.");
				AddNewHosts();
			}
			foreach (var host in Options.Hosts)
			{
				Pingers.Add(new Pinger(host));
			}
			foreach(var pinger in Pingers)
			{
				pinger.Start();
			}

			Console.CancelKeyPress += new ConsoleCancelEventHandler(Closing);
			bool pingersRunning = true;
			while (pingersRunning)
			{
				int running = 0;
				foreach (var pinger in Pingers)
				{
					if (pinger.Running)
						running++;
					Log.Verbose(running.ToString());
				}
				if (running == 0)
					pingersRunning = false;
			}
		}
		public static bool CheckIfHostExists(string hostName)
		{
			if (Options.Hosts?.Count > 0)
			{
				foreach (var host in Options.Hosts)
				{
					if (host.HostName == hostName)
						return true;
				}
			}
			return false;
		}
		public static void AddNewHosts()
		{
			bool done = false;
			while(!done)
			{
				Host newHost = new Host();
				//Get host name from console input
				Console.Write("New host name (can be IP): ");
				var hostName = Console.ReadLine();
				if(CheckIfHostExists(hostName))
				{
					Console.WriteLine("Host already exists in configuration.");
					continue;
				}
				try
				{
					IPAddress[] iPs = Dns.GetHostAddresses(hostName);
					newHost.HostName = hostName;
					if (iPs.Length < 1)
						throw new Exception("Invalid host name.");
					newHost.IP = iPs[0].ToString();
				} catch (Exception)
				{
					Console.WriteLine("Invalid host name.");
					continue;
				}

				//See if user wants to set up advanced options. Otherwise we use the defaults in the Host class
				Console.Write("Do you want to specify advanced options (threshold, packet size, interval)? (y/N) ");
				var advOpts = Console.ReadLine().ToLower();
				if(advOpts == "y" || advOpts == "yes")
				{
					//Sets the warning threshold. Defaults to 500ms;
					Console.Write("Ping time warning threshold: (500ms) ");
					var threshold = Console.ReadLine();
					if (threshold == string.Empty)
						newHost.Threshold = 500;
					else
					{
						try
						{
							//See if we can convert it, but strip the 'ms' off if the user specified it. 
							newHost.Threshold = Convert.ToInt32(threshold.Replace("ms", ""));
						} catch (Exception)
						{
							Console.WriteLine("Invalid threshold specified. Defaulting to 500ms");
						}
					}

					//Sets the ping timeout. Defaults to 1000ms
					Console.Write("Ping timeout: (1000ms) ");
					var timeout = Console.ReadLine();
					if (timeout == string.Empty)
						newHost.Timeout = 1000;
					else
					{
						try
						{
							//See if we can convert it, but strip the 'ms' off if the user specified it. 
							newHost.Timeout = Convert.ToInt32(timeout.Replace("ms", ""));
						}
						catch (Exception)
						{
							Console.WriteLine("Invalid timeout specified. Defaulting to 1000ms");
						}
					}

					//Sets the packet size. Defaults to 64 bytes
					Console.Write("Packet size in bytes: (64) ");
					var packetSize = Console.ReadLine();
					if (packetSize == string.Empty)
						newHost.PacketSize = 64;
					else
					{
						try
						{
							newHost.PacketSize = Convert.ToInt32(packetSize);
						}
						catch (Exception)
						{
							Console.WriteLine("Invalid packet size specified. Defaulting to 64");
						}
					}

					//Sets the ping interval. Defaults to 1000ms
					Console.Write("Ping interval: (1000ms) ");
					var interval = Console.ReadLine();
					if (interval == string.Empty)
						newHost.Interval = 1000;
					else
					{
						try
						{
							//See if we can convert it, but strip the 'ms' off if the user specified it. 
							newHost.Interval = Convert.ToInt32(interval.Replace("ms", ""));
						}
						catch (Exception)
						{
							Console.WriteLine("Invalid interval specified. Defaulting to 1000ms");
						}
					}
				}
				//All done. Add it to the options, then ask if they want to add another. 
				Options.Hosts.Add(newHost);
				Console.Write("Do you want to add another? (y/N) ");
				var addMore = Console.ReadLine().ToLower();
				if (addMore == string.Empty || addMore == "n" || addMore == "no")
					done = true;
				WriteConfig();
			}
		}
		public static bool ReadJsonConfig()
		{
			if(File.Exists("./opts.json"))
			{
				try
				{
					var fileContents = File.ReadAllText("./opts.json");
					Options = JsonSerializer.Deserialize<Opts>(fileContents);
					return true;
				} catch (Exception)
				{
					Log.Error("Error loading configuration file");
				}
			}
			Options = new Opts();
			Options.Hosts = new List<Host>();
			return false;
		}
		public static void WriteConfig()
		{
			try
			{
				File.WriteAllText("./opts.json", JsonSerializer.Serialize(Options, new JsonSerializerOptions { WriteIndented = true }));
			} catch (Exception e)
			{
				Log.Error("Error saving configuration file");
				Log.Error(e.ToString());
			}
		}
		public static void ShutdownAllPingers()
		{
			foreach(var pinger in Pingers)
			{
				pinger.Stop();
			}
		}
		public static void Closing(object sender, ConsoleCancelEventArgs args)
		{
			ShutdownAllPingers();
			Log.Information("Closing logger.");
			WriteConfig();
			_ = Console.ReadKey();
			args.Cancel = true;
			Environment.Exit(0);
		}
	}
}
