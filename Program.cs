using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Threading;

namespace PingLogger
{
	class Program
	{
		private static string fileName = string.Empty;
		private static string hostName = string.Empty;
		private static int pingCount = 0;
		private static IPAddress IP;
		private static LogWriter Log;
		private static int WarnThreshold = 200;
		static void Main(string[] args)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("PingLogger v0.1 by Jack Butler\n");
			Console.CancelKeyPress += new ConsoleCancelEventHandler(Closing);
			Console.ResetColor();
			var configured = ReadFromConfigFile();
			var running = true;
			var skip = false;
			if(configured && args.Length > 0)
			{
				Console.WriteLine("Configuration file used, ignoring command line arguments.");
			}
			if (!configured)
			{
				running = ProcessArgs(args);
			}
			if (hostName == string.Empty && running)
			{
				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.WriteLine("No hostname or IP address specified.");
				Console.ResetColor();
				Console.Write("Please enter a hostname: ");
				hostName = Console.ReadLine();
				if (hostName == string.Empty)
				{
					Console.WriteLine("No host specified.");
					running = false;
				}
				else
				{
					Console.Write("Log file (leave blank to skip logging to file): ");
					fileName = Console.ReadLine();
					Console.Write("Ping count (enter 0 or press enter for infinite): ");
					var cnt = Console.ReadLine();
					if (cnt != string.Empty)
					{
						try
						{
							pingCount = Convert.ToInt32(cnt);
						}
						catch (Exception)
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.WriteLine("Invalid count entry, ignoring");
							pingCount = 0;
						}
					}
					Console.Write("Warning Threshold in MS (default 200): ");
					var thresh = Console.ReadLine();
					if(thresh != string.Empty)
					{
						try
						{
							WarnThreshold = Convert.ToInt32(thresh);
						} catch(Exception)
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.WriteLine("Invalid threshold entry, ignoring");
							WarnThreshold = 200;
						}
					}
				}
			}
			string resolved = string.Empty;
			IPAddress[] iPs = Dns.GetHostAddresses(hostName);
			IP = iPs[0];
			if(pingCount > 0)
			{
				Console.WriteLine("Pinging {0} ({1}) {2} times", hostName, IP.ToString(), pingCount);
			} else
			{
				Console.WriteLine("Pinging {0} ({1}), press Ctrl-C to quit.", hostName, IP.ToString());
			}
			int loops = 0;
			Console.WriteLine();
			if (fileName != string.Empty)
			{
				Log = new LogWriter(fileName);
				Log.WriteLog("Running with the following options: ");
				Log.WriteLog("Host: " + hostName + " (" + IP.ToString() + ")");
				if (pingCount > 0)
					Log.WriteLog("Ping Count: " + pingCount);
				Log.WriteLog("Warning Threshold: " + WarnThreshold + "ms");
			}
			while (running)
			{
				if (pingCount > 0)
					loops++;
				Ping pingSender = new Ping();
				pingSender.PingCompleted += new PingCompletedEventHandler(WriteStatus);
				PingOptions options = new PingOptions();
				AutoResetEvent waiter = new AutoResetEvent(false);

				options.DontFragment = true;

				string data = "xxxxxxxpingloggerincsharpxxxxxxx";
				byte[] buffer = Encoding.ASCII.GetBytes(data);
				int timeout = 120;
				pingSender.SendAsync(IP, timeout, buffer, options, waiter);
				waiter.WaitOne();
				Thread.Sleep(1000);
				if (pingCount > 0 && loops > pingCount)
					running = false;
				pingSender.Dispose();
			}
			if (!skip)
			{
				if (fileName != string.Empty)
					Log.WriteLog("CLOSELOG");
				Console.WriteLine("\nPing logger closing...");
				Console.WriteLine("Press any key to close.");
				Console.ReadKey();
			}
		}
		public static bool ProcessArgs(string[] args)
		{

			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
					case "-H":
					case "--host":
						i++;
						hostName = args[i];
						break;
					case "-f":
					case "--file":
						i++;
						fileName = args[i];
						break;
					case "-c":
					case "--count":
						try
						{
							i++;
							pingCount = Convert.ToInt32(args[i]);
						}
						catch (Exception)
						{
							Console.ForegroundColor = ConsoleColor.DarkRed;
							Console.WriteLine("Invalid count specified. Please enter a number.");
							Console.WriteLine("Exiting...");
							Console.ResetColor();
							return false;
						}
						break;
					case "-t":
					case "--threshold":
						try
						{
							i++;
							WarnThreshold = Convert.ToInt32(args[i]);
						} catch(Exception)
						{
							Console.ForegroundColor = ConsoleColor.DarkRed;
							Console.WriteLine("Invalid threshold specified. Please enter a number.");
							Console.WriteLine("Exiting...");
							Console.ResetColor();
							return false;
						}
						break;
					case "-h":
					case "--help":
						Console.WriteLine("Usage: PingLogger -H [hostname] -f [log file] -c [ping count]\n");

						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.Write("-H or --host: ");
						Console.ResetColor();
						Console.Write(" IP address or host name of the remote pc you wish to ping.\n\n");

						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.Write("-f or --file: ");
						Console.ResetColor();
						Console.Write(" File name that you wish to log the output to.\n");
						Console.Write("               You may use either a full path or a relative one.\n\n");

						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.Write("-c or --count: ");
						Console.ResetColor();
						Console.Write("Number of times you wish to ping the remote host.\n");
						Console.Write("               Leave blank or set to negative number to constantly ping.\n\n");

						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.Write("-t or --threshold: ");
						Console.ResetColor();
						Console.Write("Warning threshold for the roundtrip time in MS. \n");
						Console.Write("               Defaults to 200ms\n\n");

						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.Write("-h or --help: ");
						Console.ResetColor();
						Console.Write(" Prints this help.\n");
						return false;
				}
			}
			return true;
		}
		public static void WriteStatus(object sender, PingCompletedEventArgs e)
		{
			if(e.Cancelled)
			{
				Console.WriteLine("Ping canceled.");
				if (fileName != string.Empty)
					Log.WriteLog("Ping canceled.");
				((AutoResetEvent)e.UserState).Set();
			}
			if (e.Error != null)
			{
				Console.WriteLine("Ping failed: {0}", e.Error.ToString());
				if (fileName != string.Empty)
					Log.WriteLog("Ping failed: " + e.Error.ToString());
				((AutoResetEvent)e.UserState).Set();
			}
			var reply = e.Reply;
			switch(reply.Status)
			{
				case IPStatus.Success:
					Console.WriteLine("Ping time: {0}ms", reply.RoundtripTime);
					if (fileName != string.Empty)
						Log.WritePingLog(hostName, (int)reply.RoundtripTime, reply.Options.Ttl, WarnThreshold);
					break;
				case IPStatus.DestinationHostUnreachable:
					Console.ForegroundColor = ConsoleColor.DarkRed;
					Console.WriteLine("Destination host unreachable.");
					if (fileName != string.Empty)
						Log.WriteLog("Error: Desitination host unreachable");
					Console.ResetColor();
					break;
				case IPStatus.DestinationNetworkUnreachable:
					Console.ForegroundColor = ConsoleColor.DarkRed;
					Console.WriteLine("Destination network unreachable.");
					if (fileName != string.Empty)
						Log.WriteLog("Error: Desitination network unreachable");
					Console.ResetColor();
					break;
				case IPStatus.DestinationUnreachable:
					Console.ForegroundColor = ConsoleColor.DarkRed;
					Console.WriteLine("Destination unreachable, cause unknown.");
					if (fileName != string.Empty)
						Log.WriteLog("Error: Desitination unreachable, cause unknown.");
					Console.ResetColor();
					break;
				case IPStatus.HardwareError:
					Console.ForegroundColor = ConsoleColor.DarkRed;
					Console.WriteLine("Ping failed due to hardware.");
					if (fileName != string.Empty)
						Log.WriteLog("Error: Failed due to hardware issues.");
					Console.ResetColor();
					break;
			}
			((AutoResetEvent)e.UserState).Set();
		}
		public static bool ReadFromConfigFile()
		{
			var cfgFile = "./plopts.cfg";
			var readOk = false;
			if(File.Exists(cfgFile))
			{
				Console.WriteLine("Configuration file detected.");
				var cfg =  File.ReadAllLines(cfgFile);
				var len = cfg.Length;
				var comments = 0;
				for (int i = 0; i < cfg.Length; i++)
				{
					if (!cfg[i].StartsWith('#'))
					{
						try
						{
							var split = cfg[i].Split('=');
							switch (split[0])
							{
								case "host":
									hostName = split[1];
									break;
								case "file":
									fileName = split[1];
									break;
								case "count":
									pingCount = Convert.ToInt32(split[1]);
									break;
								case "threshold":
									WarnThreshold = Convert.ToInt32(split[1]);
									break;
							}

						}
						catch (Exception) {
							Console.ForegroundColor = ConsoleColor.Red;
							Console.WriteLine("You have an invalid line in your options file.");
							Console.WriteLine("Line {0}: {1}", i, cfg[i]);
							Console.ResetColor();
							Console.WriteLine("Configuration file will be ignored.");
						}
					} else
					{
						comments++;
					}
				}
				if (comments < len)
				{
					if (hostName == string.Empty)
					{
						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.WriteLine("Configuration file has no host data.\nSkipping.");
						Console.ResetColor();
					}
					else
					{
						readOk = true;
						Console.ForegroundColor = ConsoleColor.Green;
						Console.WriteLine("File read OK.\nRunning with the following options:");
						Console.WriteLine("Host: {0}", hostName);
						if (fileName != string.Empty)
							Console.WriteLine("File: {0}", fileName);
						if (pingCount > 0)
							Console.WriteLine("Count: {0}", pingCount);
						if (WarnThreshold != 200)
							Console.WriteLine("Warning Threshold: {0}", WarnThreshold);
						Console.ResetColor();
					}
				}
			}
			return readOk;
		}
		public static void Closing(object sender, ConsoleCancelEventArgs args)
		{
			if (fileName != string.Empty)
				Log.WriteLog("CLOSELOG");
			args.Cancel = true;
			Console.WriteLine("\nPing logger closing...");
			Environment.Exit(0);
		}
	}
}
