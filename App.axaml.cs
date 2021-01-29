using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using PingLogger.ViewModels;
using PingLogger.Views;
using System.Threading.Tasks;

namespace PingLogger
{
	public class App : Application
	{
		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public static bool DarkMode { get; set; } = false;

		public override async void OnFrameworkInitializationCompleted()
		{
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				SetTheme();
				var mwVM = new MainWindowViewModel();
				mwVM.ThemeChanged += (object s) => SetTheme();
				desktop.MainWindow = new MainWindow
				{
					DataContext = mwVM
				};
#if Windows
				if (Workers.Config.EnableAutoUpdate)
				{
					while (!await Utils.Win.CheckForUpdates())
					{
						desktop.MainWindow.Hide();
						await Task.Delay(250);
					}
					desktop.MainWindow.Show();
				}
#endif
				if (Workers.Config.StartApplicationMinimized)
				{
					desktop.MainWindow.WindowState = Avalonia.Controls.WindowState.Minimized;
				}
			}

			base.OnFrameworkInitializationCompleted();
		}

		private void SetTheme()
		{
			switch (Workers.Config.Theme)
			{
				case Models.Theme.Auto:
					if (Utils.IsLightTheme)
						SetLightTheme();
					else
						SetDarkTheme();
					break;
				case Models.Theme.Dark:
					SetDarkTheme();
					break;
				case Models.Theme.Light:
					SetLightTheme();
					break;
			}
		}

		private void SetLightTheme()
		{
			var theme = new Avalonia.Themes.Fluent.FluentTheme(new System.Uri("avares://Avalonia.Themes.Fluent/FluentTheme.xaml"));
			var oxyPlotUri = new System.Uri("resm:OxyPlot.Avalonia.Themes.Default.xaml?assembly=OxyPlot.Avalonia");
			var lightUri = new System.Uri("avares://PingLogger/Themes/Light.axaml");
			var lightXAML = new StyleInclude(lightUri)
			{
				Source = lightUri
			};
			var oxyPlotTheme = new StyleInclude(oxyPlotUri)
			{
				Source = oxyPlotUri
			};
			Styles.Clear();
			theme.Mode = Avalonia.Themes.Fluent.FluentThemeMode.Light;
			Styles.Add(oxyPlotTheme);
			Styles.Add(theme);
			Styles.Add(lightXAML);
			DarkMode = false;
		}

		private void SetDarkTheme()
		{
			var theme = new Avalonia.Themes.Fluent.FluentTheme(new System.Uri("avares://Avalonia.Themes.Fluent/FluentTheme.xaml"));
			var oxyPlotUri = new System.Uri("resm:OxyPlot.Avalonia.Themes.Default.xaml?assembly=OxyPlot.Avalonia");
			var darkUri = new System.Uri("avares://PingLogger/Themes/Dark.axaml");
			var darkXAML = new StyleInclude(darkUri)
			{
				Source = darkUri
			};
			var oxyPlotTheme = new StyleInclude(oxyPlotUri)
			{
				Source = oxyPlotUri
			};
			Styles.Clear();
			theme.Mode = Avalonia.Themes.Fluent.FluentThemeMode.Dark;
			Styles.Add(oxyPlotTheme);
			Styles.Add(theme);
			Styles.Add(darkXAML);
			DarkMode = true;
		}
	}
}
