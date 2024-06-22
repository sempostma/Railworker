using Railworker.Core;
using Railworker.Properties;
using RWLib;
using RWLib.Interfaces;
using RWLib.RWBlueprints.Components;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Railworker.UserControls
{
    /// <summary>
    /// Interaction logic for AssetExplorer.xaml
    /// </summary>
    public partial class AssetExplorer : UserControl
    {
        public class Asset
        {
            public required RWBlueprint Blueprint { get; set; }
            public required string Name { get; set; }
        }

        internal App App { get => (App)Application.Current; }
        internal Logger Logger { get => App.Logger; }

        public Func<RWBlueprint, IEnumerable<Asset>> Mapper { get; set; } = b => new[] { new Asset { Blueprint = b, Name = System.IO.Path.GetFileName(b.BlueprintIDPath.ToString()) } };

        public class AssetExplorerViewModel : ViewModel
        {
            public Visibility ClearButtonVisibility => !AssetLoadingInProgress && AvailableBlueprints.Count > 0 ? Visibility.Visible : Visibility.Hidden;

            public ObservableCollection<Asset> AvailableBlueprints { get; } = new ObservableCollection<Asset>();

            private string _searchText = "";
            public String SearchText
            {
                get => _searchText;
                set
                {
                    SetProperty(ref _searchText, value);
                }
            }
            public SelectionMode SelectionMode { get; set; } = SelectionMode.Single;

            private int _assetLoadingProgress = 0;
            public int AssetLoadingProgress
            {
                get => _assetLoadingProgress;
                set
                {
                    SetProperty(ref _assetLoadingProgress, value);
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(CombinedLoadingProgress)));
                }
            }

            private bool _assetLoadingInProgress = false;
            public bool AssetLoadingInProgress
            {
                get => _assetLoadingInProgress;
                set
                {
                    SetProperty(ref _assetLoadingInProgress, value);
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(ClearButtonVisibility)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(LoadingBarVisible)));
                }
            }

            public int CombinedLoadingProgress
            {
                get => Math.Max(0, AssetLoadingProgress);
            }

            public Visibility LoadingBarVisible
            {
                get => AssetLoadingInProgress ? Visibility.Visible : Visibility.Hidden;
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

            public AssetExplorerViewModel()
            {
                AvailableBlueprints.CollectionChanged += AvailableVehicles_CollectionChanged;
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

        public AssetExplorerViewModel ViewModel { get; private set; }
        private string SearchText;
        private CancellationTokenSource? ScanCancellationTokenSource;
        private LogShortcut Log;
        private Action RefreshAvailableVehiclesDebounced;

        public AssetExplorer()
        {
            InitializeComponent();
            SearchText = "";
            ViewModel = new AssetExplorerViewModel();
            DataContext = ViewModel;
            Log = new LogShortcut(App.Logger);

            RefreshAvailableVehiclesDebounced = Utilities.Debounce(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    AvailableBlueprints.Items.Refresh();
                });
            });
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
            public void Error(params string[] args)
            {
                logger?.Log(RWLogType.Error, String.Format(args[0], args.Skip(1)));
            }
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            _ = Lookup();
        }

        private async Task Lookup()
        {
            try
            {
                ViewModel.AssetLoadingInProgress = true;
                DirectoryItem? selected = FilesTreeView.DirectoryTree.SelectedItem as DirectoryItem;
                if (selected == null)
                {
                    MessageBox.Show(
                        Railworker.Language.Resources.msg_no_directory_selected,
                        Railworker.Language.Resources.msg_message,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                ViewModel.AssetLoadingProgress = 1;
                IProgress<int> progress = new Progress<int>(a => ViewModel.AssetLoadingProgress = a);
                ScanCancellationTokenSource = new CancellationTokenSource();
                var results = App.RWLib!.BlueprintLoader.ScanDirectory(selected.Path, progress, ScanCancellationTokenSource);

                await foreach (var item in results)
                {
                    var assets = Mapper(item);
                    if (assets.Count() == 0) continue;
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        foreach (var asset in assets)
                        {
                            ViewModel.AvailableBlueprints.Add(asset);
                        }
                    });
                }

                progress.Report(0);

                Application.Current.Dispatcher.Invoke(delegate
                {
                    ViewModel.AssetLoadingInProgress = false;
                });
            } catch(Exception ex)
            {
                Log.Error(ex.ToString());
                Application.Current.Dispatcher.Invoke(delegate
                {
                    MessageBox.Show(
                        ex.Message,
                        Railworker.Language.Resources.msg_message,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                });
            }
        }

        private void CancelScanningButton_Click(object sender, RoutedEventArgs e)
        {
            if (ScanCancellationTokenSource == null) return;
            ScanCancellationTokenSource.Cancel();
        }

        private void AvailableVehiclesClearButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AvailableBlueprints.Clear();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchText = AvailableBlueprintsSearch.Text;
            if (!IsInitialized) return;
            RefreshAvailableVehiclesDebounced();
        }

        private void CollectionViewSource_Filter(object sender, System.Windows.Data.FilterEventArgs e)
        {
            Asset item = (Asset)e.Item;
            if (String.IsNullOrWhiteSpace(SearchText) || SearchText == Railworker.Language.Resources.search)
            {
                e.Accepted = true;
                return;
            }
            e.Accepted = item.Name.ToLower().Contains(SearchText.ToLower());
        }
    }
}
