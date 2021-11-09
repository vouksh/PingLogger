using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
#if Linux
using Mono.Posix;
using Mono.Unix;
#endif

namespace PingLogger
{
	public static partial class Utils
	{
		public static class Linux
		{
			public static void CreateShortcut()
			{
#if Linux
				string shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.config/autostart/PingLogger.desktop";
				string fileContents = @$"[Desktop Entry]
Type=Application
Path={AppDomain.CurrentDomain.BaseDirectory}
Exec={AppDomain.CurrentDomain.BaseDirectory}{AppDomain.CurrentDomain.FriendlyName}
Hidden=false
NoDisplay=false
X-GNOME-Autostart-enabled=true
Name[en_US]=PingLogger
Name=PingLogger
Comment[en_US]=
Comment=";
				if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.config/autostart"))
					Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.config/autostart");
				File.WriteAllText(shortcutPath, fileContents);
				var fileInfo = new UnixFileInfo(shortcutPath)
				{
					FileAccessPermissions = FileAccessPermissions.UserReadWriteExecute |
											FileAccessPermissions.GroupRead |
											FileAccessPermissions.OtherRead
				};
				fileInfo.Refresh();
#endif
			}

			public static void DeleteShortcut()
			{
#if Linux
				File.Delete("~/.config/autostart/PingLogger.desktop");
#endif
			}

			public static string GetFileSavePath()
			{
				// Check to see if we're running outside of the user directory. 
				// If we are, save the config and logs to the user directory, otherwise, keep it where it is. 
				if (!Environment.CurrentDirectory.Contains(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
				{
					var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Path.DirectorySeparatorChar + ".pinglogger";
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
	}
}
