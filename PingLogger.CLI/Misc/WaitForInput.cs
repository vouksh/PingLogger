using System;
using System.Threading;

namespace PingLogger.CLI.Misc
{
	//Found this on Stack Overflow. https://stackoverflow.com/a/18342182/1659361
	//Works well for what I need. 
	class WaitForInput
	{
		private readonly static Thread inputThread;
		private readonly static AutoResetEvent getInput, gotInput;
		private static string input;
		private static ConsoleKeyInfo keyInput;

		static WaitForInput()
		{
			getInput = new AutoResetEvent(false);
			gotInput = new AutoResetEvent(false);
			inputThread = new Thread(Reader)
			{
				IsBackground = true
			};
			inputThread.Start();
		}

		private static bool isKey = false;

		private static void Reader()
		{
			while (true)
			{
				getInput.WaitOne();

				if (!isKey)
					input = Console.ReadLine();
				else
					keyInput = Console.ReadKey(true);

				gotInput.Set();
			}
		}

		/// <summary>
		/// Read a console string input, with a timeout.
		/// </summary>
		/// <param name="timeOutMillisecs">Time to wait in milliseconds until giving up.</param>
		/// <returns>Text entered.</returns>
		public static string ReadLine(int timeOutMillisecs = Timeout.Infinite)
		{
			getInput.Set();
			bool success = gotInput.WaitOne(timeOutMillisecs);
			if (success)
				return input;
			else
			{
				SendKey.DoEnter();
				throw new TimeoutException("User did not provide input within the timelimit.");
			}
		}

		/// <summary>
		/// Read a console key input, with a timeout.
		/// </summary>
		/// <param name="timeOutMillisecs">Time to wait in milliseconds until giving up.</param>
		/// <returns>Key(s) entered.</returns>
		public static ConsoleKeyInfo ReadKey(int timeOutMillisecs = Timeout.Infinite)
		{
			isKey = true;
			getInput.Set();
			bool success = gotInput.WaitOne(timeOutMillisecs);
			if (success)
			{
				return keyInput;
			}
			else
			{
				SendKey.DoEnter();
				throw new TimeoutException("User did not provide input within the timelimit.");
			}
		}
	}
}
