using PingLogger.Models;
using PingLogger.Workers;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PingLogger.ViewModels
{
	public class TraceRouteViewModel : ViewModelBase
	{
		public ObservableCollection<TraceReply> TraceReplies { get; } = new ObservableCollection<TraceReply>();
		public Pinger _pinger;
		public Host Host;
		public ReactiveCommand<Unit, Unit> StartCommand { get; }
		public ReactiveCommand<Unit, Unit> CloseWindowCommand { get; }
		public ReactiveCommand<string, Unit> AddHostCommand { get; }
		readonly List<Task> HostNameLookupTasks = new();
		int hostsLookedUp = 0;
		private readonly CancellationTokenSource cancelToken = new();
		private readonly SynchronizationContext syncCtx;
		public delegate void TraceRouteCallbackHandler(object sender, TraceRouteCallbackEventArgs e);
		public event TraceRouteCallbackHandler TraceRouteCallback;

		public TraceRouteViewModel()
		{
			StartCommand = ReactiveCommand.Create(StartTraceRoute);
			AddHostCommand = ReactiveCommand.Create<string>(CreateHost);
			TraceReplies.CollectionChanged += TraceReplies_CollectionChanged;
			syncCtx = SynchronizationContext.Current;
		}

		private void CreateHost(string hostName)
		{
			TraceRouteCallback?.Invoke(this, new TraceRouteCallbackEventArgs(hostName));
		}

		private bool progressBarVisible = false;
		public bool ProgressBarVisible
		{
			get => progressBarVisible;
			private set => this.RaiseAndSetIfChanged(ref progressBarVisible, value);
		}

		private int progressBarMax = -1;
		public int ProgressBarMax
		{
			get => progressBarMax;
			private set => this.RaiseAndSetIfChanged(ref progressBarMax, value);
		}

		private int progressBarValue = 1;
		public int ProgressBarValue
		{
			get => progressBarValue;
			set => this.RaiseAndSetIfChanged(ref progressBarValue, value);
		}

		private string currentPing = string.Empty;
		public string CurrentPing
		{
			get => currentPing;
			private set => this.RaiseAndSetIfChanged(ref currentPing, value);
		}

		private string hostName = string.Empty;
		public string HostName
		{
			get => string.IsNullOrWhiteSpace(hostName) ? Host?.HostName : hostName;
			private set => this.RaiseAndSetIfChanged(ref hostName, value);
		}

		public void CloseWindow(object sender, EventArgs e)
		{
			cancelToken.Cancel();
			TraceReplies.Clear();
		}

		private void TraceReplies_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add)
				ProgressBarMax = TraceReplies.Count * 4;

			this.RaisePropertyChanged(nameof(TraceReplies));
		}

		private async void StartTraceRoute()
		{
			var stopWatch = new Stopwatch();
			stopWatch.Start();
			hostsLookedUp = 0;
			TraceReplies.Clear();
			ProgressBarVisible = true;
			if (_pinger is null)
			{
				_pinger = new Pinger(Host);
			}
			var (RoundTrip, _) = await _pinger.GetSingleRoundTrip(IPAddress.Parse(Host.IP), 128);
			CurrentPing = $"{RoundTrip}ms";
			try
			{
				await RunTraceRoute();
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
						await Task.Delay(50, cancelToken.Token);
					}
					catch { break; }
				}
				CheckButtons();
				stopWatch.Stop();
				Log.Information($"Total trace route time {stopWatch.ElapsedMilliseconds}ms");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Traceroute cancelled");
			}
		}

		private async Task RunTraceRoute()
		{
			var result = new List<TraceReply>();
			string data = Utils.RandomString(Host.PacketSize);
			byte[] buffer = Encoding.ASCII.GetBytes(data);

			using var ping = new Ping();
			for (int ttl = 1; ttl < 128; ttl++)
			{
				var pingOpts = new PingOptions(ttl, true);
				var reply = await ping.SendPingAsync(Host.HostName, Host.Timeout, buffer, pingOpts);
				if (reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired)
				{
					var newReply = new TraceReply
					{
						Id = Guid.NewGuid(),
						IPAddress = reply.Address.ToString(),
						Ttl = ttl,
						HostName = "Looking up host...",
						HostAddButtonVisible = false,
						IPAddButtonVisible = false
					};
					TraceReplies.Add(newReply);
					int index = TraceReplies.IndexOf(newReply);
					HostNameLookupTasks.Add(Task.Run(async () =>
					{
						try
						{
							var hostEntry = await Dns.GetHostEntryAsync(reply.Address);
							syncCtx.Post(new SendOrPostCallback(o =>
							{
								newReply.HostName = hostEntry.HostName;
								newReply.HostAddButtonVisible = true;
								TraceReplies[index] = newReply;
								this.RaisePropertyChanged(nameof(TraceReplies));
							}), null);
						}
						catch (System.Net.Sockets.SocketException ex)
						{
							Log.Debug(ex, $"Unable to find host entry for IP {reply.Address}");
							syncCtx.Post(new SendOrPostCallback(o =>
							{
								newReply.HostName = "N/A";
								newReply.IPAddButtonVisible = true;
								TraceReplies[index] = newReply;
								this.RaisePropertyChanged(nameof(TraceReplies));
							}), null);
						}
						catch (InvalidOperationException ex)
						{
							Log.Error(ex, "Invalid operation exception");
						}
						Interlocked.Increment(ref hostsLookedUp);
						ProgressBarValue++;
					}, cancelToken.Token));
					var (RoundTrip1, Status1) = await _pinger.GetSingleRoundTrip(reply.Address, ttl + 1);
					newReply.Ping1 = Status1 != IPStatus.Success ? Status1.ToString() : RoundTrip1.ToString() + "ms";
					TraceReplies[index] = newReply;
					this.RaisePropertyChanged(nameof(TraceReplies));
					ProgressBarValue++;
					if (Status1 == IPStatus.Success)
						await Task.Delay(250, cancelToken.Token);

					var (RoundTrip2, Status2) = await _pinger.GetSingleRoundTrip(reply.Address, ttl + 1);
					newReply.Ping2 = Status2 != IPStatus.Success ? Status2.ToString() : RoundTrip2.ToString() + "ms";
					TraceReplies[index] = newReply;
					this.RaisePropertyChanged(nameof(TraceReplies));
					ProgressBarValue++;
					if (Status2 == IPStatus.Success)
						await Task.Delay(250, cancelToken.Token);

					var (RoundTrip3, Status3) = await _pinger.GetSingleRoundTrip(reply.Address, ttl + 1);
					newReply.Ping3 = Status3 != IPStatus.Success ? Status3.ToString() : RoundTrip3.ToString() + "ms";
					TraceReplies[index] = newReply;
					this.RaisePropertyChanged(nameof(TraceReplies));
					ProgressBarValue++;

					if (Status3 == IPStatus.Success)
						await Task.Delay(250, cancelToken.Token);
				}
				if (reply.Address.ToString() == "0.0.0.0" || reply.Address.ToString() == Host.IP)
				{
					break;
				}
			}
		}

		private void CheckButtons()
		{
			for (int i = 0; i < TraceReplies.Count; i++)
			{
				// Check all 3 ping times to see if they were valid or not. We can just discard the result, we don't care what it is.
				bool firstReply = int.TryParse(TraceReplies[i].Ping1.Replace("ms", ""), out _);
				bool secondReply = int.TryParse(TraceReplies[i].Ping2.Replace("ms", ""), out _);
				bool thirdReply = int.TryParse(TraceReplies[i].Ping3.Replace("ms", ""), out _);

				if (TraceReplies[i].HostName == "Looking up host...")
				{
					TraceReplies[i].HostName = "N/A";
				}

				// if the host name is N/A, then it's not a valid host.
				bool isValidHost = TraceReplies[i].HostName != "N/A";

				if (firstReply || secondReply || thirdReply)
				{
					// At least one of the replies was good. So we can make one of the buttons visible.
					if (isValidHost)
					{
						TraceReplies[i].HostAddButtonVisible = true;
						TraceReplies[i].IPAddButtonVisible = false;
					}
					else
					{
						TraceReplies[i].HostAddButtonVisible = false;
						TraceReplies[i].IPAddButtonVisible = true;
					}
				}
				else
				{
					// None of the replies were good. Lets hide both buttons.
					TraceReplies[i].HostAddButtonVisible = false;
					TraceReplies[i].IPAddButtonVisible = false;
				}
				ProgressBarValue++;
				this.RaisePropertyChanged(nameof(TraceReplies));
			}
		}
	}
}
