using Microsoft.Win32;
using PingLogger.Models;
using PingLogger.Workers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PingLogger
{
	public static class Util
	{
		static Controls.SplashScreen splashScreen;
		private static readonly string gitHubAPIToken = "9adeae2e5d78eb1bbc8e790e181f2e7b433216a7";
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
					File.Delete(installerPath + "/PingLogger-Setup.exe");
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
					httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes($"vouksh:{gitHubAPIToken}")));
					var resp = await httpClient.GetAsync("https://api.github.com/repos/vouksh/PingLogger/releases/latest");
					var strResp = await resp.Content.ReadAsStringAsync();
					Logger.Debug(strResp);
					if (strResp.Contains("API rate limit exceeded"))
					{
						Logger.Info("Unable to check for updates due to rate limit.");
						return;
					}
					var parsedResp = System.Text.Json.JsonSerializer.Deserialize<GitHubResponse>(strResp);
					var remoteVersion = Version.Parse(parsedResp.tag_name.Replace("v", ""));
					Logger.Info($"Most recent version is {remoteVersion}, currently running {localVersion}");
					if (remoteVersion > localVersion)
					{
						Logger.Info("Remote contains a newer version");
						File.WriteAllText("./ChangeLog.txt", parsedResp.body);
						if (Controls.UpdatePromptDialog.Show())
						{
							if (appIsInstalled)
							{
								string tempDir = RandomString(8);
								string savePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Temp\\{tempDir}";
								File.WriteAllText("./tempDir.txt", savePath);
								Logger.Info($"Downloading newest installer to {savePath}\\PingLogger-Setup.exe");
								Directory.CreateDirectory(savePath);
								var downloadURL = parsedResp.assets.First(a => a.name == "PingLogger-Setup.exe").browser_download_url;
								Logger.Info($"Downloading from {downloadURL}");
								using var downloader = new HttpClientDownloadWithProgress(downloadURL, savePath + "\\PingLogger-Setup.exe");
								splashScreen.mainLabel.Text = $"Downloading PingLogger setup v{remoteVersion}";
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
										Arguments = $"/SP- /VERYSILENT /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS /LOG=\"{AppContext.BaseDirectory}Logs\\Installer-v{remoteVersion}.log\""
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

		public static void SetTheme()
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
				App.Current.Resources.MergedDictionaries[0].Source = new Uri("/Themes/LightTheme.xaml", UriKind.Relative);
			}
			else
			{
				App.Current.Resources.MergedDictionaries[0].Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative);
			}
		}
	}
}
