using System;

namespace PingLogger.Models
{
	public record Reply
	{
		public Host Host { get; set; }
		public int? Ttl { get; set; } = 0;
		public long RoundTrip { get; set; } = 0;
		public DateTime DateTime { get; set; }
		public bool TimedOut { get; set; } = false;
		public bool? Succeeded { get; set; }
	}
}
