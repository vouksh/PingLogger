using System;
using System.Collections.Generic;
using System.Text;

namespace PingLogger.GUI.Models
{
	public class TraceReply
	{
		public string IPAddress { get; set; }
		public int Ttl { get; set; }
		public long RoundTrip { get; set; }
		public long[] PingTimes { get; set; }
	}
}
