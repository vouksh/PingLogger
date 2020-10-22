using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PingLogger.Controls
{
	/// <summary>
	/// Interaction logic for UpdatePromptDialog.xaml
	/// </summary>
	public partial class UpdatePromptDialog : Window
	{
		public bool buttonClicked = false;
		public UpdatePromptDialog()
		{
			InitializeComponent();

		}

		public static new bool Show()
		{
			var promptWindow = new UpdatePromptDialog();
			Task.Factory.StartNew(() =>
			{
				for (var i = 15; i > 0; i--)
				{
					if (promptWindow.buttonClicked)
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

		private void YesBtn_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
			buttonClicked = true;
		}

		private void NoBtn_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			buttonClicked = true;
		}
	}
}
