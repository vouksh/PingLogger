using PingLogger.Models;
using PingLogger.Workers;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace PingLogger
{
	public static class Util
	{
		static Views.SplashScreen SplashScreen;
		static ViewModels.SplashScreenViewModel SplashScreenViewModel;

		public static string FileBasePath
		{
			get
			{
				if (AppIsClickOnce)
				{
					var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + "PingLogger" + Path.DirectorySeparatorChar;
					if (!Directory.Exists(appDataDir))
					{
						Directory.CreateDirectory(appDataDir);
					}
					return appDataDir;
				}
				else
				{
					return Environment.CurrentDirectory;
				}
			}
		}

		public static bool AppIsClickOnce => File.Exists(AppContext.BaseDirectory + "Launcher.exe") && File.Exists(AppContext.BaseDirectory + "Launcher.manifest");


		public static async Task<bool> CheckForUpdates()
		{
			if (Config.AppWasUpdated)
			{
				Log.Information("Application was updated last time it ran, cleaning up.");
				if (File.Exists("./PingLogger-old.exe"))
				{
					File.Delete("./PingLogger-old.exe");
				}
				if (Config.LastTempDir != string.Empty && Directory.Exists(Config.LastTempDir))
				{
					File.Delete(Config.LastTempDir + "/PingLogger-Setup.msi");
					Directory.Delete(Config.LastTempDir);
					Config.LastTempDir = string.Empty;
				}
				Config.AppWasUpdated = false;
			}
			else
			{
				if (Config.UpdateLastChecked.Date >= DateTime.Today)
				{
					Log.Information("Application already checked for update today, skipping.");
					return true;
				}
				SplashScreenViewModel = new ViewModels.SplashScreenViewModel();
				SplashScreen = new Views.SplashScreen()
				{
					DataContext = SplashScreenViewModel
				};
				SplashScreen.Show();
				SplashScreenViewModel.ProgressBarIndeterminate = true;
				var localVersion = Assembly.GetExecutingAssembly().GetName().Version;

				try
				{
					var httpClient = new WebClient();
					bool downloadComplete = false;
					httpClient.DownloadFileCompleted += (_, _) => { downloadComplete = true; };

					string azureURL = "https://pingloggerfiles.blob.core.windows.net/";

					await httpClient.DownloadFileTaskAsync($"{azureURL}version/latest.json", "./latest.json");

					while (!downloadComplete) { await Task.Delay(100); }
					var latestJson = await File.ReadAllTextAsync("./latest.json");
					var remoteVersion = JsonSerializer.Deserialize<SerializableVersion>(latestJson);
					File.Delete("./latest.json");

					Log.Information($"Remote version is {remoteVersion}, currently running {localVersion}");
					if (remoteVersion > localVersion)
					{
						Log.Information("Remote contains a newer version");
						if (true)
						{
							if (Config.IsInstalled)
							{
								Config.LastTempDir = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Temp\\{RandomString(8)}";
								Directory.CreateDirectory(Config.LastTempDir);
								Log.Information($"Creating temporary path {Config.LastTempDir}");
								Log.Information($"Downloading newest installer to {Config.LastTempDir}\\PingLogger-Setup.msi");

								if (remoteVersion is not null)
								{
									var downloadURL = $"{azureURL}v{remoteVersion.Major}{remoteVersion.Minor}{remoteVersion.Build}/PingLogger-Setup.msi";
									Log.Information($"Downloading from {downloadURL}");
									using var downloader = new HttpClientDownloadWithProgress(downloadURL, Config.LastTempDir + "\\PingLogger-Setup.msi");
									SplashScreenViewModel.UpdateMessage = $"Downloading PingLogger setup v{remoteVersion}";
									downloader.ProgressChanged += Downloader_ProgressChanged;
									await downloader.StartDownload();
								}

								Config.AppWasUpdated = true;
								Log.Information("Uninstalling current version.");
								string batchFile = $@"@echo off
msiexec.exe /q /l* '{ AppContext.BaseDirectory}Logs\Installer - v{localVersion}.log' /x {Config.InstallerGUID}
msiexec.exe /l* '{ AppContext.BaseDirectory}Logs\Installer - v{remoteVersion}.log' /i {Config.LastTempDir}/PingLogger-Setup.msi";
								await File.WriteAllTextAsync(Config.LastTempDir + "/install.bat", batchFile);
								Process.Start(new ProcessStartInfo
								{
									FileName = "cmd.exe",
									UseShellExecute = true,
									Arguments = $"{Config.LastTempDir}/install.bat"
								});

								Log.Information("Installer started, closing.");
								Environment.Exit(0);
							}
							else
							{
								Log.Information("Renamed PingLogger.exe to PingLogger-old.exe");
								File.Move("./PingLogger.exe", "./PingLogger-old.exe");
								Log.Information("Downloading new PingLogger.exe");

								if (remoteVersion is not null)
								{
									var downloadUrl = $"{azureURL}v{remoteVersion.Major}{remoteVersion.Minor}{remoteVersion.Build}/PingLogger.exe";
									Log.Information($"Downloading from {downloadUrl}");
									using var downloader = new HttpClientDownloadWithProgress(downloadUrl, "./PingLogger.exe");
									SplashScreenViewModel.UpdateMessage = $"Downloading PingLogger v{remoteVersion}";
									downloader.ProgressChanged += Downloader_ProgressChanged;
									await downloader.StartDownload();
								}

								Config.AppWasUpdated = true;

								Process.Start(new ProcessStartInfo
								{
									FileName = "./PingLogger.exe"
								});

								Log.Information("Starting new version of PingLogger");
								Environment.Exit(0);
							}
						}
					}
				}
				catch (HttpRequestException ex)
				{
					Log.Error("Unable to auto update: " + ex.Message);
					return true;
				}
			}
			Config.UpdateLastChecked = DateTime.Now;
			CloseSplashScreen();
			return true;
		}

		private static void Downloader_ProgressChanged(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
		{
			if (progressPercentage.HasValue && totalFileSize.HasValue)
			{
				SplashScreenViewModel.ProgressBarMax = 100;
				SplashScreenViewModel.ProgressBarValue = Convert.ToInt32(progressPercentage.Value);
				SplashScreenViewModel.ProgressBarIndeterminate = false;
			}
		}

		public static void CloseSplashScreen()
		{
			try
			{
				SplashScreen.Close();
				SplashScreen = null;
				SplashScreenViewModel = null;
			}
			catch (NullReferenceException ex)
			{
				Log.Debug(ex, "splashScreen was null.");
			}
		}

		/// <summary>
		/// Generates a random string with the specified length.
		/// </summary>
		/// <param name="length">Number of characters in the string</param>
		/// <returns>Random string of letters and numbers</returns>
		public static string RandomString(int length)
		{
			Random random = new Random();
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, length)
			  .Select(s => s[random.Next(s.Length)]).ToArray());
		}

		public static bool IsLightTheme
		{
			get
			{
				if (Config.Theme == Theme.Auto)
				{
					if (OperatingSystem.IsWindows())
					{
						return WinUtils.GetLightMode();
					}
					else
					{
						return true;
					}
				}
				else
				{
					return Config.Theme == Theme.Light;
				}
			}
		}

		public static void SetTheme()
		{
			//Application.Current.Resources.MergedDictionaries[0].Source = IsLightTheme ? new Uri("/Themes/LightTheme.xaml", UriKind.Relative) : new Uri("/Themes/DarkTheme.xaml", UriKind.Relative);
		}

	}
}
