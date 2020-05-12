using PingLogger.GUI.Models;
using PingLogger.GUI.Workers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PingLogger.GUI.Controls
{
	/// <summary>
	/// Interaction logic for LogViewerDialog.xaml
	/// </summary>
	public partial class LogViewerDialog : Window
	{
		public ICommand CloseWindowCommand { get; set; }
		private Tail Tail;
		private readonly SynchronizationContext syncCtx;
		public LogViewerDialog(Host host)
		{
			InitializeComponent();
			CloseWindowCommand = new Command(CloseWindow);
			Tail = new Tail($"./Logs/{host.HostName}-{DateTime.Now:yyyyMMdd}.log", 20);
			Tail.Changed += Tail_Changed;
			syncCtx = SynchronizationContext.Current;
		}

		private void CloseWindow() => this.Close();

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

				if(scrollToBottom)
					scrollViewer.ScrollToBottom();
			}),
			e.Line);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Tail.Stop();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			Tail.Run();
		}
	}
}
