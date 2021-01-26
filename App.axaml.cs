using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using PingLogger.ViewModels;
using PingLogger.Views;
using ReactiveUI;

namespace PingLogger
{
	public class App : Application
	{
		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted()
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
			}

			base.OnFrameworkInitializationCompleted();
		}

		private void SetTheme()
		{
			var theme = new Avalonia.Themes.Fluent.FluentTheme(new System.Uri("avares://Avalonia.Themes.Fluent/FluentTheme.xaml"));
			var uri = new System.Uri("avares://PingLogger/Themes/Dark.axaml");
			var darkXAML = new StyleInclude(uri)
			{
				Source = uri
			};
			switch (Workers.Config.Theme)
			{
				case Models.Theme.Auto:
					if (System.OperatingSystem.IsWindows())
					{
						if (WinUtils.GetLightMode())
						{
							Styles.Clear();
							theme.Mode = Avalonia.Themes.Fluent.FluentThemeMode.Light;
							Styles.Add(theme);
						} else
						{
							Styles.Clear();
							theme.Mode = Avalonia.Themes.Fluent.FluentThemeMode.Dark;
							Styles.Add(theme);
							Styles.Add(darkXAML);
						}
					}
					else
					{
						Styles.Clear();
						theme.Mode = Avalonia.Themes.Fluent.FluentThemeMode.Dark;
						Styles.Add(theme);
						Styles.Add(darkXAML);

					}
					break;
				case Models.Theme.Dark:
					Styles.Clear();
					theme.Mode = Avalonia.Themes.Fluent.FluentThemeMode.Dark;
					Styles.Add(theme);
					Styles.Add(darkXAML);
					break;
				case Models.Theme.Light:
					Styles.Clear();
					theme.Mode = Avalonia.Themes.Fluent.FluentThemeMode.Light;
					Styles.Add(theme);
					break;
			}
		}
	}
}
