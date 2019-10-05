using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.IO;
using Serilog;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

namespace PingLogger
{
	class Program
	{
		private static Opts Options;
		private static readonly List<Pinger> Pingers = new List<Pinger>();
		static void Main()
		{
			Log.Logger = new LoggerConfiguration()
				.WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
				.WriteTo.File("PingLogger-.log", rollingInterval: RollingInterval.Day)
				.CreateLogger();
			Log.Information("PingLogger v0.2 by Jack Butler");
			DoStartupTasks();
		}
		public static bool DoStartupTasks(bool killPingers = false)
		{
			Console.CancelKeyPress += new ConsoleCancelEventHandler(Closing);
			
			if (killPingers)
				ShutdownAllPingers();

			var configured = ReadJsonConfig();

			if (configured)
			{
				if (Options.Hosts.Count > 0)
				{
					Log.Information("Existing hosts detected.");
					try
					{
						Console.Write("Do you want to remove, edit, or add another host? (r/e/a/N) ");
						string resp = WaitForInput.ReadLine(5000).ToLower();
						if (resp == "a" || resp == "add")
						{
							AddNewHosts();
						}
						if (resp == "e" || resp == "edit")
						{
							EditHosts();
						}
						if (resp == "r" || resp == "remove")
						{
							RemoveHosts();
							if (Options.Hosts.Count < 1)
							{
								Console.WriteLine("All hosts removed.");
								AddNewHosts();
							}
						}
					}
					catch (TimeoutException)
					{
						Console.WriteLine();
						Log.Information("No input detected. Skipping addition of new hosts");
					}
				}
				else
				{
					AddNewHosts();
				}
			}
			else
			{
				Log.Information("No hosts configured.");
				AddNewHosts();
			}
			foreach (var host in Options.Hosts)
			{
				Pingers.Add(new Pinger(host));
			}
			foreach (var pinger in Pingers)
			{
				pinger.Start();
			}
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
			return true;
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
		public static void RemoveHosts()
		{
			bool done = false;
			while (!done)
			{
				Console.WriteLine("Which host would you like to remove?");
				for (int i = 0; i < Options.Hosts.Count; i++)
				{
					Console.WriteLine($"[{i + 1}] {Options.Hosts[i].HostName} ({ Options.Hosts[i].IP})");
				}
				Console.Write("Enter the number you wish to edit: ");
				var selectedHost = Console.ReadLine();
				int selectedIndex = 0;
				try
				{
					selectedIndex = Convert.ToInt32(selectedHost) - 1;
				}
				catch (Exception)
				{
					Console.WriteLine("Invalid number selected.");
				}
				Console.Write("Are you sure you want to remove host {0}? (y/N) ", Options.Hosts[selectedIndex].HostName);
				var resp = Console.ReadLine().ToLower();
				if (resp != string.Empty || resp == "y" || resp == "yes")
				{
					Log.Information("Removed host {0} with IP address {1}, Threshold {2}ms, Interval {3}ms, Packet Size {4}",
						Options.Hosts[selectedIndex].HostName,
						Options.Hosts[selectedIndex].IP,
						Options.Hosts[selectedIndex].Threshold,
						Options.Hosts[selectedIndex].Interval,
						Options.Hosts[selectedIndex].PacketSize);
					Options.Hosts.RemoveAt(selectedIndex);
				}
				Console.Write("Do you want to remove another? (y/N) ");
				var addMore = Console.ReadLine().ToLower();
				if (addMore == string.Empty || addMore == "n" || addMore == "no")
					done = true;
				WriteConfig();
			}
		}
		public static void EditHosts()
		{
			bool done = false;
			while (!done)
			{
				Console.WriteLine("Which host would you like to edit?");
				for (int i = 0; i < Options.Hosts.Count; i++)
				{
					Console.WriteLine($"[{i + 1}] {Options.Hosts[i].HostName} ({ Options.Hosts[i].IP})");
				}
				Console.Write("Enter the number you wish to edit: ");
				var selectedHost = Console.ReadLine();
				int selectedIndex;
				try
				{
					selectedIndex = Convert.ToInt32(selectedHost) - 1;
				}
				catch (Exception)
				{
					Console.WriteLine("Invalid number selected.");
					continue;
				}
				Host newHost = Options.Hosts[selectedIndex];
				//Get host name from console input
				Console.Write("New host name (can be IP): ({0})", newHost.HostName);
				var hostName = Console.ReadLine();
				if (hostName != string.Empty)
				{
					try
					{
						IPAddress[] iPs = Dns.GetHostAddresses(hostName);
						newHost.HostName = hostName;
						if (iPs.Length < 1)
							throw new Exception("Invalid host name.");
						newHost.IP = iPs[0].ToString();
						Console.WriteLine("Resolved to IP {0}", newHost.IP);
					}
					catch (Exception)
					{
						Console.WriteLine("Invalid host name.");
						continue;
					}
				}
				//See if user wants to set up advanced options. Otherwise we use the defaults in the Host class
				Console.Write("Do you want to specify advanced options (threshold, packet size, interval)? (y/N) ");
				var advOpts = Console.ReadLine().ToLower();
				if (advOpts == "y" || advOpts == "yes")
				{
					//Sets the warning threshold. Defaults to 500ms;
					Console.Write("Ping time warning threshold: ({0}ms) ", newHost.Threshold);
					var threshold = Console.ReadLine();
					if (threshold != string.Empty)
					{
						try
						{
							//See if we can convert it, but strip the 'ms' off if the user specified it. 
							newHost.Threshold = Convert.ToInt32(threshold.Replace("ms", ""));
						}
						catch (Exception)
						{
							Console.WriteLine("Invalid threshold specified. Reverting back to {0}ms", newHost.Threshold);
						}
					}
					//Sets the ping timeout. Defaults to 1000ms
					Console.Write("Ping timeout: ({0}ms) ", newHost.Timeout);
					var timeout = Console.ReadLine();
					if (timeout != string.Empty)
					{
						try
						{
							//See if we can convert it, but strip the 'ms' off if the user specified it. 
							newHost.Timeout = Convert.ToInt32(timeout.Replace("ms", ""));
						}
						catch (Exception)
						{
							Console.WriteLine("Invalid timeout specified. Reverting back to {0}ms", newHost.Timeout);
						}
					}

					//Sets the packet size. Defaults to 64 bytes
					Console.Write("Packet size in bytes: ({0}) ", newHost.PacketSize);
					var packetSize = Console.ReadLine();
					if (packetSize != string.Empty)
					{
						try
						{
							newHost.PacketSize = Convert.ToInt32(packetSize);
						}
						catch (Exception)
						{
							Console.WriteLine("Invalid packet size specified. Reverting back to {0}", newHost.PacketSize);
						}
					}

					//Sets the ping interval. Defaults to 1000ms
					Console.Write("Ping interval: ({0}ms) ", newHost.Interval);
					var interval = Console.ReadLine();
					if (interval != string.Empty)
					{
						try
						{
							//See if we can convert it, but strip the 'ms' off if the user specified it. 
							newHost.Interval = Convert.ToInt32(interval.Replace("ms", ""));
						}
						catch (Exception)
						{
							Console.WriteLine("Invalid interval specified. Reverting back to {0}ms", newHost.Interval);
						}
					}
				}
				//All done. Add it to the options, then ask if they want to add another. 
				Options.Hosts[selectedIndex] = newHost;

				Log.Information("Edited host {0} with IP address {1}, Threshold {2}ms, Interval {3}ms, Packet Size {4}",
					newHost.HostName, newHost.IP, newHost.Threshold, newHost.Interval, newHost.PacketSize);
				Console.Write("Do you want to edit another? (y/N) ");
				var addMore = Console.ReadLine().ToLower();
				if (addMore == string.Empty || addMore == "n" || addMore == "no")
					done = true;
				WriteConfig();
			}
		}
		public static void AddNewHosts()
		{
			bool done = false;
			while (!done)
			{
				Host newHost = new Host();
				//Get host name from console input
				Console.Write("New host name (can be IP): ");
				var hostName = Console.ReadLine();
				if (CheckIfHostExists(hostName))
				{
					Console.WriteLine("Host already exists in configuration.");
					continue;
				}
				if (hostName == string.Empty)
					break;
				try
				{
					IPAddress[] iPs = Dns.GetHostAddresses(hostName);
					newHost.HostName = hostName;
					if (iPs.Length < 1)
						throw new Exception("Invalid host name.");
					newHost.IP = iPs[0].ToString();
					Console.WriteLine("Resolved to IP {0}", newHost.IP);
				}
				catch (Exception)
				{
					Console.WriteLine("Invalid host name.");
					continue;
				}

				//See if user wants to set up advanced options. Otherwise we use the defaults in the Host class
				Console.Write("Do you want to specify advanced options (threshold, packet size, interval)? (y/N) ");
				var advOpts = Console.ReadLine().ToLower();
				if (advOpts == "y" || advOpts == "yes")
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
						}
						catch (Exception)
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
				Log.Information("Added a new host {0} with IP address {1}, Threshold {2}ms, Interval {3}ms, Packet Size {4}",
					newHost.HostName, newHost.IP, newHost.Threshold, newHost.Interval, newHost.PacketSize);
				Console.Write("Do you want to add another? (y/N) ");
				var addMore = Console.ReadLine().ToLower();
				if (addMore == string.Empty || addMore == "n" || addMore == "no")
					done = true;
				WriteConfig();
			}
		}
		public static bool ReadJsonConfig()
		{
			if (File.Exists("./opts.json"))
			{
				try
				{
					var fileContents = File.ReadAllText("./opts.json");
					Options = JsonSerializer.Deserialize<Opts>(fileContents);
					return true;
				}
				catch (Exception)
				{
					Log.Error("Error loading configuration file");
				}
			}
			Options = new Opts
			{
				Hosts = new List<Host>()
			};
			return false;
		}
		public static void WriteConfig()
		{
			try
			{
				File.WriteAllText("./opts.json", JsonSerializer.Serialize(Options, new JsonSerializerOptions { WriteIndented = true }));
			}
			catch (Exception e)
			{
				Log.Error("Error saving configuration file");
				Log.Error(e.ToString());
			}
		}
		public static void ShutdownAllPingers()
		{
			foreach (var pinger in Pingers)
			{
				pinger.Stop();
			}
		}
		public static void PauseAllPingers()
		{
			Log.Information("Pausing all ping loggers");
			foreach (var pinger in Pingers)
			{
				pinger.Pause();
			}
		}
		public static void ResumeAllPingers()
		{
			Log.Information("Resuming all ping loggers");
			foreach (var pinger in Pingers)
			{
				pinger.Resume();
			}
		}
		protected static void Closing(object sender, ConsoleCancelEventArgs args)
		{
			//args.Cancel = true;
			WriteConfig();
			ShutdownAllPingers();
			Console.WriteLine("Closing application.");
			Environment.Exit(0);
		}
	}
}
