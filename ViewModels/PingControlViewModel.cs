using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PingLogger.Models;
using PingLogger.Workers;
using ReactiveUI;

namespace PingLogger.ViewModels
{
	public class PingControlViewModel : ViewModelBase
	{
		public PingControlViewModel()
		{
			syncCtx = SynchronizationContext.Current;
		}
		private readonly SynchronizationContext syncCtx;
		public Host Host { get; set; }
		private Pinger _pinger;

		public string HostName
		{
			get
			{
				return Host.HostName;
			}
			set
			{
				Host.HostName = value;
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

		private async void UpdateIP()
		{
			try
			{
				foreach (var ip in await Dns.GetHostAddressesAsync(HostName))
				{
					if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
					{
						Host.IP = ip.ToString();
						this.RaisePropertyChanged("IPAddress");
						break;
					}
				}
			}
			catch (Exception)
			{
				Host.IP = "Invalid Host Name";
				//doTraceRteBtn.IsEnabled = false;
			}
		}
	}
}
