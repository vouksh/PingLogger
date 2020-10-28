using PingLogger.Workers;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FontAwesome.WPF;
using System.Linq;

namespace PingLogger.Controls
{
	/// <summary>
	/// Interaction logic for AddHostDialog.xaml
	/// </summary>
	public partial class AddHostDialog : Window
	{
		public ICommand CloseWindowCommand { get; set; }
		private readonly SynchronizationContext syncCtx;
		readonly DispatcherTimer Timer;
		private readonly ImageAwesome spinnerImage;
		public AddHostDialog()
		{
			spinnerImage = new ImageAwesome()
			{
				Icon = FontAwesomeIcon.Spinner,
				Spin = true,
				SpinDuration = 10,
				Foreground = Util.IsLightTheme() ? Brushes.Black : Brushes.White,
				Width = 14,
				Height = 14
			};
			InitializeComponent();
			CloseWindowCommand = new Command(Close);
			syncCtx = SynchronizationContext.Current;
			Timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(500),
				IsEnabled = true
			};
			Timer.Tick += Timer_Tick;
			Timer.Start();
		}

		private async void Timer_Tick(object sender, EventArgs e)
		{
			var foundHost = Config.Hosts.Any(h => h.HostName == hostNameBox.Text);
			if (!foundHost && !IsValidHost)
			{
				AddBtn.Content = spinnerImage;
				try
				{
					foreach (var ip in await Dns.GetHostAddressesAsync(hostNameBox.Text))
					{
						if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
						{
							IsValidHost = true;
							break;
						}
					}
				}
				catch (Exception)
				{
					IsValidHost = false;
				}
				syncCtx.Post(new SendOrPostCallback(o =>
				{
					AddBtn.IsEnabled = (bool)o;
				}), IsValidHost);
			}
			if (foundHost)
			{
				AddBtn.Content = new ImageAwesome
				{
					Icon = FontAwesomeIcon.Times,
					Foreground = Brushes.Red,
					Width = 14,
					Height = 14
				};
				AddBtn.IsEnabled = false;
			}
			if (!foundHost && IsValidHost)
			{
				AddBtn.Content = "Add Host";
			}
		}

		public bool IsValidHost { get; set; } = false;

		private void AddBtn_Click(object sender, RoutedEventArgs e)
		{
			(this.Owner as MainWindow).AddTab(hostNameBox.Text);
			Timer.Stop();
			this.DialogResult = true;
			this.Close();
		}

		private void CancelBtn_Click(object sender, RoutedEventArgs e)
		{
			Timer.Stop();
			this.DialogResult = false;
			this.Close();
		}

		private void Window_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				(this.Owner as MainWindow).AddTab(hostNameBox.Text);
				this.DialogResult = true;
				Timer.Stop();
				this.Close();
			}
			else if (e.Key == Key.Escape)
			{
				this.DialogResult = false;
				Timer.Stop();
				this.Close();
			}
		}

		private void HostNameBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			IsValidHost = false;
			AddBtn.Content = spinnerImage;
			Timer.Stop();
			Timer.Start();
			e.Handled = false;
			AddBtn.IsEnabled = false;
		}
	}
}
