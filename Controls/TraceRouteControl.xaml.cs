using PingLogger.GUI.Models;
using PingLogger.GUI.Workers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

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
		private readonly SynchronizationContext syncCtx;

		public TraceRouteControl()
		{
			InitializeComponent();
			CloseWindowCommand = new Command(Close);
			syncCtx = SynchronizationContext.Current;
		}

		public TraceRouteControl(ref Pinger _pinger)
		{
			InitializeComponent();
			CloseWindowCommand = new Command(Close);
			pinger = _pinger;
			host = _pinger.UpdateHost();
			hostNameLabel.Content = host.HostName;
			traceView.ItemsSource = TraceReplies;
			syncCtx = SynchronizationContext.Current;
		}

		private void TraceReplies_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			traceView.Items.Refresh();
		}

		private void RefreshTimer_Tick(object sender, EventArgs e)
		{
			traceView.Items.Refresh();
		}

		private async void startTraceRteBtn_Click(object sender, RoutedEventArgs e)
		{
			hostsLookedUp = 0;
			traceView.ItemsSource = null;
			traceView.IsReadOnly = true;
			fakeProgressBar.Visibility = Visibility.Visible;
			startTraceRteBtn.Visibility = Visibility.Hidden;
			var currentPing = $"Current Ping: {(await pinger.GetSingleRoundTrip(IPAddress.Parse(host.IP), 64)).Item1}ms";
			Logger.Info(currentPing);
			pingTimeLabel.Content = currentPing;
			TraceReplies.Clear();
			traceView.ItemsSource = TraceReplies;
			await RunTraceRoute();

			Task allLookedUp = Task.WhenAll(HostNameLookupTasks);
			try
			{
				allLookedUp.Wait();
			}
			catch { }
			while (TraceReplies.Count != hostsLookedUp)
			{
				await Task.Delay(50);
			}
			fakeProgressBar.Visibility = Visibility.Hidden;
			startTraceRteBtn.Visibility = Visibility.Visible;
			traceView.IsReadOnly = false;
		}

		private void CreateHostFromTrace(object sender, RoutedEventArgs e)
		{
			var hostName = (sender as Button).Uid;
			if (hostName != "N/A")
			{
				(this.Owner as MainWindow).AddTab(hostName);
			}
		}

		List<Task> HostNameLookupTasks = new List<Task>();
		int hostsLookedUp = 0;

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
					var newID = Guid.NewGuid();
					TraceReplies.Add(new TraceReply
					{
						ID = newID,
						IPAddress = reply.Address.ToString(),
						PingTimes = new string[3],
						Ttl = ttl,
						HostName = "Looking up host...",
						IPAddButtonVisible = Visibility.Hidden,
						HostAddButtonVisible = Visibility.Hidden
					});
					traceView.ScrollIntoView(TraceReplies.Last());
					HostNameLookupTasks.Add(Task.Run(() =>
					{
						Dns.GetHostEntryAsync(reply.Address).ContinueWith(hostEntryTask =>
						{
							Logger.Debug($"Starting lookup for address {reply.Address} with ID {newID}");
							try
							{
								TraceReplies.First(t => t.ID == newID).HostName = hostEntryTask.Result.HostName;
							}
							catch(Exception ex)
							{
								Logger.Debug($"Unable to find host entry for IP {reply.Address}");
								Logger.Log.Debug(ex, $"Exception data for {reply.Address} with Ttl {ttl} and ID of {newID}");
								TraceReplies.First(t => t.ID == newID).HostName = "N/A"; 
							}
							Interlocked.Increment(ref hostsLookedUp);
							Logger.Debug($"hostsLookedUp: {hostsLookedUp}");
							syncCtx.Post(new SendOrPostCallback(o =>
							{
								traceView.Items.Refresh();
							}), null);
						});
					}));


					var firstTry = await pinger.GetSingleRoundTrip(reply.Address, ttl);
					TraceReplies.First(t => t.ID == newID).PingTimes[0] = firstTry.Item2 != IPStatus.Success ? firstTry.Item2.ToString() : firstTry.Item1.ToString() + "ms";
					traceView.Items.Refresh();

					if (firstTry.Item2 == IPStatus.Success)
						await Task.Delay(250);

					var secondTry = await pinger.GetSingleRoundTrip(reply.Address, ttl);
					TraceReplies.First(t => t.ID == newID).PingTimes[1] = secondTry.Item2 != IPStatus.Success ? secondTry.Item2.ToString() : secondTry.Item1.ToString() + "ms";
					traceView.Items.Refresh();
					if (secondTry.Item2 == IPStatus.Success)
						await Task.Delay(250);

					var thirdTry = await pinger.GetSingleRoundTrip(reply.Address, ttl);
					TraceReplies.First(t => t.ID == newID).PingTimes[2] = thirdTry.Item2 != IPStatus.Success ? secondTry.Item2.ToString() : thirdTry.Item1.ToString() + "ms";
					traceView.Items.Refresh();

					if ((firstTry.Item2 == IPStatus.TimedOut || firstTry.Item2 == IPStatus.TtlExpired) && (secondTry.Item2 == IPStatus.TimedOut || secondTry.Item2 == IPStatus.TtlExpired) && (thirdTry.Item2 == IPStatus.TimedOut || thirdTry.Item2 == IPStatus.TtlExpired))
					{
						TraceReplies.First(t => t.ID == newID).HostAddButtonVisible = Visibility.Hidden;
						TraceReplies.First(t => t.ID == newID).IPAddButtonVisible = Visibility.Hidden;
					}
					else
					{
						if (TraceReplies.First(t => t.ID == newID).HostName == "N/A")
						{
							TraceReplies.First(t => t.ID == newID).IPAddButtonVisible = Visibility.Visible;
						}
						else
						{
							TraceReplies.First(t => t.ID == newID).HostAddButtonVisible = Visibility.Visible;
						}
					}
					if (thirdTry.Item2 == IPStatus.Success)
						await Task.Delay(250);
				}
				if (reply.Status != IPStatus.TtlExpired && reply.Status != IPStatus.TimedOut)
				{
					break;
				}
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			traceView.ItemsSource = null;
			TraceReplies.Clear();
		}
	}
}
