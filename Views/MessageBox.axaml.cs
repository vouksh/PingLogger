using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;

namespace PingLogger.Views
{
	public class MessageBox : Window
	{

		public static void ShowAsError(string title, string message)
		{
			if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				new MessageBox()
				{
					DataContext = new ViewModels.MessageBoxViewModel()
					{
						OkOnly = true,
						OkColumnSpan = 2,
						CancelVisible = false,
						Message = message,
						DialogTitle = title
					}
				}.ShowDialog(desktop.MainWindow);
			}
		}

		public async static Task<bool> ShowAsYesNo(string title, string message)
		{
			if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				var mbVM = new ViewModels.MessageBoxViewModel()
				{
					CancelVisible = true,
					DialogTitle = title,
					Message = message,
					OkColumnSpan = 1,
					OkOnly = false
				};
				return await new MessageBox()
				{
					DataContext = mbVM
				}.ShowDialog<bool>(desktop.MainWindow);
			}
			return false;
		}

		public MessageBox()
		{
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
		}


		public void OkClicked(object sender, RoutedEventArgs e)
		{
			this.Close(true);
		}

		public void CancelClicked(object sender, RoutedEventArgs e)
		{
			this.Close(false);
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
