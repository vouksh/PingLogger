using PingLogger.GUI.Models;
using PingLogger.GUI.Workers;
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace PingLogger.GUI.Controls
{
	/// <summary>
	/// Interaction logic for LogViewerDialog.xaml
	/// </summary>
	public partial class LogViewerDialog : Window
	{
		public ICommand CloseWindowCommand { get; set; }
		private readonly Tail Tail;
		private readonly SynchronizationContext syncCtx;
		private bool isRunning = false;
		public LogViewerDialog(Host host)
		{
			InitializeComponent();
			CloseWindowCommand = new Command(Close);

			Tail = new Tail($"./Logs/{host.HostName}-{DateTime.Now:yyyyMMdd}.log", 20);
			Tail.Changed += Tail_Changed;
			syncCtx = SynchronizationContext.Current;
		}

		private void Tail_Changed(object sender, Tail.TailEventArgs e)
		{
			bool scrollToBottom = false;
			if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
			{
				scrollToBottom = true;
			}
			syncCtx.Post(new SendOrPostCallback(o =>
			{
				textBlock.Text += (string)o;

				if (scrollToBottom)
					scrollViewer.ScrollToBottom();
			}),
			e.Line);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if(isRunning)
				Tail.Stop();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				Tail.Run();
				isRunning = true;
			}
			catch (FileNotFoundException)
			{
				MessageBox.Show("Please start the ping logger at least once before attempting to watch it", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				Close();
			}
		}
	}
}
