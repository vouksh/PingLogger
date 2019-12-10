using PingLogger.CLI.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;

namespace PingLogger.CLI
{
	class Program
	{
		private static Opts Options;
		private static readonly List<Pinger> Pingers = new List<Pinger>();
		private static Mutex mutex = null;
		private static bool isClosing = false;
		static void Main()
		{
			//Only allow one instance of the program to run. 
			const string AppName = "PingLogger";
			bool isNewInstance;
			mutex = new Mutex(true, AppName, out isNewInstance);
			if (!isNewInstance)
			{
				ColoredOutput.WriteLine("##red##Error, program already running.");
				Thread.Sleep(1000);
				Environment.Exit(0);
			}

			Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			ColoredOutput.WriteLine($"##cyan##{AppName} ##white##v{version}### by ##cyan##Jack B.", true);
			Console.Title = "PingLogger - Testing in progress do not close";
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
					ColoredOutput.WriteLine("Existing hosts detected.", true);
					try
					{
						ColoredOutput.Write("Do you want to make changes? ###(##green##y###/##red##N###) ", true);
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
						ColoredOutput.WriteLine("No input detected. Skipping addition of new hosts", true);

					}
				}
				else
				{
					ColoredOutput.WriteLine("No hosts configured.", true);
					AddNewHosts();
					UpdateSettings();
					madeChanges = true;
				}
			}
			else
			{
				ColoredOutput.WriteLine("No hosts configured.", true);
				AddNewHosts();
				UpdateSettings();
				madeChanges = true;
			}
			ColoredOutput.WriteLine("##white##Press ##red##Ctrl-C##white## to access program options.", true);
			if (!madeChanges)
			{
				UpdatePingers();
				StartAllPingers();
			}

			AppDomain.CurrentDomain.ProcessExit += new EventHandler(ProcessExit);
			// Override the Ctrl-C input so that we can capture it. 
			var title = Console.Title;
			Console.Title = title + " - Press Ctrl-C to access options";
			Console.TreatControlCAsInput = true;
			ConsoleKeyInfo cki;
			do
			{
				Console.CursorVisible = false;
				try
				{
					//Use the WaitForInputKey class to have a timeout so that we can keep looping the silent output above. 
					cki = WaitForInput.ReadKey(2000);
					if ((cki.Modifiers & ConsoleModifiers.Control) != 0 && cki.Key == ConsoleKey.C)
					{
						Console.CursorVisible = true;
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
						if (Options.AllSilent)
						{
							ColoredOutput.WriteMultiLine(Options.SilentOutput, true);
							ColoredOutput.WriteLine("", true);
							Console.ResetColor();
						}
					}
				}
				catch (TimeoutException)
				{
				}
			} while (!isClosing);
		}
		public static void UpdateSettings(bool interrupted = false)
		{
			// Discovered a bug where, if an additional Enter hasn't been sent, it will want an extra input.
			// This is a bit hacky, but it works.
			if (interrupted)
			{
				SendKey.DoEnter();
				Thread.Sleep(100);
			}
			var done = false;
			while (!done)
			{
				ColoredOutput.WriteLine("What would you like to do?", true);
				ColoredOutput.WriteLine("[1] ##darkred##Close Application");
				ColoredOutput.WriteLine("[2] ##white##Add a host");
				ColoredOutput.WriteLine("[3] ##white##Edit a host");
				ColoredOutput.WriteLine("[4] ##red##Remove a host");
				ColoredOutput.WriteLine("[5] ##cyan##Change silent output toggle");
				ColoredOutput.WriteLine("[6] ##white##Change how many days to keep logs");
				if (Options.LoadOnStartup)
				{
					ColoredOutput.WriteLine("[7] ##darkyellow##Remove application from system startup");
				}
				else
				{
					ColoredOutput.WriteLine("[7] ##darkyellow##Add application to system startup");
				}
				if (interrupted)
				{
					ColoredOutput.WriteLine("[8] ##green##Restart logging");
				}
				else
				{
					ColoredOutput.WriteLine("[8] ##green##Start Logging");
				}
				ColoredOutput.Write("##white##Option: ");
				var resp = Console.ReadLine().ToLower();
				switch (resp)
				{
					case "1":
					case "":
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
						if (Options.AllSilent)
						{
							ColoredOutput.WriteLine("Application is currently set to ##cyan##only### log to files.");
							ColoredOutput.Write("##yellow##Would you like to change this? ###(##green##y###/##red##N###) ");
							var changeSilent = Console.ReadLine().ToLower();
							if (changeSilent == "y" || changeSilent == "yes")
							{
								Options.AllSilent = false;
							}
						}
						else
						{
							ColoredOutput.WriteLine("Application is currently set to log to ##cyan##both### the console and files");
							ColoredOutput.Write("##yellow##Would you like to change this? ###(##green##y###/##red##N###) ");
							var changeSilent = Console.ReadLine().ToLower();
							if (changeSilent == "y" || changeSilent == "yes")
							{
								Options.AllSilent = true;
							}
						}
						WriteConfig();
						break;
					case "6":
						ColoredOutput.WriteLine($"Currently set to keep up to ##white##{Options.DaysToKeepLogs}### days of log files.");
						ColoredOutput.Write($"Days to keep (##white##{Options.DaysToKeepLogs}###): ##white##");
						int days = Options.DaysToKeepLogs;
						try
						{
							days = Convert.ToInt32(Console.ReadLine());
						}
						catch (FormatException)
						{
							ColoredOutput.WriteLine("##red##Error, response must be in numerical format.");
							ColoredOutput.WriteLine($"##red##Leaving setting at {Options.DaysToKeepLogs} days");
						}
						Options.DaysToKeepLogs = days;
						break;
					case "7":
						if (Options.LoadOnStartup)
						{
							ColoredOutput.Write("Are you sure you want to ##red##remove### this application from the system startup? ###(##green##y###/##red##N###) ");
							var startup = Console.ReadLine().ToLower();
							if (startup == "y" || startup == "yes")
							{
								RemoveStartupShortcut();
								Options.LoadOnStartup = false;
							}
						}
						else
						{
							ColoredOutput.Write("Are you sure you want to ##green##add### this application to the system startup? ###(##green##y###/##red##N###) ");
							var startup = Console.ReadLine().ToLower();
							if (startup == "y" || startup == "yes")
							{
								CreateStartupShortcut();
								Options.LoadOnStartup = true;
							}
						}
						WriteConfig();
						break;
					case "8":
						done = true;
						UpdatePingers();
						StartAllPingers();
						break;
					default:
						ColoredOutput.WriteLine("##red##Invalid selection");
						break;
				}
			}
		}
		public static void UpdatePingers()
		{
			Pingers.Clear();

			foreach (var host in Options.Hosts)
			{
				Pingers.Add(new Pinger(host, Options.AllSilent, Options.DaysToKeepLogs));
			}
		}
		public static bool CheckIfHostExists(string hostName, bool ignoreFirstFind = false)
		{
			int occurances = 0;
			if (Options.Hosts?.Count > 0)
			{
				foreach (var host in Options.Hosts)
				{
					if (host.HostName == hostName)
					{
						occurances++;
					}
					if (occurances > 0 & !ignoreFirstFind)
					{
						return true;
					}
					if (occurances >= 1 & ignoreFirstFind)
					{
						return true;
					}
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
				ColoredOutput.WriteLine("Which host would you like to ##red##remove###?");
				ColoredOutput.WriteLine("[0] ##darkyellow##Cancel");
				for (int i = 0; i < Options.Hosts.Count; i++)
				{
					ColoredOutput.WriteLine($"[{i + 1}] ##darkcyan##{Options.Hosts[i].HostName} ###(##darkcyan##{ Options.Hosts[i].IP}###)");
				}
				ColoredOutput.Write("Enter the number you wish to ##red##remove###: ");
				var selectedHost = Console.ReadLine();
				int selectedIndex = 0;
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
					ColoredOutput.WriteLine("##darkred##Invalid number selected.");
				}
				ColoredOutput.Write($"Are you sure you want to remove host ##darkcyan##{Options.Hosts[selectedIndex].HostName}###? ###(##red##y###/##green##N###) ");
				var resp = Console.ReadLine().ToLower();
				if (resp != string.Empty || resp == "y" || resp == "yes")
				{
					ColoredOutput.WriteLine($"Removed host ##cyan##{Options.Hosts[selectedIndex].HostName}### with IP address ##cyan##{Options.Hosts[selectedIndex].IP}###, Threshold ##cyan##{Options.Hosts[selectedIndex].Threshold}ms###, Interval ##cyan##{Options.Hosts[selectedIndex].Interval}ms###, Packet Size ##cyan##{Options.Hosts[selectedIndex].PacketSize}###", true);
					Options.Hosts.RemoveAt(selectedIndex);
				}
				if (Options.Hosts.Count > 0)
				{
					ColoredOutput.Write("Do you want to remove another? ###(##red##y###/##green##N###) ");
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
					ColoredOutput.WriteLine("Which host would you like to ##yellow##edit###?");
					ColoredOutput.WriteLine("[0] ##darkyellow##Cancel");
					for (int i = 0; i < Options.Hosts.Count; i++)
					{
						ColoredOutput.WriteLine($"[{i + 1}] ##darkcyan##{Options.Hosts[i].HostName} ###(##darkcyan##{ Options.Hosts[i].IP}###)");
					}
					ColoredOutput.Write("Enter the number you wish to ##yellow##edit###: ");
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
						ColoredOutput.WriteLine("##darkred##Invalid number selected.");
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
					ColoredOutput.Write($"New host name (can be IP): (##darkcyan##{editHost.HostName}###) ");
					var hostName = Console.ReadLine();
					if (hostName == string.Empty)
						break;

					if (CheckIfHostExists(hostName, true))
					{
						ColoredOutput.WriteLine("##darkred##Host already exists in configuration.");
						continue;
					}
					try
					{
						IPAddress[] iPs = Dns.GetHostAddresses(hostName);
						editHost.HostName = hostName;
						if (iPs.Length < 1)
							throw new Exception("Invalid host name.");
						editHost.IP = iPs[0].ToString();
						ColoredOutput.WriteLine($"##green##Resolved to IP ##darkcyan##{editHost.IP}");
						validHost = true;
					}
					catch (Exception)
					{
						ColoredOutput.WriteLine("##darkred##Invalid host name.");
					}
				}

				var validThreshold = false;
				while (!validThreshold)
				{
					//Sets the warning threshold. Defaults to 500ms;
					ColoredOutput.Write($"Ping time warning threshold: (##green##{editHost.Threshold}ms###) ");
					var threshold = Console.ReadLine().ToLower();
					if (threshold != string.Empty)
					{
						try
						{
							//See if we can convert it, but strip the 'ms' off if the user specified it. 
							editHost.Threshold = Convert.ToInt32(threshold.Replace("ms", ""));
							if (editHost.Threshold <= 0 || editHost.Threshold >= int.MaxValue)
							{
								ColoredOutput.WriteLine("##red##Invalid threshold specified.");
								editHost.Threshold = 0;
							}
							else
							{
								validThreshold = true;
							}
						}
						catch (Exception)
						{
							ColoredOutput.WriteLine("##darkred##Invalid threshold specified.");
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
					ColoredOutput.Write($"Ping timeout: (##green##{editHost.Timeout}ms###) ");
					var timeout = Console.ReadLine().ToLower();
					if (timeout != string.Empty)
					{
						try
						{
							//See if we can convert it, but strip the 'ms' off if the user specified it. 
							editHost.Timeout = Convert.ToInt32(timeout.Replace("ms", ""));
							if (editHost.Timeout <= 0 || editHost.Timeout >= int.MaxValue)
							{
								ColoredOutput.WriteLine("##darkred##Invalid timeout specified.");
								editHost.Timeout = 0;
							}
							else
							{
								validTimeout = true;
							}
						}
						catch (Exception)
						{
							ColoredOutput.WriteLine("##darkred##Invalid timeout specified.");
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
					ColoredOutput.Write($"Packet size in bytes: (##green##{editHost.PacketSize}###) ");
					var packetSize = Console.ReadLine();
					if (packetSize != string.Empty)
					{
						try
						{
							editHost.PacketSize = Convert.ToInt32(packetSize);
							// Maximum packet size is 65,500 bytes. Can't go higher than that.
							if (editHost.PacketSize <= 0 || editHost.PacketSize > 65500)
							{
								ColoredOutput.WriteLine("##darkred##Invalid packet size specified.");
								editHost.PacketSize = 0;
							}
							else
							{
								validPacketSize = true;
							}
						}
						catch (Exception)
						{
							ColoredOutput.WriteLine("##darkred##Invalid packet size specified.");
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
					ColoredOutput.Write($"Ping interval: (##green##{editHost.Interval}ms###) ");
					var interval = Console.ReadLine().ToLower();
					if (interval != string.Empty)
					{
						try
						{
							editHost.Interval = Convert.ToInt32(interval.Replace("ms", ""));
							if (editHost.Interval < 500 || editHost.Interval >= int.MaxValue)
							{
								ColoredOutput.WriteLine("##red##Invalid interval specified.");
								editHost.Interval = 0;
							}
							else
							{
								validInterval = true;
							}
						}
						catch (Exception)
						{
							ColoredOutput.WriteLine("##red##Invalid interval specified.");
						}
					}
					else
					{
						validInterval = true;
					}
				}
				//All done. Add it to the options, then ask if they want to add another. 
				Options.Hosts[selectedIndex] = editHost;

				ColoredOutput.WriteLine($"Edited host ##cyan##{editHost.HostName}### with IP address ##cyan##{editHost.IP}###, Threshold ##cyan##{editHost.Threshold}ms###, Interval ##cyan##{editHost.Interval}ms###, Packet Size ##cyan##{editHost.PacketSize}", true);
				ColoredOutput.Write("Do you want to edit another? ###(##red##y###/##green##N###) ");
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
						ColoredOutput.WriteLine("##darkred##Host already exists in configuration.");
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
							ColoredOutput.WriteLine($"##green##Resolved to IP ##darkcyan##{newHost.IP}");
							validHost = true;
						}
						catch (Exception)
						{
							ColoredOutput.WriteLine("##darkred##Invalid host name.");
							tries++;
						}
					}
					else
					{
						ColoredOutput.WriteLine("##darkred##Invalid host name.");
						tries++;
					}
				}

				if (tries >= 4)
				{
					break;
				}

				//See if user wants to set up advanced options. Otherwise we use the defaults in the Host class
				ColoredOutput.Write("Do you want to specify ##red##advanced### options (threshold, timeout, packet size, interval)? ###(##red##y###/##green##N###) ");
				var advOpts = Console.ReadLine().ToLower();
				if (advOpts == "y" || advOpts == "yes")
				{
					var validThreshold = false;
					while (!validThreshold)
					{
						ColoredOutput.Write($"Ping time warning threshold: (##green##{newHost.Threshold}ms###) ");
						var threshold = Console.ReadLine().ToLower();
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
									ColoredOutput.WriteLine("##red##Invalid threshold specified.");
									newHost.Threshold = 0;
								}
								else
								{
									validThreshold = true;
								}
							}
							catch (Exception)
							{
								ColoredOutput.WriteLine("##red##Invalid threshold specified.");
							}
						}
					}

					var validTimeout = false;
					while (!validTimeout)
					{
						ColoredOutput.Write($"Ping timeout: (##green##{newHost.Timeout}ms###) ");
						var timeout = Console.ReadLine().ToLower();
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
									ColoredOutput.WriteLine("##darkred##Invalid timeout specified.");
									newHost.Timeout = 0;
								}
								else
								{
									validTimeout = true;
								}
							}
							catch (Exception)
							{
								ColoredOutput.WriteLine("##darkred##Invalid timeout specified.");
							}
						}
					}

					var validPacketSize = false;
					while (!validPacketSize)
					{
						ColoredOutput.Write($"Packet size in bytes: (##green##{newHost.PacketSize}###) ");
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
								// Maximum packet size is 65,500 bytes. Can't go higher than that.
								if (newHost.PacketSize <= 0 || newHost.PacketSize > 65500)
								{
									ColoredOutput.WriteLine("##darkred##Invalid packet size specified.");
									newHost.PacketSize = 0;
								}
								else
								{
									validPacketSize = true;
								}
							}
							catch (Exception)
							{
								ColoredOutput.WriteLine("##darkred##Invalid packet size specified.");
							}
						}
					}
					var validInterval = false;
					while (!validInterval)
					{
						ColoredOutput.Write($"Ping interval: (##green##{newHost.Interval}ms###) ");
						var interval = Console.ReadLine().ToLower();
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
									ColoredOutput.WriteLine("##red##Invalid interval specified.");
									newHost.Interval = 0;
								}
								else
								{
									validInterval = true;
								}
							}
							catch (Exception)
							{
								ColoredOutput.WriteLine("##red##Invalid interval specified.");
							}
						}
					}
				}
				//All done. Add it to the options, then ask if they want to add another. 
				Options.Hosts.Add(newHost);
				ColoredOutput.WriteLine($"Added a new host ##cyan##{newHost.HostName}### with IP address ##cyan##{newHost.IP}###, Threshold ##cyan##{newHost.Threshold}ms###, Interval ##cyan##{newHost.Interval}ms###, Packet Size ##cyan##{newHost.PacketSize}###", true);
				ColoredOutput.Write("Do you want to add another? ###(##red##y###/##green##N###) ", true);
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
					var silentFile = string.Empty;
					try
					{
						silentFile = File.ReadAllText("./silent.txt");
					}
					catch (FileNotFoundException)
					{
						ColoredOutput.WriteLine("##red##Error: silent.txt not found, setting back to default.", true);
						silentFile = "##red##...............................................................................\r\n##red##.....##white##xxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxx##red##.....\r\n##red##.....##white##xxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxx##red##.....\r\n##red##.........##white##xxxxx##red##.........##white##xxxxx##red##..............##white##xxxxx##red##..................##white##xxxxx##red##.........\r\n##red##.........##white##xxxxx##red##.........##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.........##white##xxxxx##red##.........\r\n##red##.........##white##xxxxx##red##.........##white##xxxxx##red##.......................##white##xxxxx##red##.........##white##xxxxx##red##.........\r\n##red##.........##white##xxxxx##red##.........##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.........##white##xxxxx##red##.........\r\n##red##.........##white##xxxxx##red##.........##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.........##white##xxxxx##red##.........\r\n##red##...............................................................................";
					}
					if (silentFile != Options.SilentOutput)
					{
						ColoredOutput.WriteLine("The file silent.txt was changed since last ran, updating.", true);
						Options.SilentOutput = silentFile;
					}
					if (Options.LoadOnStartup)
					{
						CreateStartupShortcut(true);
					}
					else
					{
						RemoveStartupShortcut();
					}
					return true;
				}
				catch (FileNotFoundException)
				{
					ColoredOutput.WriteLine("##red##Error loading configuration file", true);
					ColoredOutput.WriteLine("##red##File not found.", true);
				}
				catch (JsonException)
				{
					ColoredOutput.WriteLine("##red##Error loading configuration file", true);
					ColoredOutput.WriteLine("##red##File is formatted incorrectly.", true);
				}
			}
			Options = new Opts
			{
				Hosts = new List<Host>()
			};
			return false;
		}
		public static void CreateStartupShortcut(bool isStartup = false)
		{
			try
			{
				var batchPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\PingLogger.bat";
				if (!File.Exists(batchPath))
				{
					if (isStartup)
					{
						ColoredOutput.WriteLine("Startup script removed since program last started.", true);
					}
					var exePath = Environment.CurrentDirectory + "\\";
					var exeName = AppDomain.CurrentDomain.FriendlyName + ".exe";

					//Find which drive the logger is running off of and change to it in the startup script.
					var loggerDrive = exePath.Substring(0, 2);

					var batchScript = "@echo off" + Environment.NewLine;
					batchScript += loggerDrive + Environment.NewLine;
					batchScript += "CD \"" + exePath + "\"" + Environment.NewLine;
					batchScript += "START \"\" \".\\" + exeName + "\"";

					ColoredOutput.WriteLine($"Writing startup script to ##darkcyan##{batchPath}", true);
					File.WriteAllText(batchPath, batchScript);
				}
			}
			catch (Exception e)
			{
				ColoredOutput.WriteLine("##red##Couldn't create startup shortcut.", true);
				ColoredOutput.WriteLine(e.ToString(), true);
			}
		}
		public static void RemoveStartupShortcut()
		{
			try
			{
				var fileDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\PingLogger.bat";
				if (File.Exists(fileDir))
				{
					ColoredOutput.WriteLine($"Removing startup script ##darkcyan##{fileDir}", true);
					File.Delete(fileDir);
				}
			}
			catch (Exception e)
			{
				ColoredOutput.WriteLine("##red##Couldn't remove startup script.", true);
				ColoredOutput.WriteLine(e.ToString(), true);
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
				ColoredOutput.WriteLine("Error saving configuration file", true);
				ColoredOutput.WriteLine(e.ToString(), true);
			}
		}
		public static void ShutdownAllPingers()
		{
			Options.Hosts.Clear();
			ColoredOutput.WriteLine("Shutting down all ping loggers.", true);
			foreach (var pinger in Pingers)
			{
				Options.Hosts.Add(pinger.UpdateHost());
				pinger.Dispose();
			}
			Pingers.Clear();
		}
		public static void StartAllPingers()
		{
			ColoredOutput.WriteLine("Starting all ping loggers.", true);
			foreach (var pinger in Pingers)
			{
				pinger.Start();
			}
		}
		private static void ProcessExit(object sender, EventArgs e)
		{
			Console.WriteLine();
			if (Pingers.Count > 0)
				ShutdownAllPingers();

			WriteConfig();
			ColoredOutput.WriteLine("##red##Closing application.", true);
			Thread.Sleep(200);
		}
	}
}
