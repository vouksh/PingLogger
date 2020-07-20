using PingLogger.GUI.Models;
using PingLogger.GUI.Workers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PingLogger.GUI.Controls
{
	/// <summary>
	/// Interaction logic for TraceRouteControl.xaml
	/// </summary>
	public partial class TraceRouteControl : Window
	{
		private Pinger pinger;
		private Host host;
		public ICommand CloseWindowCommand { get; set; }
		public ObservableCollection<TraceReply> TraceReplies = new ObservableCollection<TraceReply>();

		public TraceRouteControl()
		{
			InitializeComponent();
			CloseWindowCommand = new Command(Close);
		}

		public TraceRouteControl(ref Pinger _pinger)
		{
			InitializeComponent();
			CloseWindowCommand = new Command(Close);
			pinger = _pinger;
			host = _pinger.UpdateHost();
			hostNameLabel.Content = host.HostName;
			traceView.ItemsSource = TraceReplies;
		}

		private async void startTraceRteBtn_Click(object sender, RoutedEventArgs e)
		{
			traceView.ItemsSource = null;
			fakeProgressBar.Visibility = Visibility.Visible;
			startTraceRteBtn.Visibility = Visibility.Hidden;
			pingTimeLabel.Content = "Current Ping: " + await pinger.GetSingleRoundTrip(IPAddress.Parse(host.IP), 64) + "ms";
			TraceReplies.Clear();
			traceView.ItemsSource = TraceReplies;
			await RunTraceRoute();
			startTraceRteBtn.Visibility = Visibility.Visible;
			fakeProgressBar.Visibility = Visibility.Hidden;
		}

		private async Task RunTraceRoute()
		{
			var result = new List<TraceReply>();
			string data = Pinger.RandomString(host.PacketSize);
			Logger.Debug($"Data string: {data}");

			byte[] buffer = Encoding.ASCII.GetBytes(data);
			using var ping = new Ping();
			for (int ttl = 1; ttl <= 128; ttl++)
			{
				var pingOpts = new PingOptions(ttl, true);
				var reply = await ping.SendPingAsync(host.HostName, host.Timeout, buffer, pingOpts);
				if (reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired)
				{
					TraceReplies.Add(new TraceReply
					{
						IPAddress = reply.Address.ToString(),
						PingTimes = new string[3],
						Ttl = ttl
					});
					traceView.ScrollIntoView(TraceReplies.Last());
					var firstTry = await pinger.GetSingleRoundTrip(reply.Address, ttl);
					TraceReplies.First(t => t.Ttl == ttl).PingTimes[0] = firstTry == 0 ? "Timeout" : firstTry.ToString();
					traceView.Items.Refresh();
					await Task.Delay(250);

					var secondTry = await pinger.GetSingleRoundTrip(reply.Address, ttl);
					TraceReplies.First(t => t.Ttl == ttl).PingTimes[1] = secondTry == 0 ? "Timeout" : secondTry.ToString();
					traceView.Items.Refresh();
					await Task.Delay(250);

					var thirdTry = await pinger.GetSingleRoundTrip(reply.Address, ttl);
					TraceReplies.First(t => t.Ttl == ttl).PingTimes[2] = thirdTry == 0 ? "Timeout" : thirdTry.ToString();
					traceView.Items.Refresh();
				}
				if (reply.Status != IPStatus.TtlExpired && reply.Status != IPStatus.TimedOut)
				{
					break;
				}
			}
		}
	}
}
