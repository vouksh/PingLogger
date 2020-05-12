using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Reflection;
using System.Linq;
using PingLogger.GUI.Workers;

namespace PingLogger.GUI.Controls
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
			CloseWindowCommand = new Command(CloseWindow);
		}
		private void CloseWindow()
		{
			this.Close();
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
