using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PingLogger.Views
{
	public class PingControl : UserControl
	{
		public PingControl()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
