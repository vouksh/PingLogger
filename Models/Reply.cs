using System;

namespace PingLogger.GUI.Models
{
	public class Reply
	{
		public Host Host { get; set; }
		public int? Ttl { get; set; } = 0;
		public long RoundTrip { get; set; } = 0;
		public DateTime DateTime { get; set; }
		public bool TimedOut { get; set; } = false;
		public bool? Succeeded { get; set; }
	}
}
