using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.ComponentModel;
using Avalonia.Threading;

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
			vm!.TabItems.ForEach(pc =>
			{
				((((pc.Content as PingControl)!).DataContext as ViewModels.PingControlViewModel)!).TriggerPinger(false);
			});
		}
	}
}
