using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PingLogger.Views
{
	public class SplashScreen : Window
	{
		public SplashScreen()
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
