using System;
using System.Net;
using System.IO;
using Serilog;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;

namespace PingLogger
{
	class Program
	{
		private static Opts Options;
		private static readonly List<Pinger> Pingers = new List<Pinger>();
		static void Main()
		{
			Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			DateTime buildDate = new DateTime(2000, 1, 1)
									.AddDays(version.Build)
									.AddSeconds(version.Revision * 2);
			string displayableVersion = $"{version} ({buildDate})";
			Log.Logger = new LoggerConfiguration()
				.WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Literate)
				.CreateLogger();
			Log.Information("PingLogger {0} by Jack B.", displayableVersion);
			DoStartupTasks();
		}
		/// <summary>
		/// This is the bulk of the class.
		/// It pulls the configuration, keeps the console thread alive, and waits for input from the user.
		/// </summary>
		public static void DoStartupTasks()
		{
			var configured = ReadJsonConfig();
			var madeChanges = false;
			if (configured)
			{
				if (Options.Hosts.Count > 0)
				{
					Log.Information("Existing hosts detected.");
					try
					{
						Console.Write("Do you want to make changes? (y/N) ");
						string resp = WaitForInput.ReadLine(5000).ToLower();
						if (resp == "y" || resp == "yes")
						{
							UpdateSettings();
							madeChanges = true;
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
					Log.Information("No hosts configured.");
					AddNewHosts();
				}
			}
			else
			{
				Log.Information("No hosts configured.");
				AddNewHosts();
			}
			if (!madeChanges)
			{
				UpdatePingers();
				StartAllPingers();
			}
			// Override the Ctrl-C input so that we can capture it. 
			Console.TreatControlCAsInput = true;
			ConsoleKeyInfo cki;
			do
			{
				try
				{
					//Use the WaitForInputKey class to have a timeout so that we can keep looping the silent output above. 
					cki = WaitForInputKey.ReadKey(2000);
					if ((cki.Modifiers & ConsoleModifiers.Control) != 0 && cki.Key == ConsoleKey.C)
					{
						// Change this back to false so that the user input functions like normal while going through the options.
						Console.TreatControlCAsInput = false;
						ShutdownAllPingers();
						UpdateSettings(true);
						// Then set it back so that it can be captured again.
						Console.TreatControlCAsInput = true;
					}
					else
					{
						// If the user wants to have all ping loggers be silent, we'll print out the SilentOutput to the console instead. 
						if (Options.AllSilent || AllHostsSilent())
						{
							Console.ForegroundColor = Options.OutputColor;
							Console.Write(Options.SilentOutput);
							Console.WriteLine();
							Console.ResetColor();
						}
					}
				}
				catch (TimeoutException)
				{
				}
			} while (true);
		}
		public static bool AllHostsSilent()
		{
			foreach (var host in Options.Hosts)
			{
				if (!host.Silent)
					return false;
			}
			return true;
		}
		public static void UpdateSettings(bool interrupted = false)
		{
			// Discovered a bug where, if an additional Enter hasn't been sent, it will want an extra input.
			// This is a bit hacky, but it works.
			if (interrupted)
			{
				WaitForInputKey.DoEnter();
				Thread.Sleep(100);
			}
			var done = false;
			while (!done)
			{
				Console.ForegroundColor = Options.OutputColor;
				Console.WriteLine();
				Console.WriteLine("What would you like to do?");
				Console.WriteLine("[1] Close Application");
				Console.WriteLine("[2] Add a host");
				Console.WriteLine("[3] Edit a host");
				Console.WriteLine("[4] Remove a host");
				Console.WriteLine("[5] Refresh silent output message");
				Console.WriteLine("[6] Change silent output toggle");
				Console.WriteLine("[7] Change silent output color");
				if (Options.LoadOnStartup)
				{
					Console.WriteLine("[8] Remove application from system startup");
				}
				else
				{
					Console.WriteLine("[8] Add application to system startup");
				}
				if (interrupted)
				{
					Console.WriteLine("[9] Restart logging");
				}
				else
				{
					Console.WriteLine("[9] Start Logging");
				}
				Console.Write("Option: ");
				var resp = Console.ReadLine().ToLower();
				switch (resp)
				{
					case "1":
					case "":
						WriteConfig();
						Log.Warning("Closing application.");
						Thread.Sleep(200);
						Environment.Exit(0);
						break;
					case "2":
						AddNewHosts();
						break;
					case "3":
						EditHosts();
						break;
					case "4":
						RemoveHosts();
						break;
					case "5":
						if (!File.Exists("./silent.txt"))
						{
							Console.WriteLine("No silent.txt found. Please create one in the same directory as this program and try again.");
						}
						else
						{
							Options.SilentOutput = File.ReadAllText("./silent.txt");
						}
						break;
					case "6":
						if (Options.AllSilent)
						{
							Console.WriteLine("Application is currently set to only log to files.");
							Console.Write("Would you like to change this? (y/N) ");
							var changeSilent = Console.ReadLine().ToLower();
							if (changeSilent == "y" || changeSilent == "yes")
							{
								Options.AllSilent = false;
							}
						}
						else
						{
							Console.WriteLine("Application is currently set to log to both the console and files");
							Console.Write("Would you like to change this? (y/N) ");
							var changeSilent = Console.ReadLine().ToLower();
							if (changeSilent == "y" || changeSilent == "yes")
							{
								Options.AllSilent = true;
							}
						}
						WriteConfig();
						break;
					case "7":
						Console.ForegroundColor = ConsoleColor.White;
						Console.WriteLine("[1] White");
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine("[2] Red");
						Console.ForegroundColor = ConsoleColor.Blue;
						Console.WriteLine("[3] Blue");
						Console.ForegroundColor = ConsoleColor.Green;
						Console.WriteLine("[4] Green");
						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.WriteLine("[5] Yellow");
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.WriteLine("[6] Grey");
						Console.ForegroundColor = ConsoleColor.White;
						Console.Write("Color: ({0}) ", Options.OutputColor);
						var inputColor = Console.ReadLine();
						Options.OutputColor = inputColor switch
						{
							"1" => ConsoleColor.White,
							"2" => ConsoleColor.Red,
							"3" => ConsoleColor.Blue,
							"4" => ConsoleColor.Green,
							"5" => ConsoleColor.Yellow,
							"6" => ConsoleColor.Gray,
							_ => ConsoleColor.White,
						};
						Console.ForegroundColor = Options.OutputColor;
						Console.WriteLine("Color set to {0}", Options.OutputColor);
						WriteConfig();
						break;
					case "8":
						if(Options.LoadOnStartup)
						{
							Console.Write("Are you sure you want to remove this application from the system startup? (y/N) ");
							var startup = Console.ReadLine().ToLower();
							if(startup == "y" || startup == "yes")
							{
								RemoveStartupShortcut();
								Options.LoadOnStartup = false;
							}
						} else
						{
							Console.Write("Are you sure you want to add this application to the system startup? (y/N) ");
							var startup = Console.ReadLine().ToLower();
							if (startup == "y" || startup == "yes")
							{
								CreateStartupShortcut();
								Options.LoadOnStartup = true;
							}
						}
						WriteConfig();
						break;
					case "9":
						done = true;
						UpdatePingers();
						StartAllPingers();
						break;
					default:
						Console.WriteLine("Invalid selection");
						break;
				}
			}
		}
		public static void UpdatePingers()
		{
			Pingers.Clear();

			foreach (var host in Options.Hosts)
			{
				Pingers.Add(new Pinger(host, Options.AllSilent));
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
		public static void RemoveHosts()
		{
			bool done = false;
			while (!done)
			{
				Console.WriteLine();
				Console.WriteLine("Which host would you like to remove?");
				Console.WriteLine("[0] Cancel");
				for (int i = 0; i < Options.Hosts.Count; i++)
				{
					Console.WriteLine($"[{i + 1}] {Options.Hosts[i].HostName} ({ Options.Hosts[i].IP})");
				}
				Console.Write("Enter the number you wish to remove: ");
				var selectedHost = Console.ReadLine();
				int selectedIndex = 0;
				try
				{
					selectedIndex = Convert.ToInt32(selectedHost) - 1;
					if(selectedIndex == -1)
					{
						done = true;
						continue;
					}
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
				if (Options.Hosts.Count > 0)
				{
					Console.Write("Do you want to remove another? (y/N) ");
					var addMore = Console.ReadLine().ToLower();
					if (addMore == string.Empty || addMore == "n" || addMore == "no")
						done = true;
					WriteConfig();
				}
				else
				{
					done = true;
					WriteConfig();
				}
			}
		}
		public static void EditHosts()
		{
			bool done = false;
			while (!done)
			{
				Console.WriteLine();
				int selectedIndex;
				if (Options.Hosts.Count > 1)
				{
					Console.WriteLine("Which host would you like to edit?");
					Console.WriteLine("[0] Cancel");
					for (int i = 0; i < Options.Hosts.Count; i++)
					{
						Console.WriteLine($"[{i + 1}] {Options.Hosts[i].HostName} ({ Options.Hosts[i].IP})");
					}
					Console.Write("Enter the number you wish to edit: ");
					var selectedHost = Console.ReadLine();
					try
					{
						selectedIndex = Convert.ToInt32(selectedHost) - 1;
						if (selectedIndex == -1)
						{
							done = true;
							continue;
						}
					}
					catch (Exception)
					{
						Console.WriteLine("Invalid number selected.");
						continue;
					}
				}
				else
				{
					selectedIndex = 0;
				}

				Host editHost = Options.Hosts[selectedIndex];
				var validHost = false;
				while (!validHost)
				{
					//Get host name from console input
					Console.Write("New host name (can be IP): ({0}) ", editHost.HostName);
					var hostName = Console.ReadLine();
					if (hostName == string.Empty)
						break;
					try
					{
						IPAddress[] iPs = Dns.GetHostAddresses(hostName);
						editHost.HostName = hostName;
						if (iPs.Length < 1)
							throw new Exception("Invalid host name.");
						editHost.IP = iPs[0].ToString();
						Console.WriteLine("Resolved to IP {0}", editHost.IP);
						validHost = true;
					}
					catch (Exception)
					{
						Console.WriteLine("Invalid host name.");
					}
				}

				var silentDone = false;
				while (!silentDone)
				{
					Console.Write("Do you want this host to be silent?: (y/N/h) ");
					var silentResp = Console.ReadLine().ToLower();
					if (silentResp != string.Empty)
					{
						if (silentResp == "y" || silentResp == "yes" || silentResp == "true")
						{
							editHost.Silent = true;
							silentDone = true;
						}
						else if (silentResp == "h" || silentResp == "help")
						{
							Console.WriteLine("If this is set to 'yes', then the pings will only be logged to the file, not the console output.");
						}
						else if (silentResp == "n" || silentResp == "no" || silentResp == "false")
						{
							editHost.Silent = false;
							silentDone = true;
						}
						else
						{
							Console.WriteLine("Invalid response");
						}
					}
					else
					{
						silentDone = true;
					}
				}

				//See if user wants to set up advanced options. Otherwise we use the defaults in the Host class
				Console.Write("Do you want to specify advanced options (threshold, packet size, interval)? (y/N) ");
				var advOpts = Console.ReadLine().ToLower();
				if (advOpts == "y" || advOpts == "yes")
				{
					var validThreshold = false;
					while (!validThreshold)
					{
						//Sets the warning threshold. Defaults to 500ms;
						Console.Write($"Ping time warning threshold: ({editHost.Threshold}ms) ");
						var threshold = Console.ReadLine();
						if (threshold != string.Empty)
						{
							try
							{
								//See if we can convert it, but strip the 'ms' off if the user specified it. 
								editHost.Threshold = Convert.ToInt32(threshold.Replace("ms", ""));
								if (editHost.Threshold <= 0 || editHost.Threshold >= int.MaxValue)
								{
									Console.ForegroundColor = ConsoleColor.Red;
									Console.WriteLine("Invalid threshold specified.");
									Console.ResetColor();
									editHost.Threshold = 0;
								}
								else
								{
									validThreshold = true;
								}
							}
							catch (Exception)
							{
								Console.ForegroundColor = ConsoleColor.Red;
								Console.WriteLine("Invalid threshold specified.");
								Console.ResetColor();
							}
						}
						else
						{
							validThreshold = true;
						}
					}

					var validTimeout = false;
					while (!validTimeout)
					{
						Console.Write("Ping timeout: ({0}ms) ", editHost.Timeout);
						var timeout = Console.ReadLine();
						if (timeout != string.Empty)
						{
							try
							{
								//See if we can convert it, but strip the 'ms' off if the user specified it. 
								editHost.Timeout = Convert.ToInt32(timeout.Replace("ms", ""));
								if (editHost.Timeout <= 0 || editHost.Timeout >= int.MaxValue)
								{
									Console.ForegroundColor = ConsoleColor.Red;
									Console.WriteLine("Invalid timeout specified.");
									Console.ResetColor();
									editHost.Timeout = 0;
								}
								else
								{
									validTimeout = true;
								}
							}
							catch (Exception)
							{
								Console.ForegroundColor = ConsoleColor.Red;
								Console.WriteLine("Invalid timeout specified.");
								Console.ResetColor();
							}
						}
						else
						{
							validTimeout = true;
						}
					}

					var validPacketSize = false;
					while (!validPacketSize)
					{
						Console.Write("Packet size in bytes: ({0}) ", editHost.PacketSize);
						var packetSize = Console.ReadLine();
						if (packetSize != string.Empty)
						{
							try
							{
								editHost.PacketSize = Convert.ToInt32(packetSize);
								// Maximum packet size is 65,500 bytes. Can't go higher than that.
								if (editHost.PacketSize <= 0 || editHost.PacketSize >= 65500)
								{
									Console.ForegroundColor = ConsoleColor.Red;
									Console.WriteLine("Invalid packet size specified.");
									Console.ResetColor();
									editHost.PacketSize = 0;
								}
								else
								{
									validPacketSize = true;
								}
							}
							catch (Exception)
							{
								Console.ForegroundColor = ConsoleColor.Red;
								Console.WriteLine("Invalid packet size specified.");
								Console.ResetColor();
							}
						}
						else
						{
							validPacketSize = true;
						}
					}

					var validInterval = false;
					while (!validInterval)
					{
						Console.Write("Ping interval: ({0}ms) ", editHost.Interval);
						var interval = Console.ReadLine();
						if (interval != string.Empty)
						{
							try
							{
								editHost.Interval = Convert.ToInt32(interval.Replace("ms", ""));
								if (editHost.Interval < 500 || editHost.Interval >= int.MaxValue)
								{
									Console.ForegroundColor = ConsoleColor.Red;
									Console.WriteLine("Invalid interval specified.");
									Console.ResetColor();
									editHost.Interval = 0;
								}
								else
								{
									validInterval = true;
								}
							}
							catch (Exception)
							{
								Console.ForegroundColor = ConsoleColor.Red;
								Console.WriteLine("Invalid interval specified.");
								Console.ResetColor();
							}
						}
						else
						{
							validInterval = true;
						}
					}
				}
				//All done. Add it to the options, then ask if they want to add another. 
				Options.Hosts[selectedIndex] = editHost;

				Log.Information("Edited host {0} with IP address {1}, Threshold {2}ms, Interval {3}ms, Packet Size {4}",
					editHost.HostName, editHost.IP, editHost.Threshold, editHost.Interval, editHost.PacketSize);
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
				Console.WriteLine();
				Host newHost = new Host();
				var validHost = false;
				int tries = 0;
				while (!validHost)
				{
					if (tries >= 4)
					{
						done = true;
						break;
					}
					Console.Write("New host name (can be IP): ");
					var hostName = Console.ReadLine();
					if (CheckIfHostExists(hostName))
					{
						Console.WriteLine("Host already exists in configuration.");
						continue;
					}
					if (hostName != string.Empty)
					{
						try
						{
							IPAddress[] iPs = Dns.GetHostAddresses(hostName);
							newHost.HostName = hostName;
							if (iPs.Length < 1)
								throw new Exception("Invalid host name.");
							if (iPs.Length > 1)
							{
								foreach (var ip in iPs)
								{
									if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
									{
										newHost.IP = ip.ToString();
										break;
									}
								}
							}
							else
							{
								newHost.IP = iPs[0].ToString();
							}
							Console.WriteLine("Resolved to IP {0}", newHost.IP);
							validHost = true;
						}
						catch (Exception)
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.WriteLine("Invalid host name.");
							tries++;
						}
					}
					else
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine("Invalid host name.");
						Console.ResetColor();
						tries++;
					}
				}

				if (tries >= 4)
				{
					break;
				}
				var silentDone = false;
				while (!silentDone)
				{
					Console.Write("Do you want this host to be silent?: (y/N/h) ");
					var silentResp = Console.ReadLine().ToLower();
					if (silentResp != string.Empty)
					{
						if (silentResp == "y" || silentResp == "yes" || silentResp == "true")
						{
							newHost.Silent = true;
							silentDone = true;
						}
						else if (silentResp == "h" || silentResp == "help")
						{
							Console.WriteLine("If this is set to 'yes', then the pings will only be logged to the file, not the console output.");
						}
						else if (silentResp == "n" || silentResp == "no" || silentResp == "false")
						{
							newHost.Silent = false;
							silentDone = true;
						}
						else
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.WriteLine("Invalid response");
							Console.ResetColor();
						}
					}
					else
					{
						silentDone = true;
					}
				}
				//See if user wants to set up advanced options. Otherwise we use the defaults in the Host class
				Console.Write("Do you want to specify advanced options (threshold, packet size, interval)? (y/N) ");
				var advOpts = Console.ReadLine().ToLower();
				if (advOpts == "y" || advOpts == "yes")
				{
					var validThreshold = false;
					while (!validThreshold)
					{
						Console.Write("Ping time warning threshold: (500ms) ");
						var threshold = Console.ReadLine();
						if (threshold == string.Empty)
						{
							newHost.Threshold = 500;
							validThreshold = true;
						}
						else
						{
							try
							{
								newHost.Threshold = Convert.ToInt32(threshold.Replace("ms", ""));
								if (newHost.Threshold <= 0 || newHost.Threshold >= int.MaxValue)
								{
									Console.ForegroundColor = ConsoleColor.Red;
									Console.WriteLine("Invalid threshold specified.");
									Console.ResetColor();
									newHost.Threshold = 0;
								}
								else
								{
									validThreshold = true;
								}
							}
							catch (Exception)
							{
								Console.ForegroundColor = ConsoleColor.Red;
								Console.WriteLine("Invalid threshold specified.");
								Console.ResetColor();
							}
						}
					}

					var validTimeout = false;
					while (!validTimeout)
					{
						Console.Write("Ping timeout: (1000ms) ");
						var timeout = Console.ReadLine();
						if (timeout == string.Empty)
						{
							newHost.Timeout = 1000;
							validTimeout = true;
						}
						else
						{
							try
							{
								newHost.Timeout = Convert.ToInt32(timeout.Replace("ms", ""));
								if (newHost.Timeout <= 0 || newHost.Timeout >= int.MaxValue)
								{
									Console.ForegroundColor = ConsoleColor.Red;
									Console.WriteLine("Invalid timeout specified.");
									Console.ResetColor();
									newHost.Timeout = 0;
								}
								else
								{
									validTimeout = true;
								}
							}
							catch (Exception)
							{
								Console.ForegroundColor = ConsoleColor.Red;
								Console.WriteLine("Invalid timeout specified.");
								Console.ResetColor();
							}
						}
					}

					var validPacketSize = false;
					while (!validPacketSize)
					{
						Console.Write("Packet size in bytes: (32) ");
						var packetSize = Console.ReadLine();
						if (packetSize == string.Empty)
						{
							newHost.PacketSize = 32;
							validPacketSize = true;
						}
						else
						{
							try
							{
								newHost.PacketSize = Convert.ToInt32(packetSize);
								// Maximum packet size is 65,535 bytes. Can't go higher than that.
								if (newHost.PacketSize <= 0 || newHost.PacketSize >= 65500)
								{
									Console.ForegroundColor = ConsoleColor.Red;
									Console.WriteLine("Invalid packet size specified.");
									Console.ResetColor();
									newHost.PacketSize = 0;
								}
								else
								{
									validPacketSize = true;
								}
							}
							catch (Exception)
							{
								Console.ForegroundColor = ConsoleColor.Red;
								Console.WriteLine("Invalid packet size specified.");
								Console.ResetColor();
							}
						}
					}
					var validInterval = false;
					while (!validInterval)
					{
						Console.Write("Ping interval: (1000ms) ");
						var interval = Console.ReadLine();
						if (interval == string.Empty)
						{
							newHost.Interval = 1000;
							validInterval = true;
						}
						else
						{
							try
							{
								newHost.Interval = Convert.ToInt32(interval.Replace("ms", ""));
								if (newHost.Interval < 500 || newHost.Interval >= int.MaxValue)
								{
									Console.ForegroundColor = ConsoleColor.Red;
									Console.WriteLine("Invalid interval specified.");
									Console.ResetColor();
									newHost.Interval = 0;
								}
								else
								{
									validInterval = true;
								}
							}
							catch (Exception)
							{
								Console.ForegroundColor = ConsoleColor.Red;
								Console.WriteLine("Invalid interval specified.");
								Console.ResetColor();
							}
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
					var silentFile = File.ReadAllText("./silent.txt");
					if (silentFile != Options.SilentOutput)
					{
						Log.Information("The file silent.txt was changed since last ran, updating.");
						Options.SilentOutput = silentFile;
					}
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
		public static void CreateStartupShortcut()
		{
			try
			{
				var batchPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\PingLogger.bat";
				var exePath = Environment.CurrentDirectory + "\\";
				var exeName = AppDomain.CurrentDomain.FriendlyName + ".exe";

				var batchScript = "@echo off" + Environment.NewLine;
				batchScript += "CD \"" + exePath + "\"" + Environment.NewLine;
				batchScript += "START \"\" \".\\" + exeName + "\"";

				Log.Information("Writing startup script to {0}", batchPath);
				File.WriteAllText(batchPath, batchScript);
			} catch (Exception e)
			{
				Log.Error("Couldn't create startup shortcut.");
				Log.Error(e.ToString());
			}
		}
		public static void RemoveStartupShortcut()
		{
			try
			{
				var fileDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\PingLogger.bat";
				Log.Information("Removing startup script {0}", fileDir);
				File.Delete(fileDir);
			} catch (Exception e)
			{
				Log.Error("Couldn't remove startup script.");
				Log.Error(e.ToString());
			}
		}
		public static void WriteConfig()
		{
			try
			{
				if (File.Exists("./opts.json"))
					File.SetAttributes("./opts.json", FileAttributes.Normal);

				File.WriteAllText("./opts.json", JsonSerializer.Serialize(Options, new JsonSerializerOptions { WriteIndented = true }));
				File.WriteAllText("./silent.txt", Options.SilentOutput);
				File.SetAttributes("./opts.json", FileAttributes.Hidden | FileAttributes.ReadOnly);
			}
			catch (Exception e)
			{
				Log.Error("Error saving configuration file");
				Log.Error(e.ToString());
			}
		}
		public static void ShutdownAllPingers()
		{
			Log.Information("Shutting down all ping loggers.");
			foreach (var pinger in Pingers)
			{
				pinger.Stop();
			}
		}
		public static void StartAllPingers()
		{
			Log.Information("Starting all ping loggers.");
			foreach (var pinger in Pingers)
			{
				pinger.Start();
			}
		}
	}
}
