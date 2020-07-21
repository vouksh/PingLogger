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
			var currentPing = $"Current Ping: {(await pinger.GetSingleRoundTrip(IPAddress.Parse(host.IP), 64)).Item1}ms";
			Logger.Info(currentPing);
			pingTimeLabel.Content = currentPing;
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
					TraceReplies.First(t => t.Ttl == ttl).PingTimes[0] = firstTry.Item2 != IPStatus.Success ? firstTry.Item2.ToString() : firstTry.Item1.ToString();
					traceView.Items.Refresh();
					await Task.Delay(250);

					var secondTry = await pinger.GetSingleRoundTrip(reply.Address, ttl);
					TraceReplies.First(t => t.Ttl == ttl).PingTimes[1] = secondTry.Item2 != IPStatus.Success ? secondTry.Item2.ToString() : secondTry.Item1.ToString();
					traceView.Items.Refresh();
					await Task.Delay(250);

					var thirdTry = await pinger.GetSingleRoundTrip(reply.Address, ttl);
					TraceReplies.First(t => t.Ttl == ttl).PingTimes[2] = thirdTry.Item2 != IPStatus.Success ? secondTry.Item2.ToString() : thirdTry.Item1.ToString();
					traceView.Items.Refresh();
					await Task.Delay(250);
				}
				if (reply.Status != IPStatus.TtlExpired && reply.Status != IPStatus.TimedOut)
				{
					break;
				}
			}
		}
	}
}
