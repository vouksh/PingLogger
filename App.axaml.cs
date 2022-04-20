using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using PingLogger.ViewModels;
using PingLogger.Views;
using System.Threading.Tasks;
using Avalonia.Media;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using PingLogger.Workers;
using Material.Colors;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;

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
			AppCenter.Start("9301ab16-71c1-4d0a-ad12-894e5db36532", typeof(Analytics), typeof(Crashes));
			await Analytics.SetEnabledAsync(Config.AllowAnalytics);
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
			
			switch (Config.Theme)
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
			var paletteHelper = new PaletteHelper();
			var curTheme = paletteHelper.GetTheme();
			curTheme.SetBaseTheme(BaseThemeMode.Light.GetBaseTheme());
			var colorProps = typeof(Colors).GetProperties().OrderBy(p => p.Name);
			Color primaryColor = (Color)colorProps.ElementAt(Config.PrimaryColor).GetValue(typeof(Colors));
			Color secondaryColor = (Color)colorProps.ElementAt(Config.PrimaryColor).GetValue(typeof(Colors));
			curTheme.SetPrimaryColor(primaryColor);
			curTheme.SetSecondaryColor(secondaryColor);
			paletteHelper.SetTheme(curTheme);
			DarkMode = false;
		}

		private void SetDarkTheme()
		{
			var paletteHelper = new PaletteHelper();
			var curTheme = paletteHelper.GetTheme();
			curTheme.SetBaseTheme(BaseThemeMode.Dark.GetBaseTheme());
			var colorProps = typeof(Colors).GetProperties().OrderBy(p => p.Name);
			Color primaryColor = (Color)colorProps.ElementAt(Config.PrimaryColor).GetValue(typeof(Colors));
			Color secondaryColor = (Color)colorProps.ElementAt(Config.PrimaryColor).GetValue(typeof(Colors));
			curTheme.SetPrimaryColor(primaryColor);
			curTheme.SetSecondaryColor(secondaryColor);
			paletteHelper.SetTheme(curTheme);
			DarkMode = true;
		}
	}
}
