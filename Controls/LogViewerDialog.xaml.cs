using PingLogger.Models;
using PingLogger.Workers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PingLogger.Controls
{
	/// <summary>
	/// Interaction logic for LogViewerDialog.xaml
	/// </summary>
	public partial class LogViewerDialog : Window
	{
		public ICommand CloseWindowCommand { get; set; }
		private readonly Tail _tail;
		private readonly SynchronizationContext _syncCtx;
		private bool _isRunning;
		public LogViewerDialog(Host host)
		{
			InitializeComponent();
			CloseWindowCommand = new Command(Close);

			_tail = new Tail($"{Config.LogSavePath}{host.HostName}{Path.DirectorySeparatorChar}{host.HostName}-{DateTime.Now:yyyyMMdd}.log", 1024);
			_tail.Changed += Tail_Changed;
			_syncCtx = SynchronizationContext.Current;
		}

		private async void Tail_Changed(object sender, Tail.TailEventArgs e)
		{
			await Task.Run(() =>
			{
				bool scrollToBottom = Math.Abs(ScrollViewer.VerticalOffset - ScrollViewer.ScrollableHeight) < 1.0;

				_syncCtx.Post(o =>
							{
								TextBlock.Text += (string)o;
								if (scrollToBottom)
									ScrollViewer.ScrollToBottom();
							},
							e.Line);
			});
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			TextBlock.Text = "";
			if (_isRunning)
				_tail.Stop();
		}

		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				await Task.Run(() => { _tail.Run(); });
				_isRunning = true;
			}
			catch (FileNotFoundException)
			{
				MessageBox.Show("Please start the ping logger at least once before attempting to watch it", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				Close();
			}
		}
	}
}
