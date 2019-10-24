using System.Collections.Generic;

namespace PingLogger.Misc
{
	public class Opts
	{
		public List<Host> Hosts { get; set; }
		public bool AllSilent { get; set; } = false;
		public string SilentOutput { get; set; } =
			"##red##...............................................................................\r\n##red##.....##white##xxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxx##red##.....\r\n##red##.....##white##xxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxx##red##.....\r\n##red##.........##white##xxxxx##red##.........##white##xxxxx##red##..............##white##xxxxx##red##..................##white##xxxxx##red##.........\r\n##red##.........##white##xxxxx##red##.........##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.........##white##xxxxx##red##.........\r\n##red##.........##white##xxxxx##red##.........##white##xxxxx##red##.......................##white##xxxxx##red##.........##white##xxxxx##red##.........\r\n##red##.........##white##xxxxx##red##.........##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.........##white##xxxxx##red##.........\r\n##red##.........##white##xxxxx##red##.........##white##xxxxxxxxxxxxxx##red##.....##white##xxxxxxxxxxxxxx##red##.........##white##xxxxx##red##.........\r\n##red##...............................................................................";
		public bool LoadOnStartup { get; set; } = false;
	}
}