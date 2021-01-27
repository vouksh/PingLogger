﻿using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PingLogger.Extensions;
using PingLogger.Models;
using PingLogger.Workers;
using ReactiveUI;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Text;
using System.Diagnostics;

namespace PingLogger.ViewModels
{
	public class PingControlViewModel : ViewModelBase
	{
		public Host Host { get; set; }
		private Pinger _pinger;
		public ReactiveCommand<string, Unit> PingCommand { get; }
		public ReactiveCommand<Unit, Unit> OpenTraceRouteCommand { get; }
		public ReactiveCommand<Unit, Unit> OpenLogFolderCommand { get; }
		public ReactiveCommand<Unit, Unit> OpenHelpCommand { get; }
		readonly DispatcherTimer Timer;
		private readonly FixedList<long> PingTimes = new FixedList<long>(23);
		private long totalPings = 0;
		public delegate void HostNameUpdatedHandler(object sender, HostNameUpdatedEventArgs e);
		public event HostNameUpdatedHandler HostNameUpdated;
		public delegate void TraceRouteCallbackHandler(object sender, TraceRouteCallbackEventArgs e);
		public event TraceRouteCallbackHandler TraceRouteCallback;

		public PingControlViewModel()
		{
			PingCommand = ReactiveCommand.Create<string>(TriggerPinger);
			OpenTraceRouteCommand = ReactiveCommand.Create(OpenTraceRoute);
			OpenLogFolderCommand = ReactiveCommand.Create(OpenLogFolder);
			OpenHelpCommand = ReactiveCommand.Create(OpenHelp);

			Timer = new DispatcherTimer()
			{
				Interval = TimeSpan.FromMilliseconds(100),
				IsEnabled = true
			};
			Timer.Tick += Timer_Tick;
			Timer.Start();
		}


		private void OpenHelp()
		{
			if (OperatingSystem.IsWindows())
			{
				try
				{
					Process.Start("https://github.com/vouksh/PingLogger/blob/master/README.md");
				}
				catch
				{
					Process.Start(new ProcessStartInfo("cmd", $"/c start https://github.com/vouksh/PingLogger/blob/master/README.md")
					{
						CreateNoWindow = true
					});
				}
			}
			else if (OperatingSystem.IsLinux())
			{
				Process.Start("xdg-open", "https://github.com/vouksh/PingLogger/blob/master/README.md");
			}
			else if (OperatingSystem.IsMacOS())
			{
				Process.Start("open", "https://github.com/vouksh/PingLogger/blob/master/README.md");
			}
		}

		private void OpenLogFolder()
		{
			if (OperatingSystem.IsWindows())
			{
				Process.Start("explorer.exe", $"{Config.LogSavePath}{Host.HostName}");
			}
			else if (OperatingSystem.IsLinux())
			{
				Process.Start("xdg-open", $"{Config.LogSavePath}{Path.DirectorySeparatorChar}{Host.HostName}");
			}
			else if (OperatingSystem.IsMacOS())
			{
				Process.Start("open", $"{Config.LogSavePath}{Path.DirectorySeparatorChar}{Host.HostName}");
			}
		}

		private void OpenTraceRoute()
		{
			var traceRouteVM = new TraceRouteViewModel()
			{
				Host = Host,
				_pinger = _pinger
			};
			traceRouteVM.TraceRouteCallback += (object s, TraceRouteCallbackEventArgs e) => TraceRouteCallback?.Invoke(s, e);
			var traceRouteWindow = new Views.TraceRouteWindow()
			{
				DataContext = traceRouteVM
			};
			if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				traceRouteWindow.ShowDialog(desktop.MainWindow);
			}
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			if (_pinger is not null && _pinger.Running)
			{
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < _pinger.Replies.Count - 1; i++)
				{
					totalPings++;
					var success = _pinger.Replies.TryTake(out Reply reply);
					if (success)
					{
						Log.Debug("Ping Success");
						sb.Append($"[{reply.DateTime:T}] ");
						if (reply.RoundTrip > 0)
						{
							PingTimes.Add(reply.RoundTrip);
							Log.Debug($"{HostName} RoundTrip Time > 0: {reply.RoundTrip}");
						}

						if (reply.TimedOut)
						{
							TimeoutCount++;
							Log.Debug($"{HostName} Reply timed out. Number of Timeouts: {TimeoutCount}");
							sb.Append($"Timed out to host");
						}
						else
						{
							sb.Append($"Ping round trip: {reply.RoundTrip}ms");
							if (reply.RoundTrip >= WarningThreshold)
							{
								WarningCount++;
								sb.Append(" [Warning]");
							}
						}
						sb.Append(Environment.NewLine);
					}
				}
				PingStatusText += sb.ToString();
				var lines = PingStatusText.Split(Environment.NewLine).ToList();
				if (lines.Count > PingTimes.MaxSize)
				{
					lines.RemoveAt(0);
					PingStatusText = string.Join(Environment.NewLine, lines);
				}

				if (PingTimes.Count > 0)
				{
					AveragePing = Math.Ceiling(PingTimes.Average()).ToString() + "ms";
				}
				PacketLoss = $"{Math.Round(((double)TimeoutCount / (double)totalPings) * 100, 2)}%";
			}
			else
			{
				StopButtonEnabled = false;
				if (Host.IP == "Invalid Host Name")
				{
					StartButtonEnabled = false;
				}
				else
				{
					StartButtonEnabled = true;
				}
			}
			if (Host.IP == "CHANGEME")
			{
				UpdateIP();
			}
			CheckForFolder();
		}

		private bool DirExists = false;
		private bool LogExists = false;

		private void CheckForFolder()
		{
			if (!DirExists)
			{
				if (Directory.Exists($"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}Logs{Path.DirectorySeparatorChar}{HostName}"))
				{
					DirExists = true;
					//openLogFolderBtn.Visibility = Visibility.Visible;
				}
				else
				{
					//openLogFolderBtn.Visibility = Visibility.Hidden;
				}
			}
			if (!LogExists)
			{
				if (File.Exists($"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}Logs{Path.DirectorySeparatorChar}{HostName}{Path.DirectorySeparatorChar}{HostName}-{DateTime.Now:yyyyMMdd}.log"))
				{
					LogExists = true;
					//viewLogBtn.Visibility = Visibility.Visible;
				}
				else
				{
					//viewLogBtn.Visibility = Visibility.Hidden;
				}
			}
		}
		private void UpdateHost()
		{
			try
			{
				var index = Config.Hosts.IndexOf(Host);
				Config.Hosts[index] = Host;
			}
			catch
			{
			}
		}

		private void TriggerPinger(string start)
		{
			if (start == "true")
			{
				Log.Debug("TriggerPinger(true)");
				StartButtonEnabled = false;
				StopButtonEnabled = true;
				HostNameBoxEnabled = false;
				IntervalBoxEnabled = false;
				WarningBoxEnabled = false;
				TimeoutBoxEnabled = false;
				PacketSizeBoxEnabled = false;
				_pinger = new Pinger(Host);
				_pinger.Start();
			}
			else
			{
				Log.Debug("TriggerPinger(false)");
				StartButtonEnabled = true;
				StopButtonEnabled = false;
				HostNameBoxEnabled = true;
				IntervalBoxEnabled = true;
				WarningBoxEnabled = true;
				TimeoutBoxEnabled = true;
				PacketSizeBoxEnabled = true;
				_pinger?.Stop();
			}
		}

		public string HostName
		{
			get
			{
				return Host.HostName;
			}
			set
			{
				Host.HostName = value;
				Host.IP = "CHANGEME";
				HostNameUpdated?.Invoke(this, new HostNameUpdatedEventArgs(HostName, Host.Id.ToString()));
				UpdateHost();
			}
		}

		public string IPAddress
		{
			get
			{
				if (string.IsNullOrEmpty(Host.IP))
					UpdateIP();
				return Host.IP;
			}
		}

		public int Interval
		{
			get
			{
				return Host.Interval;
			}
			set
			{
				Host.Interval = value;
			}
		}

		public int WarningThreshold
		{
			get
			{
				return Host.Threshold;
			}
			set
			{
				Host.Threshold = value;
			}
		}

		public int Timeout
		{
			get
			{
				return Host.Timeout;
			}
			set
			{
				Host.Timeout = value;
			}
		}

		public int PacketSize
		{
			get
			{
				return Host.PacketSize;
			}
			set
			{
				Host.PacketSize = value;
			}
		}

		private bool startButtonEnabled = true;
		public bool StartButtonEnabled
		{
			get => startButtonEnabled;
			private set => this.RaiseAndSetIfChanged(ref startButtonEnabled, value);
		}

		private bool stopButtonEnabled = false;
		public bool StopButtonEnabled
		{
			get => stopButtonEnabled;
			private set => this.RaiseAndSetIfChanged(ref stopButtonEnabled, value);
		}

		private bool hostNameBoxEnabled = true;
		public bool HostNameBoxEnabled
		{
			get => hostNameBoxEnabled;
			private set => this.RaiseAndSetIfChanged(ref hostNameBoxEnabled, value);
		}

		private bool intervalBoxEnabled = true;
		public bool IntervalBoxEnabled
		{
			get => intervalBoxEnabled;
			private set => this.RaiseAndSetIfChanged(ref intervalBoxEnabled, value);
		}

		private bool warningBoxEnabled = true;
		public bool WarningBoxEnabled
		{
			get => warningBoxEnabled;
			private set => this.RaiseAndSetIfChanged(ref warningBoxEnabled, value);
		}

		private bool timeoutBoxEnabled = true;
		public bool TimeoutBoxEnabled
		{
			get => timeoutBoxEnabled;
			private set => this.RaiseAndSetIfChanged(ref timeoutBoxEnabled, value);
		}

		private bool packetSizeBoxEnabled = true;
		public bool PacketSizeBoxEnabled
		{
			get => packetSizeBoxEnabled;
			private set => this.RaiseAndSetIfChanged(ref packetSizeBoxEnabled, value);
		}

		private int warningCount = 0;
		public int WarningCount
		{
			get => warningCount;
			private set => this.RaiseAndSetIfChanged(ref warningCount, value);
		}

		private int timeoutCount = 0;
		public int TimeoutCount
		{
			get => timeoutCount;
			private set => this.RaiseAndSetIfChanged(ref timeoutCount, value);
		}

		private string packetLoss = "0%";
		public string PacketLoss
		{
			get => packetLoss;
			private set => this.RaiseAndSetIfChanged(ref packetLoss, value);
		}

		private string averagePing = "0ms";
		public string AveragePing
		{
			get => averagePing;
			private set => this.RaiseAndSetIfChanged(ref averagePing, value);
		}

		private string pingStatusText = string.Empty;
		public string PingStatusText
		{
			get => pingStatusText;
			private set => this.RaiseAndSetIfChanged(ref pingStatusText, value);
		}

		private async void UpdateIP()
		{
			try
			{
				foreach (var ip in await Dns.GetHostAddressesAsync(HostName))
				{
					if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
					{
						Host.IP = ip.ToString();
						this.RaisePropertyChanged(nameof(IPAddress));
						break;
					}
				}
			}
			catch (Exception)
			{
				Host.IP = "Invalid Host Name";
			}
		}
	}
}
