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
		private readonly Pinger _pinger;
		private readonly Host _host;
		public ICommand CloseWindowCommand { get; set; }
		public ObservableCollection<TraceReply> TraceReplies = new();
		private readonly SynchronizationContext _syncCtx;
		private readonly CancellationTokenSource _cancelToken = new();

		public TraceRouteControl()
		{
			InitializeComponent();
			CloseWindowCommand = new Command(Close);
			_syncCtx = SynchronizationContext.Current;
		}

		public TraceRouteControl(ref Pinger pinger)
		{
			InitializeComponent();
			CloseWindowCommand = new Command(Close);
			this._pinger = pinger;
			_host = pinger.UpdateHost();
			HostNameLabel.Content = _host.HostName;
			TraceView.ItemsSource = TraceReplies;
			_syncCtx = SynchronizationContext.Current;
		}

		private async void StartTraceRteBtn_Click(object sender, RoutedEventArgs e)
		{
			var stopWatch = new System.Diagnostics.Stopwatch();
			stopWatch.Start();
			_hostsLookedUp = 0;
			TraceView.ItemsSource = null;
			TraceView.IsReadOnly = true;
			FakeProgressBar.Visibility = Visibility.Visible;
			StartTraceRteBtn.Visibility = Visibility.Hidden;
			var currentPing = $"Current Ping: {(await _pinger.GetSingleRoundTrip(IPAddress.Parse(_host.IP), 64)).RoundTrip}ms";
			Logger.Info(currentPing);
			PingTimeLabel.Content = currentPing;
			TraceReplies.Clear();
			TraceView.ItemsSource = TraceReplies;
			try
			{
				await RunTraceRoute();
			}
			catch (Exception)
			{
				Logger.Error("RunTraceRoute Cancelled");
				return;
			}
			Task allLookedUp = Task.WhenAll(_hostNameLookupTasks);

			try
			{
				allLookedUp.Wait();
			}
			catch (Exception ex)
			{
				Logger.Debug(ex.Message);
			}
			while (TraceReplies.Count != _hostsLookedUp)
			{
				if (stopWatch.Elapsed == TimeSpan.FromMinutes(5))
				{
					break;
				}
				try
				{
					await Task.Delay(50, _cancelToken.Token); // Keep waiting for the all of the hosts to get looked up. Use Task.Delay to prevent UI lockups. 
				}
				catch
				{
					break;
				}
			}
			FakeProgressBar.Visibility = Visibility.Hidden;
			CheckButtons();
			StartTraceRteBtn.Visibility = Visibility.Visible;
			TraceView.IsReadOnly = false;
			stopWatch.Stop();
			Logger.Info($"Total trace route time {stopWatch.ElapsedMilliseconds}ms");
		}

		private void CreateHostFromTrace(object sender, RoutedEventArgs e)
		{
			var hostName = (sender as Button)?.Uid;
			if (hostName != "N/A")
			{
				(Owner as MainWindow)?.AddTab(hostName);
			}
		}

		readonly List<Task> _hostNameLookupTasks = new();
		int _hostsLookedUp;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0042:Deconstruct variable declaration", Justification = "<Pending>")]
		private async Task RunTraceRoute()
		{
			string data = Util.RandomString(_host.PacketSize);
			Logger.Debug($"Data string: {data}");

			byte[] buffer = Encoding.ASCII.GetBytes(data);
			using var ping = new Ping();
			for (int ttl = 1; ttl <= 128; ttl++)
			{
				var pingOpts = new PingOptions(ttl, true);
				var reply = await ping.SendPingAsync(_host.HostName, _host.Timeout, buffer, pingOpts);
				if (reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired)
				{
					var newId = Guid.NewGuid();
					TraceReplies.Add(new TraceReply
					{
						Id = newId,
						IPAddress = reply.Address.ToString(),
						PingTimes = new string[3],
						Ttl = ttl,
						HostName = "Looking up host...",
						IPAddButtonVisible = Visibility.Hidden,
						HostAddButtonVisible = Visibility.Hidden
					});
					TraceView.ScrollIntoView(TraceReplies.Last());
					var ttl2 = ttl;

					_hostNameLookupTasks.Add(Task.Run(() =>
					{
						var ttl1 = ttl2;

						Dns.GetHostEntryAsync(reply.Address).ContinueWith(hostEntryTask =>
						{
							Logger.Debug($"Starting lookup for address {reply.Address} with ID {newId}");
							try
							{
								TraceReplies.First(t => t.Id == newId).HostName = hostEntryTask.Result.HostName;
							}
							catch (Exception ex)
							{
								Logger.Debug($"Unable to find host entry for IP {reply.Address}");
								Logger.Log.Debug(ex, $"Exception data for {reply.Address} with Ttl {ttl1} and ID of {newId}");
								TraceReplies.First(t => t.Id == newId).HostName = "N/A";
							}
							Interlocked.Increment(ref _hostsLookedUp);
							Logger.Debug($"hostsLookedUp: {_hostsLookedUp}");
							_syncCtx.Post(o =>
							{
								TraceView.Items.Refresh();
							}, null);
						}, _cancelToken.Token);
					}, _cancelToken.Token));


					var firstTry = await _pinger.GetSingleRoundTrip(reply.Address, ttl + 1);
					TraceReplies.First(t => t.Id == newId).PingTimes[0] = firstTry.Status != IPStatus.Success ? firstTry.Status.ToString() : firstTry.RoundTrip.ToString() + "ms";
					TraceView.Items.Refresh();

					if (firstTry.Status == IPStatus.Success)
						await Task.Delay(250, _cancelToken.Token);

					var secondTry = await _pinger.GetSingleRoundTrip(reply.Address, ttl + 1);
					TraceReplies.First(t => t.Id == newId).PingTimes[1] = secondTry.Status != IPStatus.Success ? secondTry.Status.ToString() : secondTry.RoundTrip.ToString() + "ms";
					TraceView.Items.Refresh();
					if (secondTry.Status == IPStatus.Success)
						await Task.Delay(250, _cancelToken.Token);

					var thirdTry = await _pinger.GetSingleRoundTrip(reply.Address, ttl + 1);
					TraceReplies.First(t => t.Id == newId).PingTimes[2] = thirdTry.Status != IPStatus.Success ? thirdTry.Status.ToString() : thirdTry.RoundTrip.ToString() + "ms";
					TraceView.Items.Refresh();

					if (thirdTry.Status == IPStatus.Success)
						await Task.Delay(250, _cancelToken.Token);
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
				TraceView.Items.Refresh();
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			_cancelToken.Cancel();
			TraceView.ItemsSource = null;
			TraceReplies.Clear();
		}
	}
}
