using System;
using System.Collections.Generic;
using System.Text;

namespace PingLogger
{
	public static class ColoredOutput
	{
		public static void WriteLine(string output)
		{
			ParseText(output);
			Console.ResetColor();
			Console.WriteLine();
		}
		public static void Write(string output)
		{
			ParseText(output);
			Console.ResetColor();
		}
		public static void WriteMultiLine(string output)
		{
			foreach(var line in output.Split(Environment.NewLine))
			{
				ParseText(line);
				Console.WriteLine();
			}
			Console.ResetColor();
		}
		private static void ParseText(string input)
		{
			var splitStr = input.Split(' ', '\n');
			foreach (var str in splitStr)
			{
				if (str.StartsWith("##"))
				{
					Console.ForegroundColor = str.Remove(0, 2) switch
					{
						"blue" => ConsoleColor.Blue,
						"green" => ConsoleColor.Green,
						"red" => ConsoleColor.Red,
						"yellow" => ConsoleColor.Yellow,
						"white" => ConsoleColor.White,
						"darkblue" => ConsoleColor.DarkBlue,
						"cyan" => ConsoleColor.Cyan,
						"gray" => ConsoleColor.Gray,
						"darkcyan" => ConsoleColor.DarkCyan,
						"magenta" => ConsoleColor.Magenta,
						"darkmagenta" => ConsoleColor.DarkMagenta,
						"darkgray" => ConsoleColor.DarkGray,
						"black" => ConsoleColor.Black,
						"darkgreen" => ConsoleColor.DarkGreen,
						"darkred" => ConsoleColor.DarkRed,
						"darkyellow" => ConsoleColor.DarkYellow,
						_ => ConsoleColor.Gray
					};
				} else
				{
					Console.Write(str + " ");
				}
			}
		}
	}
}
