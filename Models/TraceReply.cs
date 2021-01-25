using System;
using System.Windows;

namespace PingLogger.Models
{
	public record TraceReply
	{
		public Guid Id { get; set; }
		public string IPAddress { get; set; }
		public int Ttl { get; set; }
		public string[] PingTimes { get; set; }
		public string HostName { get; set; }
		public Visibility HostAddButtonVisible { get; set; }
		public Visibility IPAddButtonVisible { get; set; }
	}
}
