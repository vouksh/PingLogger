using PingLogger.Controls;
using PingLogger.Models;
using PingLogger.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
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

			TabControl.DataContext = _tabItems;
			TabControl.SelectedIndex = 0;
			this.Title = $"PingLogger v{version}";
		}

		public async void ToggleWindowSize()
		{
			ScrollViewer scroller = (ScrollViewer)TabControl.Template.FindName("TabControlScroller", TabControl);
			if (Config.WindowExpanded)
			{
				this.Width = 805;
				this.Height = 480;
			}
			else
			{
				this.Width = 420;
				await Task.Delay(50); // This is really dumb, but we gotta wait for the UI thread to update before adjusting the height.
				this.Height = scroller.ComputedHorizontalScrollBarVisibility == Visibility.Visible ? 495 : 480;
			}
			foreach (var item in TabControl.Items.Cast<TabItem>())
			{
				(item.Content as PingControl)?.ToggleSideVisibility();
			}
		}

		public void UpdateGraphStyles()
		{
			foreach (var item in TabControl.Items.Cast<TabItem>())
			{
				(item.Content as PingControl)?.StatusGraphControl.StylePlot(true);
				(item.Content as PingControl)?.PingGraphControl.StylePlot();
			}
		}

		private void OpenOptionsDialog() => new SettingsDialog(this).ShowDialog();

		private void TabDelBtn_Click(object sender)
		{
			string hostId = (sender as string);

			var item = TabControl.Items.Cast<TabItem>().First(i => i.Uid.Equals(hostId));

			var selectedHost = Config.Hosts.First(h => h.Id.ToString() == hostId);
			if (item is { } tab)
			{
				var question = MessageBox.Show(
					$"Are you sure you want to remove host {selectedHost.HostName}",
					"Warning",
					MessageBoxButton.YesNo,
					MessageBoxImage.Question);
				if (question == MessageBoxResult.Yes)
				{
					(tab.Content as PingControl)?.DoStop();
					TabControl.DataContext = null;
					_tabItems.Remove(tab);
					Config.Hosts.Remove(selectedHost);
					TabControl.DataContext = _tabItems;
					if (TabControl.SelectedItem is not TabItem selectedTab || selectedTab.Equals(tab))
					{
						selectedTab = _tabItems[0];
					}

					TabControl.SelectedItem = selectedTab;
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
			TabControl.SelectedIndex = Config.LastSelectedTab;
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
			TabControl.SelectedIndex = TabControl.Items.Count - 1;
			ToggleWindowSize();
		}
		private void AddTabItem(Host host, bool addOnRuntime = false)
		{
			TabControl.DataContext = null;
			int count = _tabItems.Count;
			TabItem tab = new TabItem
			{
				Header = $"Host: {host.HostName}",
				Name = $"tab{count}",
				HeaderTemplate = TabControl.FindResource("TabHeader") as DataTemplate,
				Uid = host.Id.ToString()
			};

			tab.SetResourceReference(TemplateProperty, "CloseButton");
			PingControl pingControl = new PingControl(host, addOnRuntime);
			tab.Content = pingControl;
			_tabItems.Insert(count, tab);
			TabControl.DataContext = _tabItems;
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
						if (item != null && ((string)item.Header).Contains("Host:"))
						{
							var pingCtrl = item.Content as PingControl;
							pingCtrl?.DoStop();
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
						if (item != null && ((string)item.Header).Contains("Host:"))
						{
							var pingCtrl = item.Content as PingControl;
							pingCtrl?.DoStart();
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
			Config.LastSelectedTab = TabControl.SelectedIndex;
			foreach (var item in _tabItems)
			{
				try
				{
					if (item?.Content != null && item.Header != null && ((string)item.Header).Contains("Host:"))
					{
						var pingCtrl = item.Content as PingControl;
						pingCtrl?.DoStop();
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
			TabControl tabControl = (TabControl) sender;
			ScrollViewer scroller = (ScrollViewer) tabControl.Template.FindName("TabControlScroller", tabControl);

			if (scroller != null && tabControl.SelectedIndex > 0)
			{
				double index = tabControl.SelectedIndex;
				double offset = index * (scroller.ScrollableWidth / tabControl.Items.Count);
				scroller.ScrollToHorizontalOffset(offset);
			}
		}
	}
}
