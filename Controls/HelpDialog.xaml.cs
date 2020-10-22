using PingLogger.Workers;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
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
			var resourceNames = assembly.GetManifestResourceNames();
			var resourceName = resourceNames.Single(s => s.EndsWith("README.md"));
			using Stream stream = assembly.GetManifestResourceStream(resourceName);
			using StreamReader reader = new StreamReader(stream);
			string result = reader.ReadToEnd();

			editSource.Text = result;
		}
	}
}
