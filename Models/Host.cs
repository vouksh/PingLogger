using System;

namespace PingLogger.Models
{
	public class Host
	{
		public Guid Id { get; set; }
		public string HostName { get; set; }
		public string IP { get; set; }
		public int Threshold { get; set; } = 100;
		public int PacketSize { get; set; } = 32;
		public int Interval { get; set; } = 1000;
		public int Timeout { get; set; } = 1000;
		public bool DontFragment { get; set; } = true;
		// override object.Equals
		public override bool Equals(object obj)
		{
			//       
			// See the full list of guidelines at
			//   http://go.microsoft.com/fwlink/?LinkID=85237  
			// and also the guidance for operator== at
			//   http://go.microsoft.com/fwlink/?LinkId=85238
			//

			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}

			// TODO: write your implementation of Equals() here
			if((obj as Host).HostName == HostName || (obj as Host).IP == IP)
			{
				return true;
			}
			return base.Equals(obj);
		}

		// override object.GetHashCode
		public override int GetHashCode()
		{
			// TODO: write your implementation of GetHashCode() here

			return Tuple.Create(HostName, IP).GetHashCode();
		}
	}
}
