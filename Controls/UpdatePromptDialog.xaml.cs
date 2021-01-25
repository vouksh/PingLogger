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
		public bool ButtonClicked;
		public UpdatePromptDialog()
		{
			InitializeComponent();

		}

		public new static bool Show()
		{
			var promptWindow = new UpdatePromptDialog();
			Task.Factory.StartNew(() =>
			{
				for (var i = 15; i > 0; i--)
				{
					if (promptWindow.ButtonClicked)
					{
						break;
					}

					var i1 = i;

					promptWindow.Dispatcher.Invoke(() =>
					{
						promptWindow.YesBtn.Content = $"Yes ({i1}s)";
					});
					Thread.Sleep(1000);
				}
				promptWindow.Dispatcher.Invoke(promptWindow.Close);
			});

			promptWindow.ShowDialog();
			if (!promptWindow.ButtonClicked)
			{
				return true;
			}
			while (!promptWindow.DialogResult.HasValue) { }
			return promptWindow.DialogResult.Value;
		}

		private void YesBtn_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
			ButtonClicked = true;
		}

		private void NoBtn_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			ButtonClicked = true;
		}
	}
}
