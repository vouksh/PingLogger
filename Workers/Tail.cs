/*
 * This was taken from https://github.com/kerryjiang/Tailf
 * I take no credit for writing this. 
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PingLogger.Workers
{
	class Tail
	{
		readonly ManualResetEvent me;

		const string defaultLevel = "INFO";

		string currentLevel = defaultLevel;

		public class TailEventArgs : EventArgs
		{
			public string Level { get; set; }
			public string Line { get; set; }
		}

		long prevLen = -1;

		readonly string path;
		readonly int nLines;
		public string LineFilter { get; set; }

		public string LevelRegex { get; set; }

		Regex lineFilterRegex;
		Regex levelRegex;
		public Tail(string path, int nLines)
		{
			this.path = path;
			this.nLines = nLines;
			me = new ManualResetEvent(false);

		}
		bool requestForExit = false;
		public void Stop()
		{
			requestForExit = true;
			me.WaitOne();
		}
		public void Run()
		{
			if (!string.IsNullOrEmpty(LineFilter))
				lineFilterRegex = new Regex(LineFilter);

			if (!string.IsNullOrEmpty(LevelRegex))
				levelRegex = new Regex(LevelRegex, RegexOptions.Compiled | RegexOptions.Multiline);

			if (!File.Exists(path))
			{
				throw new FileNotFoundException("File does not exist:" + path);
			}
			FileInfo fi = new FileInfo(path);
			prevLen = fi.Length;
			MakeTail(nLines, path);
			ThreadPool.QueueUserWorkItem(new WaitCallback(q => ChangeLoop()));
		}

		private async void ChangeLoop()
		{
			while (!requestForExit)
			{
				await Fw_Changed();
				await Task.Delay(pollInterval);
			}
			me.Set();
		}
		static readonly int pollInterval = 100;
		static readonly int bufSize = 4096;
		string previous = string.Empty;
		async Task Fw_Changed()
		{
			FileInfo fi = new FileInfo(path);
			if (fi.Exists)
			{
				if (fi.Length != prevLen)
				{
					if (fi.Length < prevLen)
					{
						//assume truncated!
						prevLen = 0;
					}
					using var stream = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite);
					stream.Seek(prevLen, SeekOrigin.Begin);
					if (string.IsNullOrEmpty(LineFilter))
					{
						using StreamReader sr = new StreamReader(stream);
						var all = await sr.ReadToEndAsync();
						var lines = all.Split('\n');

						var lastIndex = lines.Length - 1;

						for (var i = 0; i < lines.Length; i++)
						{
							var line = lines[i].TrimEnd('\r');

							if (i != lastIndex)
								OnChanged(line + Environment.NewLine);
							else
								OnChanged(line);
						}
					}
					else
					{
						char[] buffer = new char[bufSize];
						StringBuilder current = new StringBuilder();
						using StreamReader sr = new StreamReader(stream);
						int nRead;
						do
						{
							nRead = sr.ReadBlock(buffer, 0, bufSize);
							for (int i = 0; i < nRead; ++i)
							{
								if (buffer[i] == '\n' || buffer[i] == '\r')
								{
									if (current.Length > 0)
									{
										string line = string.Concat(previous, current);

										if (lineFilterRegex.IsMatch(line))
										{
											OnChanged(string.Concat(line, Environment.NewLine));
										}
									}
									current = new StringBuilder();
								}
								else
								{
									current.Append(buffer[i]);
								}
							}
						} while (nRead > 0);
						if (current.Length > 0)
						{
							previous = current.ToString();
						}
					}
				}
				prevLen = fi.Length;
			}

		}
		public event EventHandler<TailEventArgs> Changed;
		private async void MakeTail(int nLines, string path)
		{
			List<string> lines = new List<string>();
			using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
			using (StreamReader sr = new StreamReader(stream))
			{
				string line;
				while (null != (line = await sr.ReadLineAsync()))
				{
					if (!string.IsNullOrEmpty(LineFilter))
					{
						if (lineFilterRegex.IsMatch(line))
						{
							EnqueueLine(nLines, lines, line);
						}
					}
					else
					{
						EnqueueLine(nLines, lines, line);
					}
				}
			}
			lines.Add(Environment.NewLine);
			foreach (var l in lines)
			{
				OnChanged(l);
			}

		}

		private static void EnqueueLine(int nLines, List<string> lines, string line)
		{
			if (nLines > 0 && lines.Count >= nLines)
			{
				lines.RemoveAt(0);
			}
			lines.Add(string.Concat(Environment.NewLine, line));
		}

		private void OnChanged(string l)
		{
			if (null == Changed)
				return;

			if (null == levelRegex)
			{
				Changed(this, new TailEventArgs() { Line = l, Level = currentLevel });
				return;
			}

			var match = levelRegex.Match(l);

			if (null == match || !match.Success)
			{
				Changed(this, new TailEventArgs() { Line = l, Level = currentLevel });
				return;
			}

			currentLevel = match.Groups["level"].Value;

			Changed(this, new TailEventArgs() { Line = l, Level = currentLevel });
		}
	}
}
