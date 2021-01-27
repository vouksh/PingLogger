using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PingLogger.ViewModels
{
	public class MessageBoxViewModel : ViewModelBase
	{
		public string DialogTitle { get; set; } = "Dialog";
		public string Message { get; set; } = "Bad things happened!";
		public bool OkOnly { get; set; }
		public int OkColumnSpan { get; set; } = 1;
		public bool CancelVisible { get; set; } = true;

	}
}
