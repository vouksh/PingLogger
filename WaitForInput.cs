using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PingLogger
{
	class WaitForInput
	{
		private static Thread inputThread;
		private static AutoResetEvent getInput, gotInput;
		private static string input;

		static WaitForInput()
		{
			getInput = new AutoResetEvent(false);
			gotInput = new AutoResetEvent(false);
			inputThread = new Thread(reader);
			inputThread.IsBackground = true;
			inputThread.Start();
		}

		private static void reader()
		{
			while (true)
			{
				getInput.WaitOne();
				input = Console.ReadLine();
				gotInput.Set();
			}
		}

		// omit the parameter to read a line without a timeout
		public static string ReadLine(int timeOutMillisecs = Timeout.Infinite)
		{
			getInput.Set();
			bool success = gotInput.WaitOne(timeOutMillisecs);
			if (success)
				return input;
			else
				throw new TimeoutException("User did not provide input within the timelimit.");
		}
	}
}
