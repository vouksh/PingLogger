using PingLogger.GUI.Workers;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PingLogger.GUI.Controls
{
	/// <summary>
	/// Interaction logic for AddHostDialog.xaml
	/// </summary>
	public partial class AddHostDialog : Window
	{
		public ICommand CloseWindowCommand { get; set; }
		private readonly SynchronizationContext syncCtx;
		readonly DispatcherTimer Timer;
		public AddHostDialog()
		{
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
			if (!IsValidHost)
			{
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

		private void hostNameBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			IsValidHost = false;
			e.Handled = false;
			AddBtn.IsEnabled = false;
		}
	}
}
