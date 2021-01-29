using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PingLogger.Extensions
{
	public static class VersionExt
	{
		public static string ToShortString(this Version version)
		{
			return $"{version.Major}.{version.Minor}.{version.Build}";
		}
	}
}
