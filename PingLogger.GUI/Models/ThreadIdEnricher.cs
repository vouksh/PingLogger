using Serilog.Core;
using Serilog.Events;
using System.Threading;

namespace PingLogger.GUI.Models
{
	class ThreadIdEnricher : ILogEventEnricher
	{
		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			string id = Thread.CurrentThread.Name ?? Thread.CurrentThread.ManagedThreadId.ToString();
			logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ThreadId", id));
		}
	}
}
