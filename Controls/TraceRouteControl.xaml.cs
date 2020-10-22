using PingLogger.Models;
using PingLogger.Workers;
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

namespace PingLogger.Controls
{
	/// <summary>
	/// Interaction logic for TraceRouteControl.xaml
	/// </summary>
	public partial class TraceRouteControl : Window
	{
		private readonly Pinger pinger;
		private readonly Host host;
		public ICommand CloseWindowCommand { get; set; }
		public ObservableCollection<TraceReply> TraceReplies = new ObservableCollection<TraceReply>();
		private readonly SynchronizationContext syncCtx;
		private readonly CancellationTokenSource cancelToken = new CancellationTokenSource();

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

		private async void StartTraceRteBtn_Click(object sender, RoutedEventArgs e)
		{
			var stopWatch = new System.Diagnostics.Stopwatch();
			stopWatch.Start();
			hostsLookedUp = 0;
			traceView.ItemsSource = null;
			traceView.IsReadOnly = true;
			fakeProgressBar.Visibility = Visibility.Visible;
			startTraceRteBtn.Visibility = Visibility.Hidden;
			var currentPing = $"Current Ping: {(await pinger.GetSingleRoundTrip(IPAddress.Parse(host.IP), 64)).RoundTrip}ms";
			Logger.Info(currentPing);
			pingTimeLabel.Content = currentPing;
			TraceReplies.Clear();
			traceView.ItemsSource = TraceReplies;
			try
			{
				await RunTraceRoute();
			}
			catch (Exception)
			{
				Logger.Error("RunTraceRoute Cancelled");
				return;
			}
			Task allLookedUp = Task.WhenAll(HostNameLookupTasks);
			try
			{
				allLookedUp.Wait();
			}
			catch { }
			while (TraceReplies.Count != hostsLookedUp)
			{
				if (stopWatch.Elapsed == TimeSpan.FromMinutes(5))
				{
					break;
				}
				try
				{
					await Task.Delay(50, cancelToken.Token); // Keep waiting for the all of the hosts to get looked up. Use Task.Delay to prevent UI lockups. 
				}
				catch
				{
					break;
				}
			}
			fakeProgressBar.Visibility = Visibility.Hidden;
			CheckButtons();
			startTraceRteBtn.Visibility = Visibility.Visible;
			traceView.IsReadOnly = false;
			stopWatch.Stop();
			Logger.Info($"Total trace route time {stopWatch.ElapsedMilliseconds}ms");
		}

		private void CreateHostFromTrace(object sender, RoutedEventArgs e)
		{
			var hostName = (sender as Button).Uid;
			if (hostName != "N/A")
			{
				(this.Owner as MainWindow).AddTab(hostName);
			}
		}

		readonly List<Task> HostNameLookupTasks = new List<Task>();
		int hostsLookedUp = 0;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0042:Deconstruct variable declaration", Justification = "<Pending>")]
		private async Task RunTraceRoute()
		{
			var result = new List<TraceReply>();
			string data = Util.RandomString(host.PacketSize);
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
							catch (Exception ex)
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
						}, cancelToken.Token);
					}, cancelToken.Token));


					var firstTry = await pinger.GetSingleRoundTrip(reply.Address, ttl + 1);
					TraceReplies.First(t => t.ID == newID).PingTimes[0] = firstTry.Status != IPStatus.Success ? firstTry.Status.ToString() : firstTry.RoundTrip.ToString() + "ms";
					traceView.Items.Refresh();

					if (firstTry.Status == IPStatus.Success)
						await Task.Delay(250, cancelToken.Token);

					var secondTry = await pinger.GetSingleRoundTrip(reply.Address, ttl + 1);
					TraceReplies.First(t => t.ID == newID).PingTimes[1] = secondTry.Status != IPStatus.Success ? secondTry.Status.ToString() : secondTry.RoundTrip.ToString() + "ms";
					traceView.Items.Refresh();
					if (secondTry.Status == IPStatus.Success)
						await Task.Delay(250, cancelToken.Token);

					var thirdTry = await pinger.GetSingleRoundTrip(reply.Address, ttl + 1);
					TraceReplies.First(t => t.ID == newID).PingTimes[2] = thirdTry.Status != IPStatus.Success ? thirdTry.Status.ToString() : thirdTry.RoundTrip.ToString() + "ms";
					traceView.Items.Refresh();

					if (thirdTry.Status == IPStatus.Success)
						await Task.Delay(250, cancelToken.Token);
				}
				if (reply.Status != IPStatus.TtlExpired && reply.Status != IPStatus.TimedOut)
				{
					break;
				}
			}
		}

		private void CheckButtons()
		{
			foreach (var reply in TraceReplies)
			{
				// Check all 3 ping times to see if they were valid or not. We can just discard the result, we don't care what it is.
				// Future TODO? Make it an array, then parse it. If all are false, then we can just set the buttons to hidden and be done. 
				bool firstReply = int.TryParse(reply.PingTimes[0].Replace("ms", ""), out _);
				bool secondReply = int.TryParse(reply.PingTimes[1].Replace("ms", ""), out _);
				bool thirdReply = int.TryParse(reply.PingTimes[2].Replace("ms", ""), out _);

				// if the host name is N/A, then it's not a valid host.
				bool isValidHost = reply.HostName != "N/A";

				if (firstReply || secondReply || thirdReply)
				{
					// At least one of the replies was good. So we can make one of the buttons visible.
					if (isValidHost)
					{
						reply.HostAddButtonVisible = Visibility.Visible;
						reply.IPAddButtonVisible = Visibility.Hidden;
					}
					else
					{
						reply.HostAddButtonVisible = Visibility.Hidden;
						reply.IPAddButtonVisible = Visibility.Visible;
					}
				}
				else
				{
					// None of the replies were good. Lets hide both buttons.
					reply.HostAddButtonVisible = Visibility.Hidden;
					reply.IPAddButtonVisible = Visibility.Hidden;
				}
				traceView.Items.Refresh();
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			cancelToken.Cancel();
			traceView.ItemsSource = null;
			TraceReplies.Clear();
		}
	}
}
