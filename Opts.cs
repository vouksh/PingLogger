using System.Collections.Generic;

namespace PingLogger
{
	public class Opts
	{
		public List<Host> Hosts { get; set; }
		public bool AllSilent { get; set; } = false;
		public string SilentOutput { get; set; } = "Testing in progress. Press Ctrl-C for options or to exit.";
	}
}