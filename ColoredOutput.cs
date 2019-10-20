using System;
using System.Collections.Generic;
using System.Text;

namespace PingLogger
{
	public static class ColoredOutput
	{
		/// <summary>
		/// Use in place of Console.WriteLine(), use tags to color text.
		/// </summary>
		/// <param name="output">String with text to output to console, replaces color tags with colors.</param>
		public static void WriteLine(string output)
		{
			ParseText(output);
			Console.ResetColor();
			Console.WriteLine();
		}
		/// <summary>
		/// Use in place of Console.Write(), use tags to color text.
		/// </summary>
		/// <param name="output">String with text to output to console, replaces color tags with colors.</param>
		public static void Write(string output)
		{
			ParseText(output);
			Console.ResetColor();
		}
		/// <summary>
		/// Used if you have a multi-line input, like from a text file, and formats each line individually.
		/// </summary>
		/// <param name="output">String with text to output to console, replaces color tags with colors.</param>
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
			//Reset the color back to default for this input. 
			Console.ResetColor();
			var change = input;
			foreach (var tag in ColorTags)
			{
				//Surround each tag with a new line so that we can parse it correctly.
				change = change.Replace(tag, "\n" + tag + "\n");
			}
			//Split the string by new line
			var splitStr = change.Split("\n");
			foreach (var line in splitStr)
			{
				//Intially, we assume the line is not containing a color.
				var colorLine = false;
				//Parse line through each tag.
				foreach (var color in ColorTags)
				{
					//Line contains a tag.
					if (line.Contains(color))
					{
						//Use pattern matching to determine which color to set. 
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
						//Line contained a color. Set to true, and break out of the foreach loop.
						colorLine = true;
						break;
					}
				}
				//Line isn't a color, so we'll print out the text.
				if (!colorLine)
					Console.Write(line);
			}

		}
		/// <summary>
		/// Each of these is a valid color tag, with ### resetting the color back to gray.
		/// </summary>
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
