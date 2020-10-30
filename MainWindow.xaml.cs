using PingLogger.Controls;
using PingLogger.Models;
using PingLogger.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PingLogger
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly List<TabItem> _tabItems;

		public ICommand CloseWindowCommand { get; set; }
		public ICommand MinimizeWindowCommand { get; set; }
		public ICommand OptionsWindowCommand { get; set; }

		public ICommand NewTabCommand { get; set; }
		public ICommand CloseTabCommand { get; set; }

		public MainWindow()
		{
			CloseWindowCommand = new Command(Close);
			MinimizeWindowCommand = new Command(Minimize);
			OptionsWindowCommand = new Command(OpenOptionsDialog);

			CloseTabCommand = new CommandParam(TabDelBtn_Click);
			NewTabCommand = new Command(AddTabItem);

			Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			InitializeComponent();
			_tabItems = new List<TabItem>();

			tabControl.DataContext = _tabItems;
			tabControl.SelectedIndex = 0;
			this.Title = $"PingLogger v{version}";
		}

		public async void ToggleWindowSize()
		{
			ScrollViewer scroller = (ScrollViewer)tabControl.Template.FindName("TabControlScroller", tabControl);
			if (Config.WindowExpanded)
			{
				this.Width = 805;
				this.Height = 480;
			}
			else
			{
				this.Width = 420;
				await Task.Delay(50); // This is really dumb, but we gotta wait for the UI thread to update before adjusting the height.
				if (scroller.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
				{
					this.Height = 495;
				}
				else
				{
					this.Height = 480;
				}
			}
			foreach (var item in tabControl.Items.Cast<TabItem>())
			{
				(item.Content as PingControl).ToggleSideVisibility();
			}
		}

		private void OpenOptionsDialog() => new SettingsDialog(this).ShowDialog();

		private void TabDelBtn_Click(object sender)
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
			ToggleWindowSize();
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
				Config.Hosts.Add(newHost);
				AddTabItem(newHost);
			}
			tabControl.SelectedIndex = Config.LastSelectedTab;
			if (Config.LoadWithWindows && Config.StartApplicationMinimized)
			{
				this.Minimize();
			}
			ToggleWindowSize();
		}

		private void AddTabItem()
		{
			var addDialog = new AddHostDialog
			{
				Owner = GetWindow(this)
			};
			addDialog.ShowDialog();
		}
		public void AddTab(string hostName)
		{
			var newHost = new Host { HostName = hostName, Id = Guid.NewGuid() };
			Config.Hosts.Add(newHost);
			AddTabItem(newHost, true);
			tabControl.SelectedIndex = tabControl.Items.Count - 1;
			ToggleWindowSize();
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
			_tabItems.Insert(count, tab);
			tabControl.DataContext = _tabItems;
			ToggleWindowSize();
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
			ToggleWindowSize();
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
			Config.LastSelectedTab = tabControl.SelectedIndex;
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

		private void Minimize() => WindowState = WindowState.Minimized;

		private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			TabControl tabControl = (TabControl)sender;
			ScrollViewer scroller = (ScrollViewer)tabControl.Template.FindName("TabControlScroller", tabControl);
			if (scroller != null && tabControl.SelectedIndex > 0)
			{
				double index = (double)(tabControl.SelectedIndex);
				double offset = index * (scroller.ScrollableWidth / (double)(tabControl.Items.Count));
				scroller.ScrollToHorizontalOffset(offset);
			}
		}
	}
}
