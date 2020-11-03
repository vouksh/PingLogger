using System;
using System.Text.Json;

namespace PingLogger.Models
{
	public class Host
	{
		public Guid Id { get; set; }
		public string HostName { get; set; }
		public string IP { get; set; }
		public int Threshold { get; set; } = 100;
		public int PacketSize { get; set; } = 32;
		public int Interval { get; set; } = 1000;
		public int Timeout { get; set; } = 1000;
		public bool DontFragment { get; set; } = true;

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}

			if((obj as Host).HostName == HostName || (obj as Host).IP == IP)
			{
				return true;
			}
			return base.Equals(obj);
		}

		public static bool operator ==(Host a, Host b)
		{
			return a.HostName == b.HostName || a.IP == b.IP;
		}

		public static bool operator !=(Host a, Host b)
		{
			return a.HostName != b.HostName && a.IP != b.IP;
		}

		public static bool operator ==(Host a, string b)
		{
			return a.HostName == b || a.IP == b;
		}

		public static bool operator !=(Host a, string b)
		{
			return a.HostName != b && a.IP != b;
		}

		public override string ToString()
		{
			return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
		}

		public override int GetHashCode()
		{
			return Tuple.Create(HostName, IP).GetHashCode();
		}
	}
}
