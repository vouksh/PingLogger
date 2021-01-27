using ReactiveUI;

namespace PingLogger.ViewModels
{
	public class SplashScreenViewModel : ViewModelBase
	{
		private int progressBarValue = 0;
		public int ProgressBarValue
		{
			get => progressBarValue;
			set => this.RaiseAndSetIfChanged(ref progressBarValue, value);
		}

		private int progressBarMax = 1;
		public int ProgressBarMax
		{
			get => progressBarMax;
			set => this.RaiseAndSetIfChanged(ref progressBarMax, value);
		}

		private bool progressBarIndeterminate = true;
		public bool ProgressBarIndeterminate
		{
			get => progressBarIndeterminate;
			set => this.RaiseAndSetIfChanged(ref progressBarIndeterminate, value);
		}

		private string updateMessage = "Checking for updates...";
		public string UpdateMessage
		{
			get => updateMessage;
			set => this.RaiseAndSetIfChanged(ref updateMessage, value);
		}
	}
}
