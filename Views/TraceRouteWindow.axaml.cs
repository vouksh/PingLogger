using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PingLogger.Views
{
	public class TraceRouteWindow : Window
	{
		public TraceRouteWindow()
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
	}
}
