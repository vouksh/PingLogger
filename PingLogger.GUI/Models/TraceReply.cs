namespace PingLogger.GUI.Models
{
	public class TraceReply
	{
		public string IPAddress { get; set; }
		public int Ttl { get; set; }
		public long RoundTrip { get; set; }
		public string[] PingTimes { get; set; }
	}
}
