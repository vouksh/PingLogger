using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.ComponentModel;

namespace PingLogger.Views
{
	public class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public void Window_Closing(object sender, CancelEventArgs e)
		{
			var vm = DataContext as ViewModels.MainWindowViewModel;
			foreach(var tabItem in vm.TabItems)
			{
				var pcDC = (tabItem.Content as PingControl).DataContext as ViewModels.PingControlViewModel;
				pcDC.TriggerPinger("false");
			}
		}
	}
}
