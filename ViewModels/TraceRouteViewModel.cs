using PingLogger.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PingLogger.Workers;
using ReactiveUI;
using System.Reactive;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using Serilog;
using System.Threading;

namespace PingLogger.ViewModels
{
	public class TraceRouteViewModel : ViewModelBase
	{
		public ObservableCollection<TraceReply> TraceReplies = new ObservableCollection<TraceReply>();
		private readonly Pinger pinger;
		public Host Host;
		public ReactiveCommand<Unit, Unit> StartCommand { get; }
		readonly List<Task> HostNameLookupTasks = new List<Task>();
		int hostsLookedUp = 0;
		private readonly CancellationTokenSource cancelToken = new CancellationTokenSource();

		public TraceRouteViewModel()
		{
			StartCommand = ReactiveCommand.Create(StartTraceRoute);
		}

		private async void StartTraceRoute()
		{
			var stopWatch = new Stopwatch();
			stopWatch.Start();
			hostsLookedUp = 0;
			TraceReplies.Clear();

		}

		private async Task RunTraceRoute()
		{
			var result = new List<TraceReply>();
			string data = Util.RandomString(Host.PacketSize);
			byte[] buffer = Encoding.ASCII.GetBytes(data);

			using var ping = new Ping();
			for(int ttl = 1; ttl < 128; ttl++)
			{
				var pingOpts = new PingOptions(ttl, true);
				var reply = await ping.SendPingAsync(Host.HostName, Host.Timeout, buffer, pingOpts);
				if(reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired)
				{
					var newID = Guid.NewGuid();
					TraceReplies.Add(new TraceReply
					{
						Id = newID,
						IPAddress = reply.Address.ToString(),
						Ttl = ttl,
						HostName = "Looking up host...",
						HostAddButtonVisible = false,
						IPAddButtonVisible = false
					});
					HostNameLookupTasks.Add(Task.Run(() =>
					{
						Dns.GetHostEntryAsync(reply.Address).ContinueWith(hostEntryTask =>
						{
							try
							{
								TraceReplies.First(t => t.Id == newID).HostName = hostEntryTask.Result.HostName;
							}
							catch (Exception ex)
							{
								Log.Debug(ex, $"Unable to find host entry for IP {reply.Address}");
								TraceReplies.First(t => t.Id == newID).HostName = "N/A";
							}
							Interlocked.Increment(ref hostsLookedUp);
							this.RaisePropertyChanged("TraceReplies");
						}, cancelToken);
					}, cancelToken));
					// TODO: add 3 pings.
				}
			}
		}
	}
}
