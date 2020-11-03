using System;
using System.Collections.Generic;
using System.Text;

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
			return $"{Major}.{Minor}.{Revision}.{Build}";
		}

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType() || obj.GetType() != typeof(Version))
			{
				return false;
			}

			if(obj as SerializableVersion == this || obj as Version == this)
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
			if (a.Major == b.Major && a.Minor == b.Minor && a.Build == b.Build && a.Revision == b.Revision)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static bool operator !=(Version a, SerializableVersion b)
		{
			if (a.Major != b.Major || a.Minor != b.Minor || a.Build != b.Build || a.Revision != b.Revision)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static bool operator ==(SerializableVersion a, Version b)
		{
			if(a.Major == b.Major && a.Minor == b.Minor && a.Build == b.Build && a.Revision == b.Revision)
			{
				return true;
			} else
			{
				return false;
			}
		}

		public static bool operator !=(SerializableVersion a, Version b)
		{
			if (a.Major != b.Major || a.Minor != b.Minor || a.Build != b.Build || a.Revision != b.Revision)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static bool operator >(SerializableVersion a, Version b)
		{
			if (a.Major > b.Major)
			{
				return true;
			}
			if(a.Major == b.Major && a.Minor > b.Minor)
			{
				return true;
			}
			if(a.Major == b.Major && a.Minor == b.Minor && a.Revision > b.Revision)
			{
				return true;
			}
			if(a.Major == b.Major && a.Minor == b.Minor && a.Revision == b.Revision && a.Build > b.Build)
			{
				return true;
			}
			return false;
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
			if (a.Major == b.Major && a.Minor == b.Minor && a.Revision < b.Revision)
			{
				return true;
			}
			if (a.Major == b.Major && a.Minor == b.Minor && a.Revision == b.Revision && a.Build < b.Build)
			{
				return true;
			}
			return false;
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
			if (a.Major == b.Major && a.Minor == b.Minor && a.Revision > b.Revision)
			{
				return true;
			}
			if (a.Major == b.Major && a.Minor == b.Minor && a.Revision == b.Revision && a.Build > b.Build)
			{
				return true;
			}
			return false;
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
			if (a.Major == b.Major && a.Minor == b.Minor && a.Revision < b.Revision)
			{
				return true;
			}
			if (a.Major == b.Major && a.Minor == b.Minor && a.Revision == b.Revision && a.Build < b.Build)
			{
				return true;
			}
			return false;
		}
	}
}
