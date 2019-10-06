﻿using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace PingLogger
{
	//Needed to adapt the WaitForInput class so that I could use ReadKey() instead of ReadLine()
	//Functionally identical to the other class. 
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

		/// <summary>
		/// Read a console key input, with a timeout.
		/// </summary>
		/// <param name="timeOutMillisecs">Time to wait in milliseconds until giving up.</param>
		/// <returns>Key(s) entered.</returns>
		public static ConsoleKeyInfo ReadKey(int timeOutMillisecs = Timeout.Infinite)
		{
			getInput.Set();
			bool success = gotInput.WaitOne(timeOutMillisecs);
			if (success)
				return input;
			else
			{
				//Ran into a problem where, if the input wasn't received, the console was technically still waiting for an input.
				//So I found https://stackoverflow.com/a/9634477/1659361
				//So now when it times out, it sends an enter key to this window. 
				var hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
				PostMessage(hWnd, WM_KEYDOWN, VK_RETURN, 0);
				throw new TimeoutException("User did not provide input within the timelimit.");
			}
		}
	}
}
