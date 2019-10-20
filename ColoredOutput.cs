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
			foreach (var line in output.Split(Environment.NewLine))
			{
				ParseText(line);
				Console.WriteLine();
			}
			Console.ResetColor();
		}

		private static void ParseText(string input)
		{

			Console.ResetColor();
			var change = input;
			foreach (var tag in ColorTags)
			{
				change = change.Replace(tag, "\n" + tag + "\n");

			}
			var splitStr = change.Split("\n");
			foreach (var line in splitStr)
			{
				var colorLine = false;
				foreach (var color in ColorTags)
				{
					if (line.Contains(color))
					{
						Console.ForegroundColor = line.Replace('#', ' ').Trim() switch
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
						colorLine = true;
					}
				}
				if (!colorLine)
					Console.Write(line);
			}

		}
		private static string[] ColorTags =
			{
				"##blue##",
				"##green##",
				"##red##",
				"##yellow##",
				"##white##",
				"##darkblue##",
				"##cyan##",
				"##gray##",
				"##darkcyan##",
				"##magenta##",
				"##darkmagenta##",
				"##darkgray##",
				"##black##",
				"##darkgreen##",
				"##darkred##",
				"##darkyellow##",
				"###"
			};
	}
}
