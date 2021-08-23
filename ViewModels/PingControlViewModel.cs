using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using OxyPlot;
using OxyPlot.Series;
using PingLogger.Extensions;
using PingLogger.Models;
using PingLogger.Workers;
using ReactiveUI;
using Serilog;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace PingLogger.ViewModels
{
	public class PingControlViewModel : ViewModelBase
	{
		public Host Host { get; set; }
		private Pinger _pinger;
		public ReactiveCommand<bool, Unit> PingCommand { get; }
		public ReactiveCommand<Unit, Unit> OpenTraceRouteCommand { get; }
		public ReactiveCommand<Unit, Unit> OpenLogFolderCommand { get; }
		public ReactiveCommand<Unit, Unit> WatchLogCommand { get; }
		public ReactiveCommand<Unit, Unit> WindowExpanderCommand { get; }
		readonly DispatcherTimer _timer;
		private readonly FixedList<long> _pingTimes = new(23);
		private long _totalPings = 0;
		public delegate void HostNameUpdatedHandler(object sender, HostNameUpdatedEventArgs e);
		public event HostNameUpdatedHandler HostNameUpdated;
		public delegate void TraceRouteCallbackHandler(object sender, TraceRouteCallbackEventArgs e);
		public event TraceRouteCallbackHandler TraceRouteCallback;
		public delegate void WindowExpansionHandler(object sender, bool expand);
		public event WindowExpansionHandler WindowExpandedEvent;
		readonly DispatcherTimer _updateIPTimer;
		readonly DispatcherTimer _checkValueTimer;

		public PingControlViewModel()
		{
			PingCommand = ReactiveCommand.Create<bool>(TriggerPinger);
			OpenTraceRouteCommand = ReactiveCommand.Create(OpenTraceRoute);
			OpenLogFolderCommand = ReactiveCommand.Create(OpenLogFolder);
			WatchLogCommand = ReactiveCommand.Create(WatchLog);
			WindowExpanderCommand = ReactiveCommand.Create(ToggleWindow);

			_timer = new DispatcherTimer()
			{
				Interval = TimeSpan.FromMilliseconds(100),
				IsEnabled = true
			};
			_timer.Tick += Timer_Tick;
			_timer.Start();
			_updateIPTimer = new DispatcherTimer()
			{
				Interval = TimeSpan.FromMilliseconds(250),
				IsEnabled = false
			};
			_updateIPTimer.Tick += UpdateIPTimer_Tick;
			SetupGraphs();
			showRightTabs = Config.WindowExpanded;
			expanderIcon = Config.WindowExpanded ? "fas fa-angle-double-left" : "fas fa-angle-double-right";

			_checkValueTimer = new DispatcherTimer()
			{
				Interval = TimeSpan.FromMilliseconds(500),
				IsEnabled = false
			};
			_checkValueTimer.Tick += (_, _) => CheckValues();
		}

		private async void ToggleWindow()
		{
			Config.WindowExpanded = !Config.WindowExpanded;
			ExpanderIcon = Config.WindowExpanded ? "fas fa-angle-double-left" : "fas fa-angle-double-right";
			WindowExpandedEvent?.Invoke(this, Config.WindowExpanded);
			await Task.Delay(10);
			ShowRightTabs = Config.WindowExpanded;
		}

		private void WatchLog()
		{
			var wlVM = new WatchLogViewModel()
			{
				Host = Host
			};
			var watchLogWindow = new Views.WatchLogWindow()
			{
				DataContext = wlVM
			};
			watchLogWindow.Closing += (_, _) => { wlVM.Closing(); };
			wlVM.Start();
			watchLogWindow.Show();
		}

		public void SetupGraphs()
		{
			GraphModel = new PlotModel();
			GraphModel.Axes.Add(new OxyPlot.Axes.LinearAxis
			{
				Position = OxyPlot.Axes.AxisPosition.Left
			});
			GraphModel.Axes.Add(new OxyPlot.Axes.DateTimeAxis()
			{
				Position = OxyPlot.Axes.AxisPosition.Bottom,
				StringFormat = "hh:mm:ss",
				IntervalType = OxyPlot.Axes.DateTimeIntervalType.Seconds
			});
			GraphModel.Series.Add(new LineSeries { LineStyle = LineStyle.Solid });
			GraphModel.Padding = new OxyThickness(5);

			StatusModel = new PlotModel();
			StatusModel.Series.Add(new PieSeries() { Diameter = 0.8 });
			(StatusModel.Series[0] as PieSeries).Slices.Add(new PieSlice("Success", 0) { Fill = OxyColor.FromRgb(51, 204, 0) });
			(StatusModel.Series[0] as PieSeries).Slices.Add(new PieSlice("Warning", 0) { Fill = OxyColor.FromRgb(235, 224, 19) });
			(StatusModel.Series[0] as PieSeries).Slices.Add(new PieSlice("Failure", 0) { Fill = OxyColor.FromRgb(194, 16, 16) });
			StatusModel.Padding = new OxyThickness(5);
			switch (Config.Theme)
			{
				case Theme.Dark:
					GraphModel.Background = OxyColor.Parse("#2A2A2A");
					GraphModel.LegendTextColor = OxyColor.Parse("#F0F0F0");
					graphModel.TextColor = OxyColor.Parse("#F0F0F0");
					StatusModel.Background = OxyColor.Parse("#2A2A2A");
					StatusModel.LegendTextColor = OxyColor.Parse("#F0F0F0");
					StatusModel.TextColor = OxyColor.Parse("#F0F0F0");
					break;
				case Theme.Auto:
					if (App.DarkMode)
					{
						GraphModel.Background = OxyColor.Parse("#2A2A2A");
						GraphModel.LegendTextColor = OxyColor.Parse("#F0F0F0");
						graphModel.TextColor = OxyColor.Parse("#F0F0F0");
						StatusModel.Background = OxyColor.Parse("#2A2A2A");
						StatusModel.LegendTextColor = OxyColor.Parse("#F0F0F0");
						StatusModel.TextColor = OxyColor.Parse("#F0F0F0");
					}
					break;
			}
		}

		private void UpdateIPTimer_Tick(object sender, EventArgs e)
		{
			UpdateIP();
			UpdateHost();
			_updateIPTimer.Stop();
		}

		private void OpenLogFolder()
		{
#if Windows
			Process.Start("explorer.exe", $"{Config.LogSavePath}{Host.HostName}");
#elif Linux
			Process.Start("xdg-open", $"{Config.LogSavePath}{Path.DirectorySeparatorChar}{Host.HostName}");
#elif OSX
			Process.Start("open", $"{Config.LogSavePath}{Path.DirectorySeparatorChar}{Host.HostName}");
#endif
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
				StopButtonEnabled = true;
				StartButtonEnabled = false;
				StringBuilder sb = new();
				for (int i = 0; i < _pinger.Replies.Count - 1; i++)
				{
					_totalPings++;
					var success = _pinger.Replies.TryTake(out Reply reply);
					if (success)
					{
						if (Config.WindowExpanded)
						{
							var s = (LineSeries)GraphModel.Series[0];
							var maxPoints = (Host.Interval * 30) / 1000;
							if (s.Points.Count > maxPoints)
								s.Points.RemoveAt(0);
							s.Points.Add(new DataPoint(reply.DateTime.ToOADate(), reply.RoundTrip));
							Dispatcher.UIThread.InvokeAsync(() => GraphModel.InvalidatePlot(true), DispatcherPriority.Background);

							var p = (PieSeries)StatusModel.Series[0];
							if (reply.Succeeded.Value)
							{
								if (reply.RoundTrip > Host.Threshold)
								{
									var oldValue = p.Slices[1].Value;
									p.Slices[1] = new PieSlice("Warning", oldValue + 1) { Fill = OxyColor.FromRgb(235, 224, 19) };
								}
								else
								{
									var oldValue = p.Slices[0].Value;
									p.Slices[0] = new PieSlice("Success", oldValue + 1) { Fill = OxyColor.FromRgb(51, 204, 0) };
								}
							}
							else
							{
								var oldValue = p.Slices[2].Value;
								p.Slices[2] = new PieSlice("Failure", oldValue + 1) { Fill = OxyColor.FromRgb(194, 16, 16) };
							}
							Dispatcher.UIThread.InvokeAsync(() => StatusModel.InvalidatePlot(true), DispatcherPriority.Background);
						}

						Log.Debug("Ping Success");
						sb.Append($"[{reply.DateTime:T}] ");
						if (reply.RoundTrip > 0)
						{
							_pingTimes.Add(reply.RoundTrip);
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
				if (lines.Count > _pingTimes.MaxSize)
				{
					lines.RemoveAt(0);
					PingStatusText = string.Join(Environment.NewLine, lines);
				}

				if (_pingTimes.Count > 0)
				{
					AveragePing = Math.Ceiling(_pingTimes.Average()).ToString() + "ms";
				}
				PacketLoss = $"{Math.Round(TimeoutCount / (double)_totalPings * 100, 2)}%";
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
			CheckForFolder();
			//CheckValues();
		}

		private bool DirExists = false;
		private bool LogExists = false;

		private void CheckValues()
		{
			if (Interval < 250 || Interval == 0)
			{
				Interval = 250;
			}
			else if (Interval > 6000)
			{
				Interval = 6000;
			}

			if (WarningThreshold < 1 || WarningThreshold == 0)
			{
				WarningThreshold = 1;
			}
			else if (WarningThreshold > 6000)
			{
				WarningThreshold = 6000;
			}

			if (PacketSize < 2 || PacketSize == 0)
			{
				PacketSize = 2;
			}
			else if (PacketSize > 65527)
			{
				PacketSize = 65527;
			}

			if (Timeout < 50 || Timeout == 0)
			{
				Timeout = 50;
			}
			else if (Timeout > 6000)
			{
				Timeout = 6000;
			}
			_checkValueTimer.Stop();
		}

		private void CheckForFolder()
		{
			if (!DirExists)
			{
				if (Directory.Exists($"{Config.LogSavePath}{HostName}"))
				{
					DirExists = true;
					OpenLogFolderVisible = true;
				}
				else
				{
					OpenLogFolderVisible = false;
				}
			}
			if (!LogExists)
			{
				if (File.Exists($"{Config.LogSavePath}{HostName}{Path.DirectorySeparatorChar}{HostName}-{DateTime.Now:yyyyMMdd}.log"))
				{
					LogExists = true;
					WatchLogVisible = true;
				}
				else
				{
					WatchLogVisible = false;
				}
			}
		}
		private void UpdateHost()
		{
			try
			{
				Host.HostName = hostName;
				var index = Config.Hosts.IndexOf(Host);
				Config.Hosts[index] = Host;
			}
			catch
			{
			}
		}

		public void TriggerPinger(bool start)
		{
			if (start)
			{
				CheckValues();
				Log.Debug("TriggerPinger(true)");
				StartButtonEnabled = false;
				StopButtonEnabled = true;
				HostNameBoxEnabled = false;
				IntervalBoxEnabled = false;
				WarningBoxEnabled = false;
				TimeoutBoxEnabled = false;
				PacketSizeBoxEnabled = false;
				IPEnabled = false;
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
				IPEnabled = true;
				_pinger?.Stop();
			}
		}

		private string hostName = string.Empty;
		public string HostName
		{
			get
			{
				if (string.IsNullOrWhiteSpace(hostName))
					hostName = Host.HostName;

				return hostName;
			}
			set
			{
				StartButtonEnabled = false;
				_updateIPTimer.Stop();
				_updateIPTimer.Start();
				HostNameUpdated?.Invoke(this, new HostNameUpdatedEventArgs(value, Host.Id.ToString()));
				this.RaiseAndSetIfChanged(ref hostName, value);
			}
		}

		private string ipAddress = string.Empty;
		public string IPAddress
		{
			get
			{
				if (string.IsNullOrEmpty(ipAddress))
					UpdateIP();
				return ipAddress;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref ipAddress, value);
				Host.IP = value;
			}
		}

		[Range(250, 6000, ErrorMessage = "Interval must be between 250 and 6,000 milliseconds")]
		private int interval = 0;
		public int Interval
		{
			get
			{
				if (interval == 0)
					interval = Host.Interval;
				return interval;
			}
			set
			{
				_checkValueTimer.Stop();
				_checkValueTimer.Start();
				this.RaiseAndSetIfChanged(ref interval, value);
				Host.Interval = value;
				UpdateHost();
			}
		}

		[Range(1, 6000, ErrorMessage = "Warning threshold must be between 1 and 6,000 milliseconds")]
		private int warningThreshold = 0;
		public int WarningThreshold
		{
			get
			{
				if (warningThreshold == 0)
					warningThreshold = Host.Threshold;
				return warningThreshold;
			}
			set
			{
				_checkValueTimer.Stop();
				_checkValueTimer.Start();
				this.RaiseAndSetIfChanged(ref warningThreshold, value);
				Host.Threshold = value;
				UpdateHost();
			}
		}

		[Range(1, 6000, ErrorMessage = "Timeout must be between 1 and 6,000 milliseconds")]
		private int timeout = 0;
		public int Timeout
		{
			get
			{
				if (timeout == 0)
					timeout = Host.Timeout;
				return timeout;
			}
			set
			{
				_checkValueTimer.Stop();
				_checkValueTimer.Start();
				this.RaiseAndSetIfChanged(ref timeout, value);
				Host.Timeout = value;
				UpdateHost();
			}
		}

		[Range(2, 65526, ErrorMessage = "Packet size must be between 2 and 65,527 bytes")]
		private int packetSize = 0;
		public int PacketSize
		{
			get
			{
				if (packetSize == 0)
					packetSize = Host.PacketSize;
				return packetSize;
			}
			set
			{
				_checkValueTimer.Stop();
				_checkValueTimer.Start();
				this.RaiseAndSetIfChanged(ref packetSize, value);
				Host.PacketSize = value;
				UpdateHost();
			}
		}

		private bool ipEnabled = true;
		public bool IPEnabled
		{
			get => ipEnabled;
			set => this.RaiseAndSetIfChanged(ref ipEnabled, value);
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

		private bool openLogFolderVisible = false;
		public bool OpenLogFolderVisible
		{
			get => openLogFolderVisible;
			set => this.RaiseAndSetIfChanged(ref openLogFolderVisible, value);
		}

		private bool watchLogVisible = false;
		public bool WatchLogVisible
		{
			get => watchLogVisible;
			set => this.RaiseAndSetIfChanged(ref watchLogVisible, value);
		}

		private PlotModel graphModel;
		public PlotModel GraphModel
		{
			get => graphModel;
			set => this.RaiseAndSetIfChanged(ref graphModel, value);
		}

		private PlotModel statusModel;
		public PlotModel StatusModel
		{
			get => statusModel;
			set => this.RaiseAndSetIfChanged(ref statusModel, value);
		}

		private bool showRightTabs = true;
		public bool ShowRightTabs
		{
			get => showRightTabs;
			set => this.RaiseAndSetIfChanged(ref showRightTabs, value);
		}

		private string expanderIcon = "fas fa-angle-double-left";
		public string ExpanderIcon
		{
			get => expanderIcon;
			set => this.RaiseAndSetIfChanged(ref expanderIcon, value);
		}

		public static bool TraceRouteEnabled
		{
			get => OperatingSystem.IsWindows();
		}


		private async void UpdateIP()
		{
			try
			{
				foreach (var ip in await Dns.GetHostAddressesAsync(HostName))
				{
					if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
					{
						IPAddress = ip.ToString();
						this.RaisePropertyChanged(nameof(IPAddress));
						return;
					}
				}
				if (System.Net.IPAddress.TryParse(HostName, out _))
				{
					IPAddress = HostName;
					this.RaisePropertyChanged(nameof(IPAddress));
				}
			}
			catch (System.Net.Sockets.SocketException)
			{
				IPAddress = "Invalid Host Name";
			}
		}
	}
}
