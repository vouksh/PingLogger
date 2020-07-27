using System.Windows;

namespace PingLogger.GUI.Models
{
	public class TraceReply
	{
		public string IPAddress { get; set; }
		public int Ttl { get; set; }
		public long RoundTrip { get; set; }
		public string[] PingTimes { get; set; }
		public string HostName { get; set; }
		public Visibility HostAddButtonVisible { get; set; }
		public Visibility IPAddButtonVisible { get; set; }
	}
}
