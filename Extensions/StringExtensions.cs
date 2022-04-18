using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PingLogger.Extensions;

public static class StringExtensions
{
	public static string SplitCamelCase(this string input, string delimeter = " ")
	{
		return input.Any(char.IsUpper) ? string.Join(delimeter, Regex.Split(input, "(?<!^)(?=[A-Z])")) : input;
	}

}