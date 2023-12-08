﻿using System.Collections.ObjectModel;
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

namespace Railworker
{
    /// <summary>
    /// Interaction logic for RollingStockReplacement.xaml
    /// </summary>
    public partial class RollingStockReplacement : Window
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
            var populateDirectoryTask = Task.Run(() =>
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
            });

            await Task.WhenAll(readConsistsTask, populateDirectoryTask);
            progress.Report(0);
        }

        public void DirectoryTree_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
        }

        public ItemsControl? ParentContainerFromItem(ItemsControl parent, DirectoryItem child)
        {
            if (parent.Items.Contains(child))
                return parent;

            foreach (DirectoryItem directoryItem in parent.Items)
            {
                DependencyObject item = parent.ItemContainerGenerator.ContainerFromItem(directoryItem);
                if (item is ItemsControl == false) continue;
                ItemsControl? result = ParentContainerFromItem(item as ItemsControl, child);
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
    }
}