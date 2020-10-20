using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
	/// Interaction logic for UpdatePromptDialog.xaml
	/// </summary>
	public partial class UpdatePromptDialog : Window
	{
		//System.Threading.Timer timer;
		public bool buttonClicked = false;
		public UpdatePromptDialog()
		{
			InitializeComponent();
			//timer = new System.Threading.Timer(OnTimerTick, null, 15, System.Threading.Timeout.Infinite);

		}

		public static bool Show()
		{
			var promptWindow = new UpdatePromptDialog();
			Task.Factory.StartNew(() =>
			{
				for (var i = 15; i > 0; i--)
				{
					if(promptWindow.buttonClicked)
					{
						break;
					}
					promptWindow.Dispatcher.Invoke(() =>
					{
						promptWindow.yesBtn.Content = $"Yes ({i}s)";
					});
					Thread.Sleep(1000);
				}
				promptWindow.Dispatcher.Invoke(promptWindow.Close);
			});

			promptWindow.ShowDialog();
			if (!promptWindow.buttonClicked)
			{
				return true;
			}
			while (!promptWindow.DialogResult.HasValue) { }
			return promptWindow.DialogResult.Value;
		}

		void OnTimerTick(object state)
		{

		}

		private void yesBtn_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
			buttonClicked = true;
		}

		private void noBtn_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			buttonClicked = true;
		}
	}
}
