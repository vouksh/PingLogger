using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using PingLogger.ViewModels;
using PingLogger.Views;
using ReactiveUI;
using System.Threading.Tasks;
using PingLogger.Extensions;

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
				if (System.OperatingSystem.IsWindows() && Workers.Config.EnableAutoUpdate)
				{
					while (!await Utils.CheckForUpdates())
					{
						desktop.MainWindow.Hide();
						await Task.Delay(250);
					}
					desktop.MainWindow.Show();
				}
				if (Workers.Config.StartApplicationMinimized)
				{
					desktop.MainWindow.WindowState = Avalonia.Controls.WindowState.Minimized;
				}
			}

			base.OnFrameworkInitializationCompleted();
		}

		private void SetTheme()
		{
			var oxyPlotUri = new System.Uri("resm:OxyPlot.Avalonia.Themes.Default.xaml?assembly=OxyPlot.Avalonia");
			var theme = new Avalonia.Themes.Fluent.FluentTheme(new System.Uri("avares://Avalonia.Themes.Fluent/FluentTheme.xaml"));
			var darkUri = new System.Uri("avares://PingLogger/Themes/Dark.axaml");
			var darkXAML = new StyleInclude(darkUri)
			{
				Source = darkUri
			};
			var lightUri = new System.Uri("avares://PingLogger/Themes/Light.axaml");
			var lightXAML = new StyleInclude(lightUri)
			{
				Source = lightUri
			};
			var oxyPlotTheme = new StyleInclude(oxyPlotUri)
			{
				Source = oxyPlotUri
			};
			switch (Workers.Config.Theme)
			{
				case Models.Theme.Auto:
					if (System.OperatingSystem.IsWindows())
					{
						if (Utils.Win.GetLightMode())
						{
							Styles.Clear();
							theme.Mode = Avalonia.Themes.Fluent.FluentThemeMode.Light;
							Styles.Add(oxyPlotTheme);
							Styles.Add(theme);
							Styles.Add(lightXAML);
							DarkMode = false;
						} else
						{
							Styles.Clear();
							theme.Mode = Avalonia.Themes.Fluent.FluentThemeMode.Dark;
							Styles.Add(oxyPlotTheme);
							Styles.Add(theme);
							Styles.Add(darkXAML);
							DarkMode = true;
						}
					}
					else
					{
						Styles.Clear();
						theme.Mode = Avalonia.Themes.Fluent.FluentThemeMode.Dark;
						Styles.Add(oxyPlotTheme);
						Styles.Add(theme);
						Styles.Add(darkXAML);
						DarkMode = true;
					}
					break;
				case Models.Theme.Dark:
					Styles.Clear();
					theme.Mode = Avalonia.Themes.Fluent.FluentThemeMode.Dark;
					Styles.Add(oxyPlotTheme);
					Styles.Add(theme);
					Styles.Add(darkXAML);
					DarkMode = true;
					break;
				case Models.Theme.Light:
					Styles.Clear();
					theme.Mode = Avalonia.Themes.Fluent.FluentThemeMode.Light;
					Styles.Add(oxyPlotTheme);
					Styles.Add(theme);
					Styles.Add(lightXAML);
					DarkMode = false;
					break;
			}
		}
	}
}
