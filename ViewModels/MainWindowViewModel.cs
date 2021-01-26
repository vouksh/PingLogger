using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using PingLogger.Models;
using PingLogger.Workers;
using Avalonia.Interactivity;
using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace PingLogger.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		public ReactiveCommand<string, Unit> CloseTabCommand { get; }
		public ReactiveCommand<Unit, Unit> AddTabCommand { get; }
		public ReactiveCommand<Unit, Unit> OpenOptionsCommand { get; }
		private ObservableCollection<TabItem> _tabItems = new ObservableCollection<TabItem>();
		public delegate void ThemeChangedEventHandler(object sender);
		public event ThemeChangedEventHandler ThemeChanged;

		public MainWindowViewModel()
		{
			CloseTabCommand = ReactiveCommand.Create<string>(CloseTab);
			AddTabCommand = ReactiveCommand.Create(AddBlankTab);
			OpenOptionsCommand = ReactiveCommand.Create(OpenOptions);
			_tabItems.CollectionChanged += _tabItems_CollectionChanged;
		}

		private void OpenOptions()
		{
			var optionsViewModel = new OptionsWindowViewModel();
			optionsViewModel.ThemeChanged += (object s) => ThemeChanged?.Invoke(s);
			var optionsDialog = new Views.OptionsWindow()
			{
				DataContext = optionsViewModel
			};
			
			optionsDialog.Show();
		}

		private void _tabItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			this.RaisePropertyChanged("TabItems");
		}

		public static string Title
		{
			get
			{
				Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
				return $"PingLogger v{version}";
			}
		}
		public List<TabItem> TabItems
		{
			get
			{
				if(_tabItems.Count == 0)
				{
					GenerateTabItems();
				}
				return _tabItems.ToList();
			}
		}

		private void GenerateTabItems()
		{
			if (Config.Hosts.Any())
			{
				foreach(Host host in Config.Hosts)
				{
					AddTabItem(host, true);
				}
			} else
			{
				var newHost = new Host()
				{
					HostName = "google.com",
					Id = Guid.NewGuid()
				};
				AddTabItem(newHost, true);
			}
			var newTabTI = new TabItem()
			{
				Header = new Button()
				{
					Content = "+",
					FontSize = 14,
					Command = AddTabCommand
				}
			};
			_tabItems.Add(newTabTI);
		}

		private void AddBlankTab()
		{
			var newHost = new Host { 
				HostName = "google.com",
				Id = Guid.NewGuid()
			};
			Config.Hosts.Add(newHost);
			AddTabItem(newHost);
		}

		private void AddTabItem(Host host, bool AddOnRuntime = false)
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
				Padding = new Avalonia.Thickness(5, 0, 0, 0),
				Classes = new Classes("Tab")
			};
			headerGrid.Children.Add(headerText);

			var closeTabBtn = new Button() { 
				Content = "X", 
				FontSize = 12, 
				Command = CloseTabCommand, 
				CommandParameter = host.Id.ToString(),
				Margin = new Avalonia.Thickness(2, 0, 0, 0),
				Padding = new Avalonia.Thickness(1, 1, 1, 1)
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
			pingControlVM.HostNameUpdated += PingControlVM_HostNameUpdated;
			var pingControl = new Views.PingControl()
			{
				DataContext = pingControlVM
			};
			tabItem.Content = pingControl;
			if(!AddOnRuntime)
			{
				_tabItems.Insert(count - 1, tabItem);
			} else
			{
				_tabItems.Add(tabItem);
			}
		}

		private void PingControlVM_HostNameUpdated(object sender, HostNameUpdatedEventArgs e)
		{
			var index = _tabItems.IndexOf(_tabItems.First(t => t.Tag.ToString() == e.HostId));
			var headerGrid = new Grid
			{
				ColumnDefinitions = new ColumnDefinitions("*,20"),
				VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
				Height = 36
			};

			var headerText = new TextBlock()
			{
				Text = e.HostName,
				FontSize = 12,
				HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
				VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
				Background = Avalonia.Media.Brushes.Transparent,
				Padding = new Avalonia.Thickness(5, 0, 0, 0),
				Classes = new Classes("Tab")
			};
			headerGrid.Children.Add(headerText);

			var closeTabBtn = new Button()
			{
				Content = "X",
				FontSize = 12,
				Command = CloseTabCommand,
				CommandParameter = e.HostId,
				Margin = new Avalonia.Thickness(2, 0, 0, 0),
				Padding = new Avalonia.Thickness(1, 1, 1, 1)
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
		}

		public void SelectedTabChanged(object sender, RoutedEventArgs e)
		{
			
		}
	}
}
