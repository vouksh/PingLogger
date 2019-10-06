using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace PingLogger
{
	class WaitForInputKey
	{
		[DllImport("User32.Dll", EntryPoint = "PostMessageA")]
		private static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

		const int VK_RETURN = 0x0D;
		const int WM_KEYDOWN = 0x100;

		private readonly static Thread inputThread;
		private readonly static AutoResetEvent getInput, gotInput;
		private static ConsoleKeyInfo input;

		static WaitForInputKey()
		{
			getInput = new AutoResetEvent(false);
			gotInput = new AutoResetEvent(false);
			inputThread = new Thread(Reader)
			{
				IsBackground = true
			};
			inputThread.Start();
		}

		private static void Reader()
		{
			while (true)
			{
				getInput.WaitOne();
				input = Console.ReadKey();
				gotInput.Set();
			}
		}
		public static ConsoleKeyInfo ReadKey(int timeOutMillisecs = Timeout.Infinite)
		{
			getInput.Set();
			bool success = gotInput.WaitOne(timeOutMillisecs);
			if (success)
				return input;
			else
			{
				var hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
				PostMessage(hWnd, WM_KEYDOWN, VK_RETURN, 0);
				throw new TimeoutException("User did not provide input within the timelimit.");
			}
		}
	}
}
