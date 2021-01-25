using System;
using System.Text.Json;

namespace PingLogger.Models
{
	[Serializable]
	public class Host
	{
		public Guid Id { get; set; }
		public string HostName { get; set; } = string.Empty;
		public string IP { get; set; } = string.Empty;
		public int Threshold { get; set; } = 100;
		public int PacketSize { get; set; } = 32;
		public int Interval { get; set; } = 1000;
		public int Timeout { get; set; } = 1000;

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}

			if(((Host) obj).HostName == HostName || ((Host) obj).IP == IP)
			{
				return true;
			}
			return base.Equals(obj);
		}

		public static bool operator ==(Host a, Host b)
		{
			return a?.HostName == b?.HostName || a?.IP == b?.IP;
		}

		public static bool operator !=(Host a, Host b)
		{
			return a?.HostName != b?.HostName && a?.IP != b?.IP;
		}

		public static bool operator ==(Host a, string b)
		{
			return a?.HostName == b || a?.IP == b;
		}

		public static bool operator !=(Host a, string b)
		{
			return a?.HostName != b && a?.IP != b;
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
