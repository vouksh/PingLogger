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

namespace PingLogger.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		public ReactiveCommand<string, Unit> CloseTabCommand { get; }
		public ReactiveCommand<Unit, Unit> AddTabCommand { get; }
		private ObservableCollection<TabItem> _tabItems = new ObservableCollection<TabItem>();
		public MainWindowViewModel()
		{
			CloseTabCommand = ReactiveCommand.Create<string>(CloseTab);
			AddTabCommand = ReactiveCommand.Create(AddBlankTab);
			_tabItems.CollectionChanged += _tabItems_CollectionChanged;
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
					PacketSize = 32,
					Threshold = 500,
					Timeout = 1000,
					Interval = 1000,
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
			AddTabItem(new Host { HostName = "google.com" });
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
				Text = $"Host: {host.HostName}",
				FontSize = 12,
				HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
				VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
				Background = Avalonia.Media.Brushes.Transparent,
				Padding = new Avalonia.Thickness(5, 0, 0, 0)
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
				Classes = new Classes("Main")
			};
			var pingControl = new Views.PingControl()
			{
				DataContext = new PingControlViewModel()
				{
					Host = host
				}
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


		private void CloseTab(string tabId)
		{

		}
	}
}
