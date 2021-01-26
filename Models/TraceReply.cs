using System;
using System.Windows;

namespace PingLogger.Models
{
	public record TraceReply
	{
		public Guid Id { get; set; }
		public string HostName { get; set; }
		public string IPAddress { get; set; }
		public int Ttl { get; set; }
		public string Ping1 { get; set; }
		public string Ping2 { get; set; }
		public string Ping3 { get; set; }
		public bool HostAddButtonVisible { get; set; }
		public bool IPAddButtonVisible { get; set; }
	}
}
