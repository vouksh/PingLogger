using PingLogger.GUI.Models;
using PingLogger.GUI.Workers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
	/// Interaction logic for TraceRouteControl.xaml
	/// </summary>
	public partial class TraceRouteControl : Window
	{
		private Pinger pinger;
		public ICommand CloseWindowCommand { get; set; }
		public List<TraceReply> TraceReplies = new List<TraceReply>();

		public TraceRouteControl()
		{
			InitializeComponent();
			CloseWindowCommand = new Command(Close);
		}

		public TraceRouteControl(ref Pinger _pinger)
		{
			InitializeComponent();
			CloseWindowCommand = new Command(Close);
			pinger = _pinger;
			hostNameLabel.Content = pinger.UpdateHost().HostName;
			//traceView.ItemsSource = TraceReplies;
		}

		private async void startTraceRteBtn_Click(object sender, RoutedEventArgs e)
		{
			traceView.ItemsSource = null;
			fakeProgressBar.Visibility = Visibility.Visible;
			startTraceRteBtn.Visibility = Visibility.Hidden;
			//traceView.Items.Clear();
			traceView.ItemsSource = await pinger.GetTraceRoute().ToListAsync();
			startTraceRteBtn.Visibility = Visibility.Visible;
			fakeProgressBar.Visibility = Visibility.Hidden;
		}
	}
}
