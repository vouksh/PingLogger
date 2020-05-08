using System;
using System.ComponentModel;

namespace PingLogger.GUI.Models
{
	public class Host
	{
		public Guid Id { get; set; }
		public string HostName { get; set; }
		public string IP { get; set; }
		public int Threshold { get; set; } = 500;
		public int PacketSize { get; set; } = 32;
		public int Interval { get; set; } = 1000;
		public int Timeout { get; set; } = 2000;
		public bool DontFragment { get; set; } = true;
	}
}
