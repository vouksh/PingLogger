using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;
using PingLogger.Workers;
using System.Net;
using PingLogger.Models;

namespace PingLogger.ViewModels
{
	public class AddHostDialogViewModel : ViewModelBase
	{
		public ReactiveCommand<Unit, Unit> SubmitCommand { get; }
		public ReactiveCommand<Unit, Unit> CancelCommand { get; }
		public delegate void WindowClosedHandler(object sender, AddHostEventArgs e);
		public event WindowClosedHandler WindowClosed;
		readonly DispatcherTimer Timer;

		public AddHostDialogViewModel()
		{
			SubmitCommand = ReactiveCommand.Create(Submit);
			CancelCommand = ReactiveCommand.Create(Cancel);

			Timer = new DispatcherTimer()
			{
				Interval = TimeSpan.FromMilliseconds(500)
			};
			Timer.Tick += Timer_Tick;
			Timer.Start();
		}

		private async void Timer_Tick(object sender, EventArgs e)
		{
			if(!Config.Hosts.Any(h => h.HostName == HostName || h.IP == HostName))
			{
				SpinnerVisible = true;
				TextVisible = false;
				InvalidIconVisible = false;
				try
				{
					if(!string.IsNullOrWhiteSpace(HostName))
					{
						foreach(var ip in await Dns.GetHostAddressesAsync(HostName))
						{
							if(ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
							{
								IPAddress = ip.ToString();
								AddEnabled = true;
								SpinnerVisible = false;
								TextVisible = true;
								break;
							}
						}
					}
				}
				catch
				{
					AddEnabled = false;
					SpinnerVisible = false;
					TextVisible = true;
					InvalidIconVisible = false;
				}
			} else
			{
				AddEnabled = false;
				SpinnerVisible = false;
				TextVisible = false;
				InvalidIconVisible = true;
			}
			Timer.Stop();
		}

		private string hostName = "google.com";
		public string HostName
		{
			get => hostName;
			set
			{
				InvalidIconVisible = false;
				SpinnerVisible = true;
				TextVisible = false;
				Timer.Stop();
				Timer.Start();
				this.RaiseAndSetIfChanged(ref hostName, value);
			}
		}

		private string ipAddress = "0.0.0.0";
		public string IPAddress
		{
			get => ipAddress;
			set => this.RaiseAndSetIfChanged(ref ipAddress, value);
		}

		private bool textVisible = true;
		public bool TextVisible
		{
			get => textVisible;
			set => this.RaiseAndSetIfChanged(ref textVisible, value);
		}

		private bool spinnerVisible = false;
		public bool SpinnerVisible
		{
			get => spinnerVisible;
			set => this.RaiseAndSetIfChanged(ref spinnerVisible, value);
		}

		private bool addEnabled = false;
		public bool AddEnabled
		{
			get => addEnabled;
			set => this.RaiseAndSetIfChanged(ref addEnabled, value);
		}

		private bool invalidIconVisible = false;
		public bool InvalidIconVisible
		{
			get => invalidIconVisible;
			set => this.RaiseAndSetIfChanged(ref invalidIconVisible, value);
		}

		private void Submit()
		{
			WindowClosed?.Invoke(this, new AddHostEventArgs(HostName, IPAddress, true));
		}

		private void Cancel()
		{
			WindowClosed?.Invoke(this, new AddHostEventArgs(string.Empty, string.Empty, false));
		}
	}
}
