using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Linq;

namespace PingLogger
{
	class LogWriter
	{
		private string fileName = string.Empty;
		private BlockingCollection<string> logWrites = new BlockingCollection<string>();
		public LogWriter(string file)
		{
			if (!file.EndsWith(".log"))
				file += ".log";
			if (file.Contains('/') || file.Contains('\\'))
			{
				fileName = DateTime.Now.Month +"-" + DateTime.Now.Day + "-" + DateTime.Now.Year + "_" + file;
			} else
			{
				fileName = Environment.CurrentDirectory + "/" + DateTime.Now.Month + "-" + DateTime.Now.Day + "-" + DateTime.Now.Year + "_" + file;
			}
			var _logThread = new Thread(DoLogWrite);
			_logThread.Name = "PingLogger file stream";
			_logThread.Start();
		}

		public void WriteLog(string logString)
		{
			if ((string)logString != "CLOSELOG")
			{
				StringBuilder sBuild = new StringBuilder();
				sBuild.AppendFormat(
					"{0}:{1}:{2}:{3}# {4}",
					DateTime.Now.Hour.ToString().PadLeft(2, '0'),
					DateTime.Now.Minute.ToString().PadLeft(2, '0'),
					DateTime.Now.Second.ToString().PadLeft(2, '0'),
					DateTime.Now.Millisecond.ToString().PadLeft(3, '0'),
					logString
					);
					logWrites.Add(sBuild.ToString());
			}
			else
			{
				logWrites.Add((string)logString);
			}
		}

		public void WritePingLog(string host, int roundtrip, int ttl, int threshold)
		{
			StringBuilder builder = new StringBuilder();
			if (roundtrip >= threshold)
			{
				builder.AppendFormat(
					"Pinged {0} RoundTrip: {1}ms (WARNING) TTL: {2}",
					host,
					roundtrip,
					ttl
				);
			}
			else
			{
				builder.AppendFormat(
					"Pinged {0} RoundTrip: {1}ms TTL: {2}",
					host,
					roundtrip,
					ttl
				);
			}
			
			WriteLog(builder.ToString());
		}

		private void DoLogWrite()
		{
			var exists = File.Exists(fileName);
			StreamWriter writer = new StreamWriter(fileName, true);
			if (!exists)
			{
				writer.WriteLine(fileName + " created at " +
						DateTime.Now.ToShortDateString() + " " +
						DateTime.Now.ToLongTimeString());
			}
			else
			{
				writer.WriteLine("----------------------------------------");
				writer.WriteLine(fileName + " opened at " +
					DateTime.Now.ToShortDateString() + " " +
					DateTime.Now.ToLongTimeString());
			}
			while (true)
			{
				string logLine = logWrites.Take();
				if (logLine == "CLOSELOG")
				{
					break;
				}
				else if (logLine != "")
				{
					writer.WriteLine(logLine);
				}
				writer.Flush();
			}
			writer.WriteLine("Closing log file");
			writer.Flush();
			writer.Close();
		}
	}
}
