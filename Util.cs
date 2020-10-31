using Microsoft.Win32;
using PingLogger.Models;
using PingLogger.Workers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace PingLogger
{
	public static class Util
	{
		static Controls.SplashScreen splashScreen;
		public static async Task CheckForUpdates()
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
					File.Delete(installerPath + "latest.json");
					File.Delete(installerPath + "/PingLogger-Setup.msi");
					Directory.Delete(installerPath);
					File.Delete("./tempDir.txt");
				}
				Config.AppWasUpdated = false;
			}
			else
			{
				if (Config.UpdateLastChecked.Date >= DateTime.Today)
				{
					Logger.Info("Application already checked for update today, skipping.");
					return;
				}
				splashScreen = new Controls.SplashScreen();
				splashScreen.Show();
				splashScreen.dlProgress.IsIndeterminate = true;
				splashScreen.dlProgress.Value = 1;
				var localVersion = Assembly.GetExecutingAssembly().GetName().Version;
				File.WriteAllText("./version.json", JsonSerializer.Serialize(localVersion));
				var appGUID = Assembly.GetExecutingAssembly().GetCustomAttribute<GuidAttribute>().Value.ToUpper();
				bool appIsInstalled = false;
				string installerGUID = $"{{{appGUID}}}";
				var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
				if (key.GetSubKeyNames().Contains(installerGUID))
				{
					appIsInstalled = true;
					Logger.Info($"Application was installed in {AppContext.BaseDirectory}.");
				}
				try
				{
					string tempDir = RandomString(8);
					string savePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Temp\\{tempDir}";
					File.WriteAllText("./tempDir.txt", savePath);
					Directory.CreateDirectory(savePath);
					Logger.Info($"Creating temporary path {savePath}");

					var httpClient = new WebClient();

					string azureURL = "https://pingloggerfiles.blob.core.windows.net/";
					await httpClient.DownloadFileTaskAsync($"{azureURL}version/latest.json", $"{savePath}/latest.json");
					var remoteVersion = JsonSerializer.Deserialize<Version>(File.ReadAllText($"{savePath}/latest.json"));

					Logger.Info($"Most recent version is {remoteVersion}, currently running {localVersion}");
					if (remoteVersion > localVersion)
					{
						Logger.Info("Remote contains a newer version");
						if (Controls.UpdatePromptDialog.Show())
						{
							if (appIsInstalled)
							{
								Logger.Info($"Downloading newest installer to {savePath}\\PingLogger-Setup.msi");
								var downloadURL = $"{azureURL}v{remoteVersion.Major}{remoteVersion.Minor}{remoteVersion.Build}/PingLogger-Setup.msi";
								Logger.Info($"Downloading from {downloadURL}");
								using var downloader = new HttpClientDownloadWithProgress(downloadURL, savePath + "\\PingLogger-Setup.msi");
								splashScreen.mainLabel.Text = $"Downloading PingLogger setup v{remoteVersion}";
								downloader.ProgressChanged += Downloader_ProgressChanged;
								await downloader.StartDownload();
								Config.AppWasUpdated = true;
								Logger.Info("Running installer");
								new Process
								{
									StartInfo = new ProcessStartInfo
									{
										FileName = "msiexec.exe",
										UseShellExecute = true,
										Arguments = $"/q /l* \"{AppContext.BaseDirectory}Logs\\Installer-v{remoteVersion}.log\" /i {savePath}\\PingLogger-Setup.msi"
									}
								}.Start();
								Logger.Info("Installer completed, closing.");
								Environment.Exit(0);
							}
							else
							{
								Logger.Info("Renamed PingLogger.exe to PingLogger-old.exe");
								File.Move("./PingLogger.exe", "./PingLogger-old.exe");
								Logger.Info("Downloading new PingLogger.exe");
								var downloadURL = $"{azureURL}v{remoteVersion.Major}{remoteVersion.Minor}{remoteVersion.Build}/PingLogger.exe";
								Logger.Info($"Downloading from {downloadURL}");
								using var downloader = new HttpClientDownloadWithProgress(downloadURL, "./PingLogger.exe");
								splashScreen.mainLabel.Text = $"Downloading PingLogger v{remoteVersion}";
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
					return;
				}
			}
			Config.UpdateLastChecked = DateTime.Now;
			return;
		}

		private static void Downloader_ProgressChanged(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
		{
			if (progressPercentage.HasValue && totalFileSize.HasValue)
			{
				splashScreen.dlProgress.Maximum = 100;
				splashScreen.dlProgress.Value = progressPercentage.Value;
				splashScreen.dlProgress.IsIndeterminate = false;
			}
		}

		public static void CloseSplashScreen()
		{
			try
			{
				splashScreen?.Close();
				splashScreen = null;
			}
			catch (NullReferenceException)
			{
				Logger.Debug("splashScreen was null.");
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

		public static bool IsLightTheme()
		{
			if (Config.Theme == Theme.Auto)
			{
				int lightTheme = Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", 1));
				if (lightTheme == 1)
					return true;
				else
					return false;
			}
			else
			{
				return Config.Theme == Theme.Light;
			}
		}

		public static void SetTheme()
		{
			if (IsLightTheme())
			{
				App.Current.Resources.MergedDictionaries[0].Source = new Uri("/Themes/LightTheme.xaml", UriKind.Relative);
			}
			else
			{
				App.Current.Resources.MergedDictionaries[0].Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative);
			}
		}
	}
}
