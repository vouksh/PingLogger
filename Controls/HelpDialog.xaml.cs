using PingLogger.Workers;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace PingLogger.Controls
{
	/// <summary>
	/// Interaction logic for HelpDialog.xaml
	/// </summary>
	public partial class HelpDialog : Window
	{
		public ICommand CloseWindowCommand { get; set; }
		public HelpDialog()
		{
			InitializeComponent();
			CloseWindowCommand = new Command(Close);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			var assembly = Assembly.GetExecutingAssembly();
			var resourceName = assembly.GetManifestResourceNames().Single(s => s.EndsWith("README.md"));
			using Stream stream = assembly.GetManifestResourceStream(resourceName);
			using StreamReader reader = new StreamReader(stream);
			string result = reader.ReadToEnd();

			helpText.Text = result;
		}
	}
}
