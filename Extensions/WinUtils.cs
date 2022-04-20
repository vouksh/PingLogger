#if Windows
using Microsoft.Win32;
#endif
using PingLogger.Models;
using PingLogger.Workers;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using PingLogger.Extensions;

namespace PingLogger
{
	public static partial class Utils
	{
		public static class Win
		{

			static Views.SplashScreen SplashScreen;
			static ViewModels.SplashScreenViewModel SplashScreenViewModel;

			public static bool AppIsClickOnce => File.Exists(AppContext.BaseDirectory + "Launcher.exe") && File.Exists(AppContext.BaseDirectory + "Launcher.manifest");
			
			public static async Task<bool> CheckForUpdates()
			{
#if DEBUG
				File.WriteAllText("../../../Installer/latest.json",
					JsonSerializer.Serialize(SerializableVersion.GetAppVersion())
					);
#endif
				if (Config.AppWasUpdated)
				{
					Log.Information("Application was updated last time it ran, cleaning up.");
					if (File.Exists("./PingLogger-old.exe"))
					{
						File.Delete("./PingLogger-old.exe");
					}
					if (Config.LastTempDir != string.Empty && Directory.Exists(Config.LastTempDir))
					{
						File.Delete(Config.LastTempDir + "/PingLogger.Setup.exe");
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
						//var httpClient = new WebClient();
						bool downloadComplete = false;

						string serverUrl = "https://pinglogger.lexdysia.com/";
						var downloadClient = new DownloadClient($"{serverUrl}/latest.json", "./latest.json");
						downloadClient.FileDownloaded += (o, c) => { downloadComplete = c; };
						await downloadClient.DownloadFileTaskAsync();
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
										var downloadURL = $"{serverUrl}/{remoteVersion.Major}{remoteVersion.Minor}{remoteVersion.Build}/win/install/PingLogger.Setup.exe";
										Log.Information($"Downloading from {downloadURL}");
										using var downloader = new HttpClientDownloadWithProgress(downloadURL, Config.LastTempDir + "\\PingLogger.Setup.exe");
										SplashScreenViewModel.UpdateMessage = $"Downloading PingLogger setup v{remoteVersion}";
										downloader.ProgressChanged += Downloader_ProgressChanged;
										await downloader.StartDownload();
									}

									Config.AppWasUpdated = true;
									Log.Information("Uninstalling current version.");
									Process.Start(new ProcessStartInfo
									{
										FileName = $"{Config.LastTempDir}/PingLogger.Setup.exe",
										UseShellExecute = true,
										Arguments = "/SILENT /CLOSEAPPLICATIONS"
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
										string[] fileList =  { "libHarfBuzzSharp.dll", "libSkiaSharp.dll", "PingLogger.exe" };
										SplashScreenViewModel.UpdateMessage = $"Downloading PingLogger v{remoteVersion}";
										foreach (var file in fileList)
										{
											var downloadUrl = $"{serverUrl}{remoteVersion.Major}{remoteVersion.Minor}{remoteVersion.Build}/win/sf/{file}";
											Log.Information($"Downloading from {downloadUrl}");
											using var downloader = new HttpClientDownloadWithProgress(downloadUrl, $"./{file}");
											downloader.ProgressChanged += Downloader_ProgressChanged;
											await downloader.StartDownload();
										}
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

			public static bool GetLightMode()
			{
#if Windows
				int lightTheme = Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", 1));
				return lightTheme == 1;
#else
				return true;
#endif
			}

			public static void CreateShortcut()
			{
#if Windows
				string shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\PingLogger.lnk";
				if (!File.Exists(shortcutPath))
				{
					Log.Information("CreateStartupShortcut called");
					Log.Information($"Saving shortcut to {shortcutPath}");

					Create(shortcutPath,
						Environment.CurrentDirectory + "\\" + AppDomain.CurrentDomain.FriendlyName + ".exe",
						string.Empty,
						Environment.CurrentDirectory,
						"Startup shortcut for PingLogger",
						string.Empty,
						string.Empty
						);
				}
#endif
			}

			public static void DeleteShortcut()
			{
#if Windows
				File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\PingLogger.lnk");
#endif
			}

#if Windows
			private static readonly Type m_type = Type.GetTypeFromProgID("WScript.Shell");
			private static readonly object m_shell = Activator.CreateInstance(m_type);

			[ComImport, TypeLibType(0x1040), Guid("F935DC23-1CF0-11D0-ADB9-00C04FD58A0B")]
			private interface IWshShortcut
			{
				[DispId(0)]
				string FullName { [return: MarshalAs(UnmanagedType.BStr)] [DispId(0)] get; }
				[DispId(0x3e8)]
				string Arguments { [return: MarshalAs(UnmanagedType.BStr)] [DispId(0x3e8)] get; [param: In, MarshalAs(UnmanagedType.BStr)] [DispId(0x3e8)] set; }
				[DispId(0x3e9)]
				string Description { [return: MarshalAs(UnmanagedType.BStr)] [DispId(0x3e9)] get; [param: In, MarshalAs(UnmanagedType.BStr)] [DispId(0x3e9)] set; }
				[DispId(0x3ea)]
				string Hotkey { [return: MarshalAs(UnmanagedType.BStr)] [DispId(0x3ea)] get; [param: In, MarshalAs(UnmanagedType.BStr)] [DispId(0x3ea)] set; }
				[DispId(0x3eb)]
				string IconLocation { [return: MarshalAs(UnmanagedType.BStr)] [DispId(0x3eb)] get; [param: In, MarshalAs(UnmanagedType.BStr)] [DispId(0x3eb)] set; }
				[DispId(0x3ec)]
				string RelativePath { [param: In, MarshalAs(UnmanagedType.BStr)] [DispId(0x3ec)] set; }
				[DispId(0x3ed)]
				string TargetPath { [return: MarshalAs(UnmanagedType.BStr)] [DispId(0x3ed)] get; [param: In, MarshalAs(UnmanagedType.BStr)] [DispId(0x3ed)] set; }
				[DispId(0x3ee)]
				int WindowStyle { [DispId(0x3ee)] get; [param: In] [DispId(0x3ee)] set; }
				[DispId(0x3ef)]
				string WorkingDirectory { [return: MarshalAs(UnmanagedType.BStr)] [DispId(0x3ef)] get; [param: In, MarshalAs(UnmanagedType.BStr)] [DispId(0x3ef)] set; }
				[TypeLibFunc((short)0x40), DispId(0x7d0)]
				void Load([In, MarshalAs(UnmanagedType.BStr)] string PathLink);
				[DispId(0x7d1)]
				void Save();
			}

			public static void Create(string fileName, string targetPath, string arguments, string workingDirectory, string description, string hotkey, string iconPath)
			{
				IWshShortcut shortcut = (IWshShortcut)m_type.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, m_shell, new object[] { fileName });
				shortcut.Description = description;
				shortcut.Hotkey = hotkey;
				shortcut.TargetPath = targetPath;
				shortcut.WorkingDirectory = workingDirectory;
				shortcut.Arguments = arguments;
				if (!string.IsNullOrEmpty(iconPath))
					shortcut.IconLocation = iconPath;
				shortcut.Save();
			}
#endif
		}
	}
}
