using Avalonia;
using PingLogger.Models;
using PingLogger.Workers;
using ReactiveUI;
using System;
using System.IO;

namespace PingLogger.ViewModels
{
	public class WatchLogViewModel : ViewModelBase
	{
		private Tail tail;
		public Host Host;

		public WatchLogViewModel()
		{
		}

		public void Start()
		{
			tail = new Tail($"{Config.LogSavePath}{Host.HostName}{Path.DirectorySeparatorChar}{Host.HostName}-{DateTime.Now:yyyyMMdd}.log", 1024);
			tail.Changed += Tail_Changed;
			tail.Run();
			ScrollOffset = new Vector(0, LogText.Length);
		}

		public void Closing()
		{
			tail.Stop();
		}

		private void Tail_Changed(object sender, Tail.TailEventArgs e)
		{
			LogText += e.Line;
			ScrollOffset = new Vector(0, LogText.Length);
		}

		private string logText = string.Empty;
		public string LogText
		{
			get => logText;
			set => this.RaiseAndSetIfChanged(ref logText, value);
		}

		private Vector scrollOffset = new Vector();
		public Vector ScrollOffset
		{
			get => scrollOffset;
			set => this.RaiseAndSetIfChanged(ref scrollOffset, value);
		}
	}
}
