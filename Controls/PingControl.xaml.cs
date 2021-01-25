using FontAwesome.WPF;
using PingLogger.Extensions;
using PingLogger.Models;
using PingLogger.Workers;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PingLogger.Controls
{
	/// <summary>
	/// Interaction logic for PingControl.xaml
	/// </summary>
	public partial class PingControl : UserControl
	{
		public Host PingHost;
		private Pinger _pinger;
		private readonly FixedList<long> _pingTimes = new(23);
		private int _timeouts;
		private int _warnings;
		private readonly SynchronizationContext _syncCtx;
		private readonly bool _loadFromVar;
		private double _packetLoss;
		private long _totalPings;
		public ICommand CloseTabCommand { get; set; }
		public PingControl()
		{
			InitializeComponent();
			var timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(100)
			};
			timer.Tick += Timer_Tick;
			PingHost = new Host();
			_syncCtx = SynchronizationContext.Current;
			timer.Start();
			StatusGraphControl.StylePlot(true);
			PingGraphControl.StylePlot();
		}

		public PingControl(Host host, bool runTimeAdded = false)
		{
			InitializeComponent();
			var timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(100)
			};
			timer.Tick += Timer_Tick;
			PingHost = host;
			_loadFromVar = true;
			_syncCtx = SynchronizationContext.Current;
			timer.Start();
			if (!runTimeAdded)
				AutoStart();
			StatusGraphControl.StylePlot(true);
			PingGraphControl.StylePlot();

		}

		private void AutoStart()
		{
			if (Config.StartLoggersAutomatically)
			{
				StopBtn.IsEnabled = true;
				StartBtn.IsEnabled = false;
				HostNameBox.IsEnabled = false;
				IntervalBox.IsEnabled = false;
				WarningBox.IsEnabled = false;
				TimeoutBox.IsEnabled = false;
				PacketSizeBox.IsEnabled = false;
				_pinger = new Pinger(PingHost);
				_pinger.Start();
			}
		}
		public void DoStart()
		{
			StopBtn.IsEnabled = true;
			StartBtn.IsEnabled = false;
			HostNameBox.IsEnabled = false;
			IntervalBox.IsEnabled = false;
			WarningBox.IsEnabled = false;
			TimeoutBox.IsEnabled = false;
			PacketSizeBox.IsEnabled = false;
			_pinger = new Pinger(PingHost);
			_pinger.Start();
		}

		private void Timer_Tick(object sender, EventArgs e)
		{

			if (_pinger != null && _pinger.Running)
			{
				if (Config.WindowExpanded)
				{
					switch (RightTabs.SelectedIndex)
					{
						case 1:
							PingGraphControl.UpdatePlot();
							break;
						case 2:
							StatusGraphControl.UpdatePieChart(PingHost.Threshold, PingHost.Timeout);
							break;
					}
				}
				StartBtn.Visibility = Visibility.Hidden;
				StopBtn.Visibility = Visibility.Visible;
				DoTraceRteBtn.Visibility = Visibility.Hidden;
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < _pinger.Replies.Count - 1; i++)
				{
					_totalPings++;
					// Logger.Info($"{PingHost.HostName} TotalPings: {TotalPings}");
					var success = _pinger.Replies.TryTake(out Reply reply);

					if (reply is not null)
					{
						PingGraphControl.AddData(reply.DateTime, reply.RoundTrip);
						StatusGraphControl.AddData(reply.DateTime, reply.RoundTrip);

						if (success)
						{
							Logger.Debug("Ping Success");
							var line = $"[{reply.DateTime:T}] ";

							if (reply.RoundTrip > 0)
							{
								_pingTimes.Add(reply.RoundTrip);
								Logger.Debug($"{PingHost.HostName} RoundTrip Time > 0: {reply.RoundTrip}");
							}

							if (reply.TimedOut)
							{
								_timeouts++;
								Logger.Debug($"{PingHost.HostName} Reply timed out. Number of Timeouts: {_timeouts}");
								line += "Timed out to host";
							}
							else
							{
								line += $"Ping round trip: {reply.RoundTrip}ms";

								if (reply.RoundTrip >= PingHost.Threshold)
								{
									_warnings++;
									line += " [Warning]";
								}
							}

							sb.Append(line);
							sb.Append(Environment.NewLine);
						}
					}
				}
				PingStatusBox.Text += sb.ToString();
				var lines = PingStatusBox.Text.Split(Environment.NewLine).ToList();
				if (lines.Count > _pingTimes.MaxSize)
				{
					Logger.Debug($"{PingHost.HostName} Lines in text box greater than {_pingTimes.MaxSize}, removing a line.");
					lines.RemoveAt(0);
					PingStatusBox.Text = string.Join(Environment.NewLine, lines);
				}

				if (_pingTimes.Count > 0)
				{
					AvgPingLbl.Content = Math.Ceiling(_pingTimes.Average()).ToString(CultureInfo.CurrentCulture) + "ms";
				}
				TimeoutLbl.Content = _timeouts.ToString();
				WarningLbl.Content = _warnings.ToString();
				_packetLoss = ((double)_timeouts / _totalPings) * 100;
				PacketLossLabel.Content = $"{Math.Round(_packetLoss, 2)}%";
			}
			else
			{
				StartBtn.Visibility = Visibility.Visible;
				StopBtn.Visibility = Visibility.Hidden;
				DoTraceRteBtn.Visibility = Visibility.Visible;
				if (IpAddressBox.Text == "Invalid Host Name")
				{
					StartBtn.IsEnabled = false;
					DoTraceRteBtn.IsEnabled = false;
				}
				else
				{
					StartBtn.IsEnabled = true;
					DoTraceRteBtn.IsEnabled = true;
				}
			}
			CheckForFolder();
		}

		private bool _dirExists;
		private bool _logExists;

		private void CheckForFolder()
		{
			if (!_dirExists)
			{
				if (Directory.Exists($"{Config.LogSavePath}{HostNameBox.Text}"))
				{
					_dirExists = true;
					OpenLogFolderBtn.Visibility = Visibility.Visible;
				}
				else
				{
					OpenLogFolderBtn.Visibility = Visibility.Hidden;
				}
			}
			if (!_logExists)
			{
				if (File.Exists($"{Config.LogSavePath}{HostNameBox.Text}{Path.DirectorySeparatorChar}{HostNameBox.Text}-{DateTime.Now:yyyyMMdd}.log"))
				{
					_logExists = true;
					ViewLogBtn.Visibility = Visibility.Visible;
				}
				else
				{
					ViewLogBtn.Visibility = Visibility.Hidden;
				}
			}
		}

		private void StartBtn_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Logger.Debug("StartBtn_Click");
				StopBtn.IsEnabled = true;
				StartBtn.IsEnabled = false;
				HostNameBox.IsEnabled = false;
				IntervalBox.IsEnabled = false;
				WarningBox.IsEnabled = false;
				TimeoutBox.IsEnabled = false;
				PacketSizeBox.IsEnabled = false;
				_pinger = new Pinger(PingHost);
				Logger.Debug("Pinger instance created.");
				_pinger.Start();
				Logger.Debug("Pinger started.");
			}
			catch (Exception ex)
			{
				Logger.Debug(ex.ToString());
			}
		}

		public void DoStop()
		{
			StopBtn.IsEnabled = false;
			StartBtn.IsEnabled = true;
			HostNameBox.IsEnabled = true;
			IntervalBox.IsEnabled = true;
			WarningBox.IsEnabled = true;
			TimeoutBox.IsEnabled = true;
			PacketSizeBox.IsEnabled = true;
			if (_pinger != null)
			{
				_pinger.Stop();
				_pinger = null;
			}
		}

		private void StopBtn_Click(object sender, RoutedEventArgs e)
		{
			StopBtn.IsEnabled = false;
			StartBtn.IsEnabled = true;
			HostNameBox.IsEnabled = true;
			IntervalBox.IsEnabled = true;
			WarningBox.IsEnabled = true;
			TimeoutBox.IsEnabled = true;
			PacketSizeBox.IsEnabled = true;
			_pinger.Stop();
		}
		private void UpdateHost()
		{
			var index = Config.Hosts.IndexOf(PingHost);
			PingHost.HostName = HostNameBox.Text;
			PingHost.IP = IpAddressBox.Text;
			PingHost.Interval = Convert.ToInt32(IntervalBox.Text);
			PingHost.PacketSize = Convert.ToInt32(PacketSizeBox.Text);
			PingHost.Threshold = Convert.ToInt32(WarningBox.Text);
			PingHost.Timeout = Convert.ToInt32(TimeoutBox.Text);
			Config.Hosts[index] = PingHost;
		}
		private async void HostNameBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (PingHost.HostName != HostNameBox.Text)
			{
				StartBtn.IsEnabled = false;

				await UpdateIPBox();
				((TabItem) Parent).Header = $"Host: {HostNameBox.Text}";
				UpdateHost();
			}

			ViewLogBtn.Visibility = !Directory.Exists($"{Config.LogSavePath}{HostNameBox.Text}") ? Visibility.Hidden : Visibility.Visible;
		}

		private async void MainControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (_loadFromVar)
			{
				HostNameBox.Text = PingHost.HostName;
				IntervalBox.Text = PingHost.Interval.ToString();
				WarningBox.Text = PingHost.Threshold.ToString();
				TimeoutBox.Text = PingHost.Timeout.ToString();
				PacketSizeBox.Text = PingHost.PacketSize.ToString();
			}
			ToggleSideVisibility();
			await UpdateIPBox();
		}

		private async Task UpdateIPBox()
		{
			try
			{
				foreach (var ip in await Dns.GetHostAddressesAsync(HostNameBox.Text))
				{
					if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
					{
						_syncCtx.Post(o =>
									{
										IpAddressBox.Text = ((string)o)!;
									},
						ip.ToString()
						);
						PingHost.IP = ip.ToString();
						PingHost.HostName = HostNameBox.Text;
						break;
					}
				}
			}
			catch (Exception)
			{
				IpAddressBox.Text = "Invalid Host Name";
				DoTraceRteBtn.IsEnabled = false;
			}
		}

		private static readonly Regex _regex = new("[^0-9.-]+");
		private static bool IsNumericInput(string text)
		{
			return !_regex.IsMatch(text);
		}

		private void IntervalBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !IsNumericInput(e.Text);
		}

		private void WarningBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !IsNumericInput(e.Text);
		}

		private void TimeoutBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !IsNumericInput(e.Text);
		}

		private void PacketSizeBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !IsNumericInput(e.Text);
		}

		private void IntervalBox_LostFocus(object sender, RoutedEventArgs e)
		{
			var interval = Convert.ToInt32(IntervalBox.Text);
			if (interval < 250)
			{
				IntervalBox.Text = "250";
				MessageBox.Show("Interval can not be less than 250ms!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			UpdateHost();
		}

		private void PacketSizeBox_LostFocus(object sender, RoutedEventArgs e)
		{
			var packetSize = Convert.ToInt32(PacketSizeBox.Text);
			if (packetSize > 65500)
			{
				PacketSizeBox.Text = "65500";
				MessageBox.Show("Packet size can not be larger than 65,500 bytes!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			if (packetSize == 0)
			{
				PacketSizeBox.Text = "1";
				MessageBox.Show("Packet size can not be smaller than 1 byte!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			UpdateHost();
		}

		private void HostNameBox_GotFocus(object sender, RoutedEventArgs e)
		{
			HostNameBox.SelectAll();
		}

		private void IntervalBox_GotFocus(object sender, RoutedEventArgs e)
		{
			IntervalBox.SelectAll();
		}

		private void WarningBox_GotFocus(object sender, RoutedEventArgs e)
		{
			WarningBox.SelectAll();
		}

		private void TimeoutBox_GotFocus(object sender, RoutedEventArgs e)
		{
			TimeoutBox.SelectAll();
		}

		private void PacketSizeBox_GotFocus(object sender, RoutedEventArgs e)
		{
			PacketSizeBox.SelectAll();
		}

		private void HostNameBox_MouseUp(object sender, MouseButtonEventArgs e)
		{
			HostNameBox.SelectAll();
		}

		private void WarningBox_LostFocus(object sender, RoutedEventArgs e)
		{
			UpdateHost();
		}

		private void TimeoutBox_LostFocus(object sender, RoutedEventArgs e)
		{
			UpdateHost();
		}

		private void HelpBtn_Click(object sender, RoutedEventArgs e)
		{
			var helpDlg = new HelpDialog();
			helpDlg.ShowDialog();
		}

		private void OpenLogFolderBtn_Click(object sender, RoutedEventArgs e)
		{
			_ = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "explorer.exe",
					Arguments = $"{Config.LogSavePath}{HostNameBox.Text}"
				}
			}.Start();
		}

		private void ViewLogBtn_Click(object sender, RoutedEventArgs e) => new LogViewerDialog(this.PingHost).Show();

		private void HostNameBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			if (e.Text != PingHost.HostName)
			{
				ViewLogBtn.Visibility = Visibility.Hidden;
			}

			StartBtn.IsEnabled = false;
		}

		private void DoTraceRteBtn_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_pinger ??= new Pinger(PingHost);
				var showTraceRteWindow = new TraceRouteControl(ref _pinger)
				{
					Owner = Window.GetWindow(this)
				};
				showTraceRteWindow.ShowDialog();
			}
			catch (System.Net.Sockets.SocketException)
			{
				MessageBox.Show("Invalid host.");
			}
		}

		private void ResetCountersBtn_Click(object sender, RoutedEventArgs e)
		{
			_timeouts = 0;
			_packetLoss = 0.0;
			_totalPings = 0;
			_warnings = 0;
			_pingTimes.Clear();
			PingGraphControl.ClearData();
			StatusGraphControl.ClearData();
			PingStatusBox.Text = string.Empty;
		}

		private void PingWindowToggle_Click(object sender, RoutedEventArgs e)
		{
			Config.WindowExpanded = !Config.WindowExpanded;
			(Window.GetWindow(this) as MainWindow)?.ToggleWindowSize();
		}

		public void ToggleSideVisibility()
		{
			// Funny enough, this is never called directly in this class.
			// It just gets hit from the MainWindow calling it.
			if (Config.WindowExpanded)
			{
				RightTabs.Visibility = Visibility.Visible;
				PingWindowToggle.Content = new ImageAwesome
				{
					Icon = FontAwesomeIcon.AngleDoubleLeft,
					Foreground = Util.IsLightTheme ? Brushes.Black : Brushes.White,
					Width = 14,
					Height = 14
				};

			}
			else
			{
				RightTabs.Visibility = Visibility.Collapsed;
				PingWindowToggle.Content = new ImageAwesome
				{
					Icon = FontAwesomeIcon.AngleDoubleRight,
					Foreground = Util.IsLightTheme ? Brushes.Black : Brushes.White,
					Width = 14,
					Height = 14
				};
			}
		}
	}
}
