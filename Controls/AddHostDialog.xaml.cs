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
		private readonly DockPanel buttonDock;
		public AddHostDialog()
		{
			InitializeComponent();
			spinnerImage = new ImageAwesome()
			{
				Icon = FontAwesomeIcon.Spinner,
				Spin = true,
				SpinDuration = 2,
				Foreground = Util.IsLightTheme() ? Brushes.Black : Brushes.White,
				Width = 14,
				Height = 14,
				ToolTip = "Please wait..."
			};
			buttonDock = new DockPanel()
			{
				Name = "buttonDock",
				FlowDirection = FlowDirection.LeftToRight,

			};
			buttonDock.Children.Add(new TextBlock { Text = "Loading" });
			AddBtn.Content = buttonDock;
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
				if (!buttonDock.Children.Contains(spinnerImage))
				{
					buttonDock.Children.Clear();
					buttonDock.Children.Add(spinnerImage);
					buttonDock.Children.Add(new TextBlock { Text = "Looking up host IP...", Padding = new Thickness(5, 0, 0, 0) });
					AddBtn.ToolTip = "Please wait..";
				}
				string ipAddr = "0.0.0.0";
				try
				{
					if (hostNameBox.Text != string.Empty)
					{
						foreach (var ip in await Dns.GetHostAddressesAsync(hostNameBox.Text))
						{
							if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
							{
								IsValidHost = true;
								ipAddr = ip.ToString();
								break;
							}
						}
					}
				}
				catch (Exception)
				{
					IsValidHost = false;
				}
				syncCtx.Post(new SendOrPostCallback(o =>
				{
					ipBox.Text = ipAddr;
					AddBtn.IsEnabled = (bool)o;
				}), IsValidHost);
			}
			if (foundHost)
			{
				buttonDock.Children.Remove(spinnerImage);
				buttonDock.Children.Clear();
				buttonDock.Children.Add(new ImageAwesome
				{
					Icon = FontAwesomeIcon.Times,
					Foreground = Brushes.Red,
					Width = 14,
					Height = 14,
					ToolTip = "You already have a host with this address."
				});
				buttonDock.Children.Add(new TextBlock { Text = "Duplicate Host", Padding = new Thickness(5, 0, 0, 0) });
				AddBtn.IsEnabled = false;
				AddBtn.ToolTip = "You already have a host with this address.";
			}
			if (!foundHost && IsValidHost)
			{
				buttonDock.Children.Remove(spinnerImage);
				buttonDock.Children.Clear();
				buttonDock.Children.Add(new ImageAwesome
				{
					Icon = FontAwesomeIcon.Check,
					Foreground = Brushes.Green,
					Width = 14,
					Height = 14,
					ToolTip = "Add Host."
				});
				buttonDock.Children.Add(new TextBlock { Text = "Add New Host", Padding = new Thickness(5, 0, 0, 0) });
				AddBtn.ToolTip = "Add Host";
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
			Timer.Stop();
			Timer.Start();
			e.Handled = false;
			AddBtn.IsEnabled = false;
		}

		private void HostNameBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			IsValidHost = false;
			Timer?.Stop();
			Timer?.Start();
			e.Handled = false;
		}
	}
}
