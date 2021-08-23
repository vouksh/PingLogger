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
	internal class Tail
	{
		private readonly ManualResetEvent _me;

		const string _defaultLevel = "INFO";

		string _currentLevel = _defaultLevel;

		public class TailEventArgs : EventArgs
		{
			public string Level { get; set; }
			public string Line { get; set; }
		}

		long _prevLen = -1;

		private readonly string _path;
		private readonly int _nLines;
		public string LineFilter { get; set; }

		public string LevelRegex { get; set; }

		Regex _lineFilterRegex;
		Regex _levelRegex;
		public Tail(string path, int nLines)
		{
			this._path = path;
			this._nLines = nLines;
			_me = new ManualResetEvent(false);

		}
		bool _requestForExit;
		public void Stop()
		{
			_requestForExit = true;
			_me.WaitOne();
		}
		public void Run()
		{
			if (!string.IsNullOrEmpty(LineFilter))
				_lineFilterRegex = new Regex(LineFilter);

			if (!string.IsNullOrEmpty(LevelRegex))
				_levelRegex = new Regex(LevelRegex, RegexOptions.Compiled | RegexOptions.Multiline);

			if (!File.Exists(_path))
			{
				throw new FileNotFoundException("File does not exist:" + _path);
			}
			FileInfo fi = new(_path);
			_prevLen = fi.Length;
			MakeTail(_nLines, _path);
			ThreadPool.QueueUserWorkItem(_ => ChangeLoop());
		}

		private async void ChangeLoop()
		{
			while (!_requestForExit)
			{
				await Fw_Changed();
				await Task.Delay(_pollInterval);
			}
			_me.Set();
		}

		private const int _pollInterval = 100;
		private const int _bufSize = 4096;
		private string _previous = string.Empty;

		private async Task Fw_Changed()
		{
			FileInfo fi = new(_path);
			if (fi.Exists)
			{
				if (fi.Length != _prevLen)
				{
					if (fi.Length < _prevLen)
					{
						//assume truncated!
						_prevLen = 0;
					}

					await using var stream = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite);
					stream.Seek(_prevLen, SeekOrigin.Begin);
					if (string.IsNullOrEmpty(LineFilter))
					{
						using StreamReader sr = new(stream);
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
						char[] buffer = new char[_bufSize];
						StringBuilder current = new();
						using StreamReader sr = new(stream);
						int nRead;
						do
						{
							nRead = await sr.ReadBlockAsync(buffer, 0, _bufSize);
							for (int i = 0; i < nRead; ++i)
							{
								if (buffer[i] == '\n' || buffer[i] == '\r')
								{
									if (current.Length > 0)
									{
										string line = string.Concat(_previous, current);

										if (_lineFilterRegex.IsMatch(line))
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
							_previous = current.ToString();
						}
					}
				}
				_prevLen = fi.Length;
			}

		}
		public event EventHandler<TailEventArgs> Changed;
		private async void MakeTail(int nLines, string path)
		{
			List<string> lines = new();

			await using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
			using (StreamReader sr = new(stream))
			{
				string line;
				while (null != (line = await sr.ReadLineAsync()))
				{
					if (!string.IsNullOrEmpty(LineFilter))
					{
						if (_lineFilterRegex.IsMatch(line))
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

			if (null == _levelRegex)
			{
				Changed(this, new TailEventArgs() { Line = l, Level = _currentLevel });
				return;
			}

			var match = _levelRegex.Match(l);

			if (!match.Success)
			{
				Changed(this, new TailEventArgs() { Line = l, Level = _currentLevel });
				return;
			}

			_currentLevel = match.Groups["level"].Value;

			Changed(this, new TailEventArgs() { Line = l, Level = _currentLevel });
		}
	}
}
