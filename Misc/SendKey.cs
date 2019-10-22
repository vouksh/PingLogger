using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PingLogger.Misc
{
	public static class SendKey
	{
		[DllImport("User32.Dll", EntryPoint = "PostMessageA")]
		private static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

		const int VK_RETURN = 0x0D;
		const int WM_KEYDOWN = 0x100;

		public static void DoEnter()
		{
			var hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
			PostMessage(hWnd, WM_KEYDOWN, VK_RETURN, 0);
		}
	}
}
