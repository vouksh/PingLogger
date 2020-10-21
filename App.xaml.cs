using Microsoft.Win32;
using PingLogger.GUI.Models;
using PingLogger.GUI.Workers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Net;

namespace PingLogger.GUI
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		Controls.SplashScreen splashScreen;
		private async void Application_Startup(object sender, StartupEventArgs e)
		{
			SetTheme();
			splashScreen = new Controls.SplashScreen();
			splashScreen.dlProgress.IsIndeterminate = true;
			splashScreen.dlProgress.Value = 1;
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			Logger.Info($"Application start, version {version}");
			Logger.Info($"Application is running from directory {AppContext.BaseDirectory}");

			if (Config.EnableAutoUpdate)
			{
				splashScreen.Show();
				await CheckForUpdates();
			}

			SetTheme();
			MainWindow window = new MainWindow(this);
			window.Show();
			splashScreen.Close();
			splashScreen = null;
		}

		public async Task CheckForUpdates()
		{
			if (Config.AppWasUpdated)
			{
				Logger.Info("Application was updated last time it ran, cleaning up.");
				if (File.Exists("./PingLogger-old.exe"))
				{
					File.Delete("./PingLogger-old.exe");
				}
				if (File.Exists("./tempDir.txt"))
				{
					var installerPath = File.ReadAllText("./tempDir.txt");
					File.Delete(installerPath + "/PingLogger-Setup.exe");
					Directory.Delete(installerPath);
					File.Delete("./tempDir.txt");
				}
				Config.AppWasUpdated = false;
			}
			else
			{
				if(Config.LastUpdated.Date >= DateTime.Today)
				{
					Logger.Info("Application already checked for update today, skipping.");
					return;
				}
				var localVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
				bool appIsInstalled = false;
				string installerGUID = "{39E66F87-E17F-4311-A477-C5F47F7F7B1F}_is1";
				var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
				if (key.GetSubKeyNames().Contains(installerGUID))
				{
					appIsInstalled = true;
					Logger.Info($"Application was installed in {AppContext.BaseDirectory}.");
				}
				try
				{
					HttpClient httpClient = new HttpClient();
					httpClient.DefaultRequestHeaders.Add("User-Agent", "PingLogger Auto-Update");
					var resp = await httpClient.GetAsync("https://api.github.com/repos/vouksh/PingLogger/releases/latest");
					var strResp = await resp.Content.ReadAsStringAsync();
					Logger.Debug(strResp);
					if(strResp.Contains("API rate limit exceeded"))
					{
						Logger.Info("Unable to check for updates due to rate limit.");
						return;
					}
					var parsedResp = System.Text.Json.JsonSerializer.Deserialize<Models.GitHubResponse>(strResp);
					var respVersion = Version.Parse(parsedResp.tag_name.Replace("v", ""));
					Logger.Info($"Most recent version is {respVersion}, currently running {localVersion}");
					if (respVersion > localVersion)
					{
						Logger.Info("Remote contains a newer version");
						if (Controls.UpdatePromptDialog.Show())
						{
							if (appIsInstalled)
							{
								string tempDir = Workers.Pinger.RandomString(8);
								string savePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}Temp\\{tempDir}";
								File.WriteAllText("./tempDir.txt", savePath);
								Logger.Info($"Downloading newest installer to {savePath}\\PingLogger-Setup.exe");
								Directory.CreateDirectory(savePath);
								var downloadURL = parsedResp.assets.First(a => a.name == "PingLogger-Setup.exe").browser_download_url;
								using var downloader = new Workers.HttpClientDownloadWithProgress(downloadURL, savePath + "\\PingLogger-Setup.exe");
								splashScreen.mainLabel.Content = $"Downloading PingLogger setup v{respVersion}";
								downloader.ProgressChanged += Downloader_ProgressChanged;
								await downloader.StartDownload();
								Config.AppWasUpdated = true;
								Logger.Info("Running installer");
								new Process
								{
									StartInfo = new ProcessStartInfo
									{
										FileName = savePath + "\\PingLogger-Setup.exe",
										Verb = "runas",
										UseShellExecute = true,
										Arguments = $"/SP- /VERYSILENT /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS /LOG=\"{AppContext.BaseDirectory}\\Logs\\Installer-v{respVersion}.log\""
									}
								}.Start();
								Logger.Info("Installer completed, closing.");
							}
							else
							{
								Logger.Info("Renamed PingLogger.exe to PingLogger-old.exe");
								File.Move("./PingLogger.exe", "./PingLogger-old.exe");
								Logger.Info("Downloading new PingLogger.exe");
								var downloadURL = parsedResp.assets.First(a => a.name == "PingLogger.exe").browser_download_url;
								using var downloader = new Workers.HttpClientDownloadWithProgress(downloadURL, "./PingLogger.exe");
								splashScreen.mainLabel.Content = $"Downloading PingLogger v{respVersion}";
								downloader.ProgressChanged += Downloader_ProgressChanged;
								await downloader.StartDownload();
								Config.AppWasUpdated = true;
								new Process
								{
									StartInfo = new ProcessStartInfo
									{
										FileName = "./PingLogger.exe"
									}
								}.Start();
								Logger.Info("Starting new version of PingLogger");
								Environment.Exit(0);
							}
						}
					}
				}
				catch (HttpRequestException ex)
				{
					Logger.Error("Unable to auto update: " + ex.Message);
				}
			}
			return;
		}

		private void Downloader_ProgressChanged(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
		{
			if (progressPercentage.HasValue && totalFileSize.HasValue)
			{
				splashScreen.dlProgress.Maximum = 100;
				splashScreen.dlProgress.Value = progressPercentage.Value;
				splashScreen.dlProgress.IsIndeterminate = false;
			}
		}

		public void SetTheme()
		{
			bool useLightTheme = false;
			switch (Config.Theme)
			{
				case Theme.Auto:
					int lightTheme = Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", 1));
					if (lightTheme == 1)
						useLightTheme = true;
					else
						useLightTheme = false;
					break;
				case Theme.Light:
					useLightTheme = true;
					break;
				case Theme.Dark:
					useLightTheme = false;
					break;
			}
			if (useLightTheme)
			{
				this.Resources.MergedDictionaries[0].Source = new Uri("/Themes/LightTheme.xaml", UriKind.Relative);
			}
			else
			{
				this.Resources.MergedDictionaries[0].Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative);
			}
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			MessageBox.Show((e.ExceptionObject as Exception).Message, "An error has occurred", MessageBoxButton.OK, MessageBoxImage.Error);
			Logger.Log.Fatal((e.ExceptionObject as Exception), "A fatal unhandled exception occurred");
		}
	}
}
