namespace PingLogger
{
	public class Host
	{
		public string HostName { get; set; }
		public string IP { get; set; }
		public int Threshold { get; set; } = 500;
		public int PacketSize { get; set; } = 64;
		public int Interval { get; set; } = 1000;
		public int Timeout { get; set; } = 10000;
		public bool Silent { get; set; } = false;
	}
}
