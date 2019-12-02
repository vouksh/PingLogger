using System.Collections.Generic;

namespace PingLogger.Misc
{
	public class Opts
	{
		public List<Host> Hosts { get; set; }
		// So after thinking it over, I think it's best, for the use-case that I designed this program for, that defaulting to silent is best. 
		public bool AllSilent { get; set; } = true; 
		public string SilentOutput { get; set; } =
			"##red##...............................................................................\r\n##red##.....##white##xxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxx##red##.....\r\n##red##.....##white##xxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxx##red##.....\r\n##red##.........##white##xxxxx##red##.........##white##xxxxx##red##..............##white##xxxxx##red##..................##white##xxxxx##red##.........\r\n##red##.........##white##xxxxx##red##.........##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.........##white##xxxxx##red##.........\r\n##red##.........##white##xxxxx##red##.........##white##xxxxx##red##.......................##white##xxxxx##red##.........##white##xxxxx##red##.........\r\n##red##.........##white##xxxxx##red##.........##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.........##white##xxxxx##red##.........\r\n##red##.........##white##xxxxx##red##.........##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.........##white##xxxxx##red##.........\r\n##red##...............................................................................";
		public bool LoadOnStartup { get; set; } = false;
		public int DaysToKeepLogs { get; set; } = 7;
	}
}