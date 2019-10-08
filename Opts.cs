using System.Collections.Generic;

namespace PingLogger
{
	public class Opts
	{
		public List<Host> Hosts { get; set; }
		public bool AllSilent { get; set; } = false;
		public string SilentOutput { get; set; } = 
			"...............................................................................\r\n.....xxxxxxxxxxxxx.....xxxxxxxxxxxxxx.....xxxxxxxxxxxxxx.....xxxxxxxxxxxxx.....\r\n.....xxxxxxxxxxxxx.....xxxxxxxxxxxxxx.....xxxxxxxxxxxxxx.....xxxxxxxxxxxxx.....\r\n.........xxxxx.........xxxxx..............xxxxx..................xxxxx.........\r\n.........xxxxx.........xxxxxxxxxxxxxx.....xxxxxxxxxxxxxx.........xxxxx.........\r\n.........xxxxx.........xxxxx.......................xxxxx.........xxxxx.........\r\n.........xxxxx.........xxxxxxxxxxxxxx.....xxxxxxxxxxxxxx.........xxxxx.........\r\n.........xxxxx.........xxxxxxxxxxxxxx.....xxxxxxxxxxxxxx.........xxxxx.........\r\n...............................................................................";
		public System.ConsoleColor OutputColor { get; set; } = System.ConsoleColor.White;
	}
}