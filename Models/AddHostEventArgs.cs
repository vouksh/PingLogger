using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PingLogger.Models
{
	public record AddHostEventArgs(string HostName, string IPAddress, bool IsValid);
}
