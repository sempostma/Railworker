using System.Collections.ObjectModel;
using System.ComponentModel;
using System;
using System.Windows;
using System.Threading;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Railworker.Properties;
using System.Windows.Controls;
using Microsoft.VisualBasic.Logging;
using System.IO.Compression;
using System.Linq;
using RWLib.Interfaces;
using Railworker.Core;
using System.Windows.Input;

namespace Railworker.Pages
{
    /// <summary>
    /// Interaction logic for RollingStockReplacement.xaml
    /// </summary>
    public partial class RollingStockReplacement : Page
    {
        internal App App { get => (App)Application.Current; }
        internal Logger Logger { get => App.Logger; }

        public class RollingStockReplacementViewModel : ViewModel
        {
            private bool _didPressEnter = false;
            public bool DidPressEnter
            {
                get => _didPressEnter;
                set => SetProperty(ref _didPressEnter, value);
            }
            private string _searchTerm = "";
            public string SearchTerm
            {
                get => _searchTerm;
                set => SetProperty(ref _searchTerm, value);
            }
            private DateTime _lastSearch = DateTime.Now;
            public DateTime LastSearch
            {
                get => _lastSearch;
                set => SetProperty(ref _lastSearch, value);
            }
            public ObservableCollection<DirectoryItem> Directories { get; set; } = new ObservableCollection<DirectoryItem>();

            public required Scenario Scenario { get; set; }
            public ObservableCollection<ConsistVehicle> ConsistVehicles { get; } = new ObservableCollection<ConsistVehicle>();
            public ObservableCollection<Consist> Consists { get; } = new ObservableCollection<Consist>();

            private Visibility _clearButtonVisibility = Visibility.Hidden;
            public Visibility ClearButtonVisibility
            {
                get => _clearButtonVisibility;
                set => SetProperty(ref _clearButtonVisibility, value);
            }

            public ObservableCollection<ReplacementVehicle> AvailableVehicles { get; } = new ObservableCollection<ReplacementVehicle>();

            private string _searchText = "";
            public String SearchText
            {
                get => _searchText;
                set
                {
                    SetProperty(ref _searchText, value);
                }
            }

            private int _vehicleAssetLoadingProgress = 0;
            public int VehicleAssetLoadingProgress
            {
                get => _vehicleAssetLoadingProgress;
                set
                {
                    SetProperty(ref _vehicleAssetLoadingProgress, value);
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(CombinedLoadingProgress)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(LoadingBarVisible)));
                }
            }

            private int _scenarioLoadingProgress = 0;
            public int ScenarioLoadingProgress
            {
                get => _scenarioLoadingProgress;
                set
                {
                    SetProperty(ref _scenarioLoadingProgress, value);
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(CombinedLoadingProgress)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(LoadingBarVisible)));
                }
            }

            public int CombinedLoadingProgress
            {
                get => Math.Max(ScenarioLoadingProgress, VehicleAssetLoadingProgress);
            }

            public Visibility LoadingBarVisible
            {
                get => CombinedLoadingProgress > 0 ? Visibility.Visible : Visibility.Hidden;
            }

            private string _loadingInformation = "";
            public string LoadingInformation
            {
                get => _loadingInformation;
                set
                {
                    SetProperty(ref _loadingInformation, value);
                }
            }

            private bool _vehicleScanInProgress = false;
            public bool VehicleScanInProgress
            {
                get => _vehicleScanInProgress;
                set
                {
                    SetProperty(ref _vehicleScanInProgress, value);
                }
            }

            public RollingStockReplacementViewModel()
            {
                AvailableVehicles.CollectionChanged += AvailableVehicles_CollectionChanged;
            }

            public Consist? _selectedConsist = null;
            public Consist? SelectedConsist
            {
                get => _selectedConsist;
                set
                {
                    SetProperty(ref _selectedConsist, value);
                }
            }

            private void AvailableVehicles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(ClearButtonVisibility)));
            }
        }

        public RollingStockReplacementViewModel ViewModel { get; private set; }

        private CancellationTokenSource? ScanCancellationTokenSource;
        private ReplacementRulesWindow? ReplacementRulesWindow;
        private Action RefreshAvailableVehiclesDebounced;
        private LogShortcut Log;

        private bool didPressEnter = false;
        private string searchTerm = "";
        private DateTime lastSearch = DateTime.Now;

        public RollingStockReplacement(Scenario scenario)
        {
            InitializeComponent();
            ViewModel = new RollingStockReplacementViewModel { Scenario = scenario };
            RefreshAvailableVehiclesDebounced = Utilities.Debounce(this.RefreshAvailableVehicles, 300);
            DataContext = ViewModel;
            Log = new LogShortcut(App.Logger);
            ReadScenario();
        }

        class LogShortcut
        {
            private IRWLogger logger;

            public LogShortcut(IRWLogger logger)
            {
                this.logger = logger;
            }
            public void Debug(params string[] args)
            {
                logger?.Log(RWLogType.Debug, String.Format(args[0], args.Skip(1)));
            }
        }

        private void RefreshAvailableVehicles()
        {
            throw new NotImplementedException();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {

        }

        public async Task<List<Consist>> GetConsists(IProgress<int> progress)
        {
            var list = new List<Consist>();

            await foreach (var item in App.RWLib!.RouteLoader.LoadConsists(ViewModel.Scenario.RWScenario))
            {
                list.Add(item: new Consist
                {
                    RWConsist = item
                });
            }

            return list;
        }

        public async void ReadScenario()
        {
            IProgress<int> progress = new Progress<int>(value => { ViewModel.ScenarioLoadingProgress = value; });
            ViewModel.LoadingInformation = Railworker.Language.Resources.reading_scenario_files;

            List<Task> tasks = new List<Task>();
            var readConsistsTask = Task.Run(async () =>
            {
                List<Consist> ret = await GetConsists(progress);

                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    ViewModel.Consists.Clear();
                    foreach (Consist consist in ret)
                    {
                        ViewModel.Consists.Add(consist);
                    }
                });
            });
            var populateDirectoryTask = Task.Run((Action)(() =>
            {
                DirectoryItem rootNode = new DirectoryItem
                {
                    Name = "Assets",
                    Path = Path.Combine(Settings.Default.TsPath, "Assets")
                };
                rootNode.PopulateSubDirectories();

                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    foreach (DirectoryItem item in rootNode.SubDirectories)
                    {
                        ViewModel.Directories.Add(item);
                    }
                });
            }));

            await Task.WhenAll(readConsistsTask, populateDirectoryTask);
            progress.Report(0);
        }

        private void DirectoryTree_TextInput(object sender, TextCompositionEventArgs e)
        {
            var selectedItem = DirectoryTree.SelectedItem as DirectoryItem;

            if (selectedItem == null) return;
            TreeViewItem? selectedTvi = (TreeViewItem?)ContainerFromItem(DirectoryTree, selectedItem);
            if (selectedTvi == null) return;

            if (e.Text == "\b")
            {
                if (selectedItem == null)
                {
                    return;
                }
                selectedTvi.IsExpanded = false;

                return;

            }
            if (e.Text == "\r")
            {
                if (selectedItem == null)
                {
                    return;
                }

                didPressEnter = true;

                // expand, select and show sub directories
                selectedItem.PopulateSubDirectories();
                selectedTvi.IsSelected = true;
                selectedItem.IsSelected = true;
                selectedTvi.IsExpanded = true;

                return;
            }

            if ((DateTime.Now - lastSearch).Seconds > 1)
            {
                searchTerm = "";
            }

            lastSearch = DateTime.Now;
            searchTerm += e.Text;

            List<DirectoryItem> searchItems;

            if (selectedItem == null)
            {
                searchItems = DirectoryTree.Items.Cast<DirectoryItem>().ToList();
            }
            else if (didPressEnter)
            {
                searchItems = selectedTvi.Items.Cast<DirectoryItem>().ToList();
            }
            else
            {
                ItemsControl? parent = ParentContainerFromItem(DirectoryTree, selectedItem);

                if (parent is TreeViewItem)
                {
                    searchItems = ((TreeViewItem)parent).Items.Cast<DirectoryItem>().ToList();
                }
                else
                {
                    searchItems = DirectoryTree.Items.Cast<DirectoryItem>().ToList();
                }
            }

            var firstItem = searchItems.FirstOrDefault(item => item.Name.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase));

            if (firstItem == null)
            {
                searchTerm = e.Text;
                firstItem = searchItems.FirstOrDefault(item => item.Name.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (firstItem != null)
            {
                TreeViewItem? firstItemTvi = (TreeViewItem?)ContainerFromItem(DirectoryTree, firstItem);
                if (firstItemTvi != null)
                {
                    firstItem.IsSelected = true;
                    firstItemTvi.IsSelected = true;
                    firstItemTvi.BringIntoView();
                }
            }

            didPressEnter = false;
        }

        public DependencyObject? ContainerFromItem(ItemsControl itemsControl, object value)
        {
            var dp = itemsControl.ItemContainerGenerator.ContainerFromItem(value);

            if (dp != null)
                return dp;

            foreach (var item in itemsControl.Items)
            {
                var currentTreeViewItem = itemsControl.ItemContainerGenerator.ContainerFromItem(item);

                if (currentTreeViewItem is ItemsControl == false)
                {
                    continue;
                }

                var childDp = ContainerFromItem((ItemsControl)currentTreeViewItem, value);

                if (childDp != null)
                    return childDp;
            }
            return null;
        }


        public ItemsControl? ParentContainerFromItem(ItemsControl parent, DirectoryItem child)
        {
            if (parent.Items.Contains(child))
                return parent;

            foreach (DirectoryItem directoryItem in parent.Items)
            {
                DependencyObject item = parent.ItemContainerGenerator.ContainerFromItem(directoryItem);
                if (item is ItemsControl == false) continue;
                ItemsControl? result = ParentContainerFromItem((ItemsControl)item, child);
                if (result != null) return result;
            }
            return null;
        }


        private async Task LookupVehicles(string path)
        {
            ScanCancellationTokenSource = new CancellationTokenSource();
            var token = ScanCancellationTokenSource.Token;
            IProgress<int> progress = new Progress<int>(value => { ViewModel.VehicleAssetLoadingProgress = value == 0 ? value : Math.Max(value, ViewModel.VehicleAssetLoadingProgress); });
            ViewModel.VehicleScanInProgress = true;
            List<string> files = Directory.GetFiles(path, "*.bin", SearchOption.AllDirectories).ToList();
            List<string> apFiles = Directory.GetFiles(path, "*.ap", SearchOption.AllDirectories).ToList();

            ViewModel.LoadingInformation = Railworker.Language.Resources.scanning_bin_files;
            var startDateTime = DateTime.Now;
            
        }

        public void TreeView_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem tvi = (TreeViewItem)e.OriginalSource;
            DirectoryItem selected = (DirectoryItem)tvi.Header;
            selected.PopulateSubDirectories();

            ItemsControl? parent = ParentContainerFromItem(DirectoryTree, selected);

            int index;
            if (parent is TreeViewItem)
            {
                index = parent.Items.IndexOf(selected);
                if (index >= 0 && index + 1 < parent.Items.Count)
                {
                    TreeViewItem childTvi = (TreeViewItem)parent.ItemContainerGenerator.ContainerFromItem(parent.Items[index + 1]);
                    childTvi.BringIntoView();
                }
            }
            else
            {
                index = DirectoryTree.Items.IndexOf(selected);
                if (index >= 0 && index + 1 < DirectoryTree.Items.Count)
                {
                    TreeViewItem childTvi = (TreeViewItem)DirectoryTree.ItemContainerGenerator.ContainerFromItem(DirectoryTree.Items[index + 1]);
                    childTvi.BringIntoView();
                }
            }
            tvi.BringIntoView();
        }

        private void ConsistsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.ConsistVehicles.Clear();
            ViewModel.SelectedConsist = ConsistsListView.SelectedItem as Consist;
            foreach (var vehicle in (ConsistsListView.SelectedItem as Consist)?.RWConsist.Vehicles ?? [])
            {
                ViewModel.ConsistVehicles.Add(new ConsistVehicle
                {
                    RWConsistVehicle = vehicle
                });
            }
        }
    }
}
