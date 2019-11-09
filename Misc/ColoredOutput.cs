using System;
using System.Text;
using System.Threading;

namespace PingLogger.Misc
{
	public static class ColoredOutput
	{
		/// <summary>
		/// Adds a timestamp to the beginning of the input line.
		/// Format: [<paramref name="format"/>] <paramref name="input"/>
		/// </summary>
		/// <param name="input">String to append timestamp to</param>
		/// <param name="format">Format of the output, defaults to HH:mm:ss</param>
		/// <param name="color">Color of the timestamp, defaults to darkgray</param>
		/// <returns>String with timestamp</returns>
		private static string AddTimestamp(string input, string format = "HH:mm:ss", string color = "darkgray")
		{
			var builder = new StringBuilder();
			builder.Append("##");
			builder.Append(color);
			builder.Append("##[###");
			builder.Append(DateTime.Now.ToString(format));
			builder.Append("##");
			builder.Append(color);
			builder.Append("##]### ");
			builder.Append(input);

			return builder.ToString();
		}
		/// <summary>
		/// Use in place of Console.WriteLine(), use tags to color text.
		/// </summary>
		/// <param name="output">String with text to output to console, replaces color tags with colors.</param>
		/// <param name="doTimeStamp">If true, adds timestamp to beginning of line</param>
		public static void WriteLine(string output, bool doTimeStamp = false)
		{
			if(doTimeStamp)
			{
				output = AddTimestamp(output);
			}
			ParseText(output);
			Console.ResetColor();
			Console.WriteLine();
		}
		/// <summary>
		/// Use in place of Console.Write(), use tags to color text.
		/// </summary>
		/// <param name="output">String with text to output to console, replaces color tags with colors.</param>
		/// <param name="doTimeStamp">If true, adds timestamp to beginning of line</param>
		public static void Write(string output, bool doTimeStamp = false)
		{
			if (doTimeStamp)
			{
				output = AddTimestamp(output);
			}
			ParseText(output);
			Console.ResetColor();
		}
		/// <summary>
		/// Used if you have a multi-line input, like from a text file, and formats each line individually.
		/// </summary>
		/// <param name="output">String with text to output to console, replaces color tags with colors.</param>
		/// <param name="doTimeStamp">If true, adds timestamp to beginning of each line</param>
		public static void WriteMultiLine(string output, bool doTimeStamp = false)
		{
			foreach (var line in output.Split(Environment.NewLine))
			{
				string lineOut = line;
				if (doTimeStamp)
				{
					lineOut = AddTimestamp(line);
				}
				ParseText(lineOut);
				Console.WriteLine();
			}
			Console.ResetColor();
		}

		/// <summary>
		/// Parses the input and assigns colors according to the tags in the input
		/// Outputs directly to console. 
		/// </summary>
		/// <param name="input">Text to parse</param>
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
				//Initially, we assume the line is not containing a color.
				var colorLine = false;
				//Parse line through each tag.
				foreach (var color in ColorTags)
				{
					//Line contains a tag.
					if (line.ToLower().Contains(color))
					{
						//Use pattern matching to determine which color to set. 
						Console.ForegroundColor = line.Replace('#', ' ').Trim().ToLower() switch
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
