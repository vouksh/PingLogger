using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PingLogger
{
	public static partial class Utils
	{
		public static class Linux
		{
			public static void CreateShortcut()
			{
#if Linux
				string fileContents = @$"[Desktop Entry]
Type=Application
Exec={AppDomain.CurrentDomain.FriendlyName}
Hidden=false
NoDisplay=false
X-GNOME-Autostart-enabled=true
Name[en_US]=PingLogger
Name=PingLogger
Comment[en_US]=
Comment=";
				File.WriteAllText("~/.config/autostart/PingLogger.desktop", fileContents);
#endif
			}

			public static void DeleteShortcut()
			{
#if Linux
				File.Delete("~/.config/autostart/PingLogger.desktop");
#endif
			}
		}
	}
}
