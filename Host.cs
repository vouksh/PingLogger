using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PingLogger
{
	public class Host
	{
		public string HostName { get; set; }
		public string IP { get; set; }
		public int Count { get; set; } = 0;
		public int Threshold { get; set; } = 500;
		public int PacketSize { get; set; } = 64;
		public bool WriteFile { get; set; } = false;
		public int Interval { get; set; } = 1000;
		public int Timeout { get; set; } = 10000;
	}
}
