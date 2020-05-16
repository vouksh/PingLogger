using PingLogger.GUI.Controls;
using PingLogger.GUI.Models;
using PingLogger.GUI.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PingLogger.GUI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public static App CurrentApp;
		private readonly List<TabItem> _tabItems;

		public ICommand CloseWindowCommand { get; set; }
		public ICommand MinimizeWindowCommand { get; set; }

		public ICommand NewTabCommand { get; set; }
		public ICommand CloseTabCommand { get; set; }

		public MainWindow(App curApp)
		{
			CurrentApp = curApp;
			CloseWindowCommand = new Command(Close);
			MinimizeWindowCommand = new Command(Minimize);

			CloseTabCommand = new CommandParam(tabDelBtn_Click);
			NewTabCommand = new Command(AddTabItem);

			Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			Thread.CurrentThread.Name = "MainWindowThread";
			InitializeComponent();
			_tabItems = new List<TabItem>();
			TabItem optsTab = new TabItem
			{
				Header = "Options",
				Name = "tabOpts",
				Content = new SettingsControl()
			};

			_tabItems.Add(optsTab);

			tabControl.DataContext = _tabItems;
			tabControl.SelectedIndex = 0;
			this.Title = $"PingLogger v{version}";
		}

		private void tabDelBtn_Click(object sender)
		{
			string hostId = (sender as string);

			var item = tabControl.Items.Cast<TabItem>().First(i => i.Uid.Equals(hostId));

			var selectedHost = Config.Hosts.First(h => h.Id.ToString() == hostId);
			if (item is TabItem tab)
			{
				var question = MessageBox.Show(
					$"Are you sure you want to remove host {selectedHost.HostName}",
					"Warning",
					MessageBoxButton.YesNo,
					MessageBoxImage.Question);
				if (question == MessageBoxResult.Yes)
				{
					(tab.Content as PingControl).DoStop();
					tabControl.DataContext = null;
					_tabItems.Remove(tab);
					Config.Hosts.Remove(selectedHost);
					tabControl.DataContext = _tabItems;
					if (!(tabControl.SelectedItem is TabItem selectedTab) || selectedTab.Equals(tab))
					{
						selectedTab = _tabItems[0];
					}

					tabControl.SelectedItem = selectedTab;
				}
			}
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (Config.Hosts.Count > 0)
			{
				foreach (var host in Config.Hosts)
				{
					AddTabItem(host);
				}
			}
			else
			{
				var newHost = new Host
				{
					Id = Guid.NewGuid(),
					HostName = "google.com"
				};
				AddTabItem(newHost);
			}
			tabControl.SelectedIndex = 0;
		}

		private void AddTabItem()
		{

			tabControl.DataContext = null;
			var newHost = new Host
			{
				Id = Guid.NewGuid(),
				HostName = "google.com"
			};
			int count = _tabItems.Count;
			TabItem tab = new TabItem
			{
				Header = string.Format("Host: {0}", newHost.HostName),
				Name = string.Format("tab{0}", count),
				HeaderTemplate = tabControl.FindResource("TabHeader") as DataTemplate,
				Uid = newHost.Id.ToString(),
			};
			tab.SetResourceReference(Control.TemplateProperty, "CloseButton");
			Config.Hosts.Add(newHost);
			PingControl pingControl = new PingControl(newHost, true);
			tab.Content = pingControl;
			_tabItems.Insert(count - 1, tab);
			tabControl.DataContext = _tabItems;
			tabControl.SelectedIndex = tabControl.Items.Count - 2;
			//return tab;
		}
		private TabItem AddTabItem(Host host, bool AddOnRuntime = false)
		{
			tabControl.DataContext = null;
			int count = _tabItems.Count;
			TabItem tab = new TabItem
			{
				Header = string.Format("Host: {0}", host.HostName),
				Name = string.Format("tab{0}", count),
				HeaderTemplate = tabControl.FindResource("TabHeader") as DataTemplate,
				Uid = host.Id.ToString()
			};

			tab.SetResourceReference(Control.TemplateProperty, "CloseButton");
			PingControl pingControl = new PingControl(host, AddOnRuntime);
			tab.Content = pingControl;
			_tabItems.Insert(count - 1, tab);
			tabControl.DataContext = _tabItems;
			return tab;
		}

		public void UpdateHeader(string hostName, string hostId)
		{
			var item = tabControl.Items.Cast<TabItem>().First(i => i.Uid.Equals(hostId.ToString()));

			if (item is TabItem tab)
			{
				tabControl.SelectedItem = tab;
				var curIndex = _tabItems.IndexOf(tab);
				_tabItems.Remove(tab);
				tab.Header = $"Host: {hostName}";
				_tabItems.Insert(curIndex, tab);
			}
		}

		public void StopAllLoggers()
		{
			var question = MessageBox.Show("Are you sure you want to stop all ping loggers?", "Alert", MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (question == MessageBoxResult.Yes)
			{
				foreach (var item in _tabItems)
				{
					try
					{
						if (item != null && (item.Header as string).Contains("Host:"))
						{
							var pingCtrl = item.Content as PingControl;
							pingCtrl.DoStop();
						}
					}
					catch (Exception e)
					{
						Logger.Log.Debug(e.ToString());
					}
				}
			}
		}

		public void StartAllLoggers()
		{
			var question = MessageBox.Show("Are you sure you want to start all ping loggers?", "Alert", MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (question == MessageBoxResult.Yes)
			{
				foreach (var item in _tabItems)
				{
					try
					{
						if (item != null && (item.Header as string).Contains("Host:"))
						{
							var pingCtrl = item.Content as PingControl;
							pingCtrl.DoStart();
						}
					}
					catch (Exception e)
					{
						Logger.Debug(e.ToString());
					}
				}
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Logger.Debug("Application closing");
			foreach (var item in _tabItems)
			{
				try
				{
					if (item != null && item.Content != null && item.Header != null && (item.Header as string).Contains("Host:"))
					{
						var pingCtrl = item.Content as PingControl;
						pingCtrl.DoStop();
					}
				}
				catch (Exception ex)
				{
					Logger.Debug(ex.ToString());
				}
			}
		}

		private void Window_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.F1)
			{
				HelpDialog helpDialog = new HelpDialog();
				helpDialog.ShowDialog();
			}
		}

		private void Minimize()
		{
			WindowState = WindowState.Minimized;
		}
	}
}
