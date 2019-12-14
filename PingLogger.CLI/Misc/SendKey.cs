using System;
using System.Runtime.InteropServices;

namespace PingLogger.CLI.Misc
{
	//Ran into a problem where, if the input wasn't received, the console was technically still waiting for an input.
	//So I found https://stackoverflow.com/a/9634477/1659361
	//So now when it times out, it sends an enter key to this window.
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
