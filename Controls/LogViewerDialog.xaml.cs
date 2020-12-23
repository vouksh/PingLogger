﻿using PingLogger.Models;
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
		private readonly Tail Tail;
		private readonly SynchronizationContext syncCtx;
		private bool isRunning = false;
		public LogViewerDialog(Host host)
		{
			InitializeComponent();
			CloseWindowCommand = new Command(Close);

			Tail = new Tail($"{Config.LogSavePath}{host.HostName}{Path.DirectorySeparatorChar}{host.HostName}-{DateTime.Now:yyyyMMdd}.log", 1024);
			Tail.Changed += Tail_Changed;
			syncCtx = SynchronizationContext.Current;
		}

		private async void Tail_Changed(object sender, Tail.TailEventArgs e)
		{
			await Task.Run(() =>
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
			});
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			textBlock.Text = "";
			if (isRunning)
				Tail.Stop();
		}

		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				await Task.Run(() => { Tail.Run(); });
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
