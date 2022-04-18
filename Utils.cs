﻿using PingLogger.Models;
using PingLogger.Workers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace PingLogger
{
	public static partial class Utils
	{

		public static void CreateShortcut()
		{
#if Windows
			Win.CreateShortcut();
#elif Linux
			Linux.CreateShortcut();
#elif OSX
			PingLogger.Views.MessageBox.ShowAsError("Error", "This option is not avaiable on MacOS");
#endif
		}

		public static void DeleteShortcut()
		{
#if Windows
			Win.DeleteShortcut();
#elif Linux
			Linux.DeleteShortcut();
#elif OSX
			PingLogger.Views.MessageBox.ShowAsError("Error", "This option is not avaiable on MacOS");
#endif
		}

		public static Dictionary<string, string> GetOSInfo()
		{
			var values = new Dictionary<string, string>();
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				values.Add("OperatingSystem", "Windows");
			}
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				values.Add("OperatingSystem", "Linux");
			}
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				values.Add("OperatingSystem", "Linux");
			}
			values.Add("OSVersion", Environment.OSVersion.VersionString);
			values.Add("CountryCode", RegionInfo.CurrentRegion.TwoLetterISORegionName);

			return values;
		}

		public static string FileBasePath
		{
			get
			{
#if Windows
				if (Win.AppIsClickOnce)
				{
					var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + "PingLogger";
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
#elif Linux
				return Linux.GetFileSavePath();
#else
				return Environment.CurrentDirectory;
#endif
			}
		}


		/// <summary>
		/// Generates a random string with the specified length.
		/// </summary>
		/// <param name="length">Number of characters in the string</param>
		/// <returns>Random string of letters and numbers</returns>
		public static string RandomString(int length)
		{
			Random random = new();
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
#if Windows
					return Win.GetLightMode();
#else
					return true;
#endif

				}
				else
				{
					return Config.Theme == Theme.Light;
				}
			}
		}
	}
}
