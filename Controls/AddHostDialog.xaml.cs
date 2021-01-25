using PingLogger.Workers;
using System;
using System.Net;
using System.Threading;
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
		private readonly SynchronizationContext _syncCtx;
		readonly DispatcherTimer _timer;
		private readonly ImageAwesome _spinnerImage;
		private readonly DockPanel _buttonDock;
		public AddHostDialog()
		{
			InitializeComponent();
			_spinnerImage = new ImageAwesome()
			{
				Icon = FontAwesomeIcon.Spinner,
				Spin = true,
				SpinDuration = 2,
				Foreground = Util.IsLightTheme ? Brushes.Black : Brushes.White,
				Width = 14,
				Height = 14,
				ToolTip = "Please wait..."
			};
			_buttonDock = new DockPanel()
			{
				Name = "buttonDock",
				FlowDirection = FlowDirection.LeftToRight,

			};
			_buttonDock.Children.Add(new TextBlock { Text = "Loading" });
			AddBtn.Content = _buttonDock;
			CloseWindowCommand = new Command(Close);
			_syncCtx = SynchronizationContext.Current;
			_timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(500),
				IsEnabled = true
			};
			_timer.Tick += Timer_Tick;
			_timer.Start();
		}

		private async void Timer_Tick(object sender, EventArgs e)
		{
			var foundHost = Config.Hosts.Any(h => h.HostName == HostNameBox.Text);
			if (!foundHost && !IsValidHost)
			{
				if (!_buttonDock.Children.Contains(_spinnerImage))
				{
					_buttonDock.Children.Clear();
					_buttonDock.Children.Add(_spinnerImage);
					_buttonDock.Children.Add(new TextBlock { Text = "Looking up host IP...", Padding = new Thickness(5, 0, 0, 0) });
					AddBtn.ToolTip = "Please wait..";
				}
				string ipAddress = "0.0.0.0";
				try
				{
					if (HostNameBox.Text != string.Empty)
					{
						foreach (var ip in await Dns.GetHostAddressesAsync(HostNameBox.Text))
						{
							if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
							{
								IsValidHost = true;
								ipAddress = ip.ToString();
								break;
							}
						}
					}
				}
				catch (Exception)
				{
					IsValidHost = false;
				}
				_syncCtx.Post(o =>
				{
					IpBox.Text = ipAddress;
					if (o != null) AddBtn.IsEnabled = (bool) o;
				}, IsValidHost);
			}
			if (foundHost)
			{
				_buttonDock.Children.Remove(_spinnerImage);
				_buttonDock.Children.Clear();
				_buttonDock.Children.Add(new ImageAwesome
				{
					Icon = FontAwesomeIcon.Times,
					Foreground = Brushes.Red,
					Width = 14,
					Height = 14,
					ToolTip = "You already have a host with this address."
				});
				_buttonDock.Children.Add(new TextBlock { Text = "Duplicate Host", Padding = new Thickness(5, 0, 0, 0) });
				AddBtn.IsEnabled = false;
				AddBtn.ToolTip = "You already have a host with this address.";
			}
			if (!foundHost && IsValidHost)
			{
				_buttonDock.Children.Remove(_spinnerImage);
				_buttonDock.Children.Clear();
				_buttonDock.Children.Add(new ImageAwesome
				{
					Icon = FontAwesomeIcon.Check,
					Foreground = Brushes.Green,
					Width = 14,
					Height = 14,
					ToolTip = "Add Host."
				});
				_buttonDock.Children.Add(new TextBlock { Text = "Add New Host", Padding = new Thickness(5, 0, 0, 0) });
				AddBtn.ToolTip = "Add Host";
			}
		}

		public bool IsValidHost { get; set; }

		private void AddBtn_Click(object sender, RoutedEventArgs e)
		{
			(this.Owner as MainWindow)?.AddTab(HostNameBox.Text);
			_timer.Stop();
			this.DialogResult = true;
			this.Close();
		}

		private void CancelBtn_Click(object sender, RoutedEventArgs e)
		{
			_timer.Stop();
			this.DialogResult = false;
			this.Close();
		}

		private void Window_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				(this.Owner as MainWindow)?.AddTab(HostNameBox.Text);
				this.DialogResult = true;
				_timer.Stop();
				this.Close();
			}
			else if (e.Key == Key.Escape)
			{
				this.DialogResult = false;
				_timer.Stop();
				this.Close();
			}
		}

		private void HostNameBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			IsValidHost = false;
			_timer.Stop();
			_timer.Start();
			e.Handled = false;
			AddBtn.IsEnabled = false;
		}

		private void HostNameBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			IsValidHost = false;
			_timer?.Stop();
			_timer?.Start();
			e.Handled = false;
		}
	}
}
