using PingLogger.GUI.Workers;
using System.Windows;
using System.Windows.Input;

namespace PingLogger.GUI.Controls
{
	/// <summary>
	/// Interaction logic for AddHostDialog.xaml
	/// </summary>
	public partial class AddHostDialog : Window
	{
		public ICommand CloseWindowCommand { get; set; }
		public AddHostDialog()
		{
			InitializeComponent();
			CloseWindowCommand = new Command(Close);
		}

		private void AddBtn_Click(object sender, RoutedEventArgs e)
		{
			(this.Owner as MainWindow).AddTab(hostNameBox.Text);
			this.DialogResult = true;
			this.Close();
		}

		private void CancelBtn_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.Close();
		}

		private void Window_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				(this.Owner as MainWindow).AddTab(hostNameBox.Text);
				this.DialogResult = true;
				this.Close();
			}
			else if (e.Key == Key.Escape)
			{
				this.DialogResult = false;
				this.Close();
			}
		}
	}
}
