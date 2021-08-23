using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using PingLogger.Extensions;
using PingLogger.Models;
using PingLogger.Workers;
using Projektanker.Icons.Avalonia;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;

namespace PingLogger.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		public ReactiveCommand<string, Unit> CloseTabCommand { get; }
		public ReactiveCommand<Unit, Unit> AddTabCommand { get; }
		public ReactiveCommand<Unit, Unit> OpenOptionsCommand { get; }
		public ReactiveCommand<Unit, Unit> OpenHelpCommand { get; }
		private readonly ObservableCollection<TabItem> _tabItems = new();
		public delegate void ThemeChangedEventHandler(object sender);
		public event ThemeChangedEventHandler ThemeChanged;
		private Views.AddHostDialog AddHostDialog;

		public MainWindowViewModel()
		{
			CloseTabCommand = ReactiveCommand.Create<string>(CloseTab);
			AddTabCommand = ReactiveCommand.Create(AddBlankTab);
			OpenOptionsCommand = ReactiveCommand.Create(OpenOptions);
			OpenHelpCommand = ReactiveCommand.Create(OpenHelp);
			_tabItems.CollectionChanged += TabItems_CollectionChanged;
			WindowWidth = Config.WindowExpanded ? 805 : 410;
		}

		private void OpenHelp()
		{
#if Windows
			try
			{
				Process.Start("https://github.com/vouksh/PingLogger/blob/master/README.md");
			}
			catch
			{
				Process.Start(new ProcessStartInfo("cmd", $"/c start https://github.com/vouksh/PingLogger/blob/master/README.md")
				{
					CreateNoWindow = true
				});
			}
#elif Linux
			Process.Start("xdg-open", "https://github.com/vouksh/PingLogger/blob/master/README.md");
#elif OSX
			Process.Start("open", "https://github.com/vouksh/PingLogger/blob/master/README.md");
#endif
		}

		private void OpenOptions()
		{
			var optionsViewModel = new OptionsWindowViewModel();
			optionsViewModel.ThemeChanged += OptionsViewModel_ThemeChanged;
			var optionsDialog = new Views.OptionsWindow()
			{
				DataContext = optionsViewModel
			};

			if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				optionsDialog.ShowDialog(desktop.MainWindow);
			}
		}

		private void OptionsViewModel_ThemeChanged(object sender)
		{
			ThemeChanged?.Invoke(sender);
			foreach (var tabItem in _tabItems)
			{
				((tabItem.Content as Views.PingControl).DataContext as PingControlViewModel).SetupGraphs();
			}
		}

		private void TabItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			this.RaisePropertyChanged(nameof(TabItems));
		}

		public static string Title
		{
			get
			{
				Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
#if DEBUG
				return $"PingLogger v{version.ToShortString()}b-{version.Revision}";
#else
				return $"PingLogger v{version.ToShortString()}";
#endif
			}
		}
		public List<TabItem> TabItems
		{
			get
			{
				if (_tabItems.Count == 0)
				{
					GenerateTabItems();
				}
				return _tabItems.ToList();
			}
		}

		private int selectedTabIndex = 0;
		public int SelectedTabIndex
		{
			get => selectedTabIndex;
			set
			{
				Config.LastSelectedTab = value;
				this.RaiseAndSetIfChanged(ref selectedTabIndex, value);
			}
		}

		private int windowWidth = 805;
		public int WindowWidth
		{
			get => windowWidth;
			set => this.RaiseAndSetIfChanged(ref windowWidth, value);
		}

		private void GenerateTabItems()
		{
			if (Config.Hosts.Any())
			{
				foreach (Host host in Config.Hosts)
				{
					AddTabItem(host);
				}
				SelectedTabIndex = Config.LastSelectedTab;
			}
			else
			{
				var newHost = new Host
				{
					HostName = "google.com",
					Id = Guid.NewGuid()
				};
				Config.Hosts.Add(newHost);
				AddTabItem(newHost);
			}
			StartAllLoggers();
		}

		private void StartAllLoggers()
		{
			if (Config.StartLoggersAutomatically)
			{
				foreach (TabItem tabItem in _tabItems)
				{
					((tabItem.Content as Views.PingControl).DataContext as PingControlViewModel).TriggerPinger(true);
				}
			}
		}

		private void AddBlankTab()
		{
			if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				var addHostVM = new AddHostDialogViewModel();
				addHostVM.WindowClosed += AddHostVM_WindowClosed;
				AddHostDialog = new Views.AddHostDialog()
				{
					DataContext = addHostVM
				};
				AddHostDialog.ShowDialog(desktop.MainWindow);
			}
		}

		private void AddHostVM_WindowClosed(object sender, AddHostEventArgs e)
		{
			if (e.IsValid)
			{
				var newHost = new Host(e.HostName, e.IPAddress);
				Config.Hosts.Add(newHost);
				AddTabItem(newHost);
			}
			AddHostDialog.Close();
		}

		private void AddTabItem(Host host)
		{
			int count = _tabItems.Count;
			var headerGrid = new Grid
			{
				ColumnDefinitions = new ColumnDefinitions("*,20"),
				VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
				Height = 36
			};

			var headerText = new TextBlock()
			{
				Text = host.HostName,
				FontSize = 12,
				HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
				VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
				Background = Avalonia.Media.Brushes.Transparent,
				Padding = new Thickness(5, 0, 0, 0),
				Classes = new Classes("Tab")
			};
			headerGrid.Children.Add(headerText);

			var closeTabBtn = new Button()
			{
				Content = new Icon() { Value = "fas fa-times" },
				FontSize = 12,
				Command = CloseTabCommand,
				CommandParameter = host.Id.ToString(),
				Margin = new Thickness(2, 0, 0, 0),
				Padding = new Thickness(1, 1, 1, 1),
				Foreground = Avalonia.Media.Brushes.Red
			};
			Grid.SetColumn(closeTabBtn, 1);
			headerGrid.Children.Add(closeTabBtn);

			var tabItem = new TabItem
			{
				Header = headerGrid,
				Name = $"tab{count}",
				Tag = host.Id.ToString(),
				Classes = new Classes("MainTabItem")
			};
			var pingControlVM = new PingControlViewModel()
			{
				Host = host
			};
			pingControlVM.WindowExpandedEvent += PingControlVM_WindowExpandedEvent;
			pingControlVM.HostNameUpdated += PingControlVM_HostNameUpdated;
			pingControlVM.TraceRouteCallback += PingControlVM_TraceRouteCallback;
			var pingControl = new Views.PingControl()
			{
				DataContext = pingControlVM
			};
			tabItem.Content = pingControl;
			_tabItems.Add(tabItem);
			SelectedTabIndex = _tabItems.IndexOf(tabItem);
		}

		private void PingControlVM_WindowExpandedEvent(object sender, bool expand)
		{
			if (expand)
			{
				WindowWidth = 805;
			}
			else
			{
				WindowWidth = 410;
			}
		}

		private void PingControlVM_TraceRouteCallback(object sender, TraceRouteCallbackEventArgs e)
		{
			if (!Config.Hosts.Any(h => h.HostName.ToLower() == e.HostName.ToLower()))
			{
				var newHost = new Host
				{
					HostName = e.HostName,
					Id = Guid.NewGuid()
				};
				Config.Hosts.Add(newHost);
				AddTabItem(newHost);
			}
			else
			{
				if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
				{
					Views.MessageBox.ShowAsError("Error", "Host already exists");
				}
			}
		}

		private void PingControlVM_HostNameUpdated(object sender, HostNameUpdatedEventArgs e)
		{
			var index = _tabItems.IndexOf(_tabItems.First(t => t.Tag.ToString() == e.HostId));
			var headerGrid = new Grid
			{
				ColumnDefinitions = new ColumnDefinitions("*,20"),
				VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
				Height = 36,
				Tag = e.HostId
			};

			var headerText = new TextBlock()
			{
				Text = e.HostName,
				FontSize = 12,
				HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
				VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
				Background = Avalonia.Media.Brushes.Transparent,
				Padding = new Thickness(5, 0, 0, 0),
				Classes = new Classes("Tab")
			};
			headerGrid.Children.Add(headerText);

			var closeTabBtn = new Button()
			{
				Content = new Icon() { Value = "fas fa-times" },
				FontSize = 12,
				Command = CloseTabCommand,
				CommandParameter = e.HostId,
				Margin = new Thickness(2, 0, 0, 0),
				Padding = new Thickness(1, 1, 1, 1),
				Foreground = Avalonia.Media.Brushes.Red
			};

			Grid.SetColumn(closeTabBtn, 1);
			headerGrid.Children.Add(closeTabBtn);
			_tabItems[index].Header = headerGrid;
		}

		private void CloseTab(string tabId)
		{
			var tabItem = _tabItems.IndexOf(_tabItems.First(t => t.Tag.ToString() == tabId));
			_tabItems.RemoveAt(tabItem);
			Config.Hosts.RemoveAt(Config.Hosts.IndexOf(Config.Hosts.First(h => h.Id.ToString() == tabId)));
			if (_tabItems.Count == 0)
			{
				AddBlankTab();
			}
		}
	}
}
