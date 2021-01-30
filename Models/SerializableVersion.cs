using System;

namespace PingLogger.Models
{
	public class SerializableVersion
	{
		public int Major { get; set; } = 0;
		public int Minor { get; set; } = 0;
		public int Revision { get; set; } = 0;
		public int Build { get; set; } = 0;

		public override string ToString()
		{
			return $"{Major}.{Minor}.{Build}.{Revision}";
		}

		public static SerializableVersion GetAppVersion()
		{
			var localVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			return new SerializableVersion()
			{
				Major = localVer.Major,
				Minor = localVer.Minor,
				Revision = localVer.Revision,
				Build = localVer.Build
			};
		}

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType() || obj.GetType() != typeof(Version))
			{
				return false;
			}

			if (ReferenceEquals(obj as SerializableVersion, this) || obj as Version == this)
			{
				return true;
			}

			return base.Equals(obj);
		}

		// override object.GetHashCode
		public override int GetHashCode()
		{

			return (Major + Minor + Revision + Build).GetHashCode();
		}

		public static bool operator ==(Version a, SerializableVersion b)
		{
			return b is not null && a is not null && a.Major == b.Major && a.Minor == b.Minor && a.Build == b.Build && a.Revision == b.Revision;
		}

		public static bool operator !=(Version a, SerializableVersion b)
		{
			return b is not null && a is not null && (a.Major != b.Major || a.Minor != b.Minor || a.Build != b.Build || a.Revision != b.Revision);
		}

		public static bool operator ==(SerializableVersion a, Version b)
		{
			return b is not null && a is not null && a.Major == b.Major && a.Minor == b.Minor && a.Build == b.Build && a.Revision == b.Revision;
		}

		public static bool operator !=(SerializableVersion a, Version b)
		{
			return b is not null && a is not null && (a.Major != b.Major || a.Minor != b.Minor || a.Build != b.Build || a.Revision != b.Revision);
		}

		public static bool operator >(SerializableVersion a, Version b)
		{
			if (a.Major > b.Major)
			{
				return true;
			}
			if (a.Major == b.Major && a.Minor > b.Minor)
			{
				return true;
			}
			if (a.Major == b.Major && a.Minor == b.Minor && a.Build > b.Build)
			{
				return true;
			}
			return a.Major == b.Major && a.Minor == b.Minor && a.Build == b.Build && a.Revision > b.Revision;
		}
		public static bool operator <(SerializableVersion a, Version b)
		{
			if (a.Major < b.Major)
			{
				return true;
			}
			if (a.Major == b.Major && a.Minor < b.Minor)
			{
				return true;
			}
			if (a.Major == b.Major && a.Minor == b.Minor && a.Build < b.Build)
			{
				return true;
			}
			return a.Major == b.Major && a.Minor == b.Minor && a.Build == b.Build && a.Revision < b.Revision;
		}

		public static bool operator >(Version a, SerializableVersion b)
		{
			if (a.Major > b.Major)
			{
				return true;
			}
			if (a.Major == b.Major && a.Minor > b.Minor)
			{
				return true;
			}
			if (a.Major == b.Major && a.Minor == b.Minor && a.Build > b.Build)
			{
				return true;
			}
			return a.Major == b.Major && a.Minor == b.Minor && a.Build == b.Build && a.Revision > b.Revision;
		}
		public static bool operator <(Version a, SerializableVersion b)
		{
			if (a.Major < b.Major)
			{
				return true;
			}
			if (a.Major == b.Major && a.Minor < b.Minor)
			{
				return true;
			}
			if (a.Major == b.Major && a.Minor == b.Minor && a.Build < b.Build)
			{
				return true;
			}
			return a.Major == b.Major && a.Minor == b.Minor && a.Build == b.Build && a.Revision < b.Revision;
		}
	}
}
