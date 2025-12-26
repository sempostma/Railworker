﻿﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit;
using Railworker.Core;
using RWLib;
using System.Xml.Linq;
using Railworker.UserControls;
using static RWLib.RWLibraryDependent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using RWLib.RWBlueprints;
using RWLib.Graphics;
using RWLib.Interfaces;
using System.Collections.Specialized;
using static Railworker.UserControls.AssetExplorer;
using System.IO;
using System.Threading.Tasks.Dataflow;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using MS.WindowsAPICodePack.Internal;
using System.Text.Json;
using Microsoft.Win32;

namespace Railworker.Pages
{
    /// <summary>
    /// Interaction logic for TgPcDxViewer.xaml
    /// </summary>
    public partial class TgPcDxViewer : Page
    {
        public class Dependency
        {
            public required string Provider { get; set; }
            public required string Product { get; set; }
        }

        public class PreloadEntry
        {
            public required RWXml Entry { get; set; }
            public string Name
            {
                get
                {
                    return Entry.XMLElementName;
                }
            }
        }

        private LogShortcut Log;
        public TgPcDxViewerViewModel ViewModel { get; set; }

        public class TgpcdxImageFile : ViewModel
        {
            private static TgpcdxDecoder decoder = new TgpcdxDecoder();

            private BitmapImage? _imageSource;
            private bool _isLoading = false;

            public required string Name { get; set; }
            public required string Path { get; set; }

            public TgPcDxFile? Tgpcdx { get; private set; } = null;
            public Image<Rgba32>? Image { get; private set; } = null;

            public BitmapImage? ImageSource
            {
                get => _imageSource;
                private set => SetProperty(ref _imageSource, value);
            }

            public bool IsLoading
            {
                get => _isLoading;
                private set => SetProperty(ref _isLoading, value);
            }

            public async Task LoadImageAsync()
            {
                if (_imageSource != null) return; // Avoid reloading

                await Task.Run(async () =>
                {
                    IsLoading = true;

                    var app = (Application.Current as App);

                    var xml = await app!.RWLib!.Serializer.Deserialize(Path);
                    Tgpcdx = new TgPcDxFile(xml, app.RWLib);
                    Image = decoder.Decode(Tgpcdx);

                    using var stream = new MemoryStream();
                    await Image.SaveAsPngAsync(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Ensures image is fully loaded
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze(); // Important: Freeze to allow cross-thread access

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ImageSource = bitmap;
                        IsLoading = false;
                    });
                });
            }
        }

        private string SearchText;
        private CancellationTokenSource? ScanCancellationTokenSource;
        private Action RefreshAvailableVehiclesDebounced;

        private TgpcdxDecoder decoder = new TgpcdxDecoder();

        public class TgPcDxViewerViewModel : ViewModel
        {
            public required string Filename { get; set; }
            public required string Directory { get; set; }

            public ObservableCollection<TgpcdxImageFile> TgPcDxEntries { get; set; } = new ObservableCollection<TgpcdxImageFile>();

            private int _tgPcDxLoadingProgress = 0;
            public int PreloadLoadingProgress
            {
                get => _tgPcDxLoadingProgress;
                set
                {
                    SetProperty(ref _tgPcDxLoadingProgress, value);
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(CombinedLoadingProgress)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(LoadingBarVisible)));
                }
            }

            public int CombinedLoadingProgress
            {
                get => Math.Max(PreloadLoadingProgress, 0);
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

            private bool _scanInProgress = false;
            public bool ScanInProgress
            {
                get => _scanInProgress;
                set
                {
                    SetProperty(ref _scanInProgress, value);
                }
            }

            public Visibility ClearButtonVisibility => !AssetLoadingInProgress && TgPcDxEntries.Count > 0 ? Visibility.Visible : Visibility.Hidden;

            private string _searchText = "";
            public String SearchText
            {
                get => _searchText;
                set
                {
                    SetProperty(ref _searchText, value);
                }
            }
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

            private double _zoomLevel = 1.0;
            public double ZoomLevel
            {
                get => _zoomLevel;
                set
                {
                    SetProperty(ref _zoomLevel, value);
                }
            }

            private bool _hasSelectedItems = false;
            public bool HasSelectedItems
            {
                get => _hasSelectedItems;
                set
                {
                    SetProperty(ref _hasSelectedItems, value);
                }
            }

            public TgPcDxViewerViewModel()
            {
                //AvailableBlueprints.CollectionChanged += AvailableVehicles_CollectionChanged;
            }
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

                await Task.Run(() =>
                {
                    var enumeration = Directory.EnumerateFiles(selected.Path, "*.TgPcDx", SearchOption.AllDirectories);

                    foreach (var file in enumeration)
                    {
                        if (ScanCancellationTokenSource.IsCancellationRequested) break;
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            ViewModel.TgPcDxEntries.Add(new TgpcdxImageFile
                            {
                                Name = System.IO.Path.GetFileName(file),
                                Path = file
                            });
                        });
                    }
                });

                progress.Report(0);

                Application.Current.Dispatcher.Invoke(delegate
                {
                    ViewModel.AssetLoadingInProgress = false;
                });
            }
            catch (Exception ex)
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
                ViewModel.AssetLoadingInProgress = false;
            }
        }

        private void CancelScanningButton_Click(object sender, RoutedEventArgs e)
        {
            if (ScanCancellationTokenSource == null) return;
            ScanCancellationTokenSource.Cancel();
        }

        private void AvailableVehiclesClearButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TgPcDxEntries.Clear();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchText = AvailableBlueprintsSearch.Text;
            if (!IsInitialized) return;
            RefreshAvailableVehiclesDebounced();
        }

        private void CollectionViewSource_Filter(object sender, System.Windows.Data.FilterEventArgs e)
        {
            TgpcdxImageFile item = (TgpcdxImageFile)e.Item;
            if (String.IsNullOrWhiteSpace(SearchText) || SearchText == Railworker.Language.Resources.search)
            {
                e.Accepted = true;
                return;
            }
            e.Accepted = item.Name.ToLower().Contains(SearchText.ToLower());
        }

        internal App App { get => (App)Application.Current; }
        internal Logger Logger { get => App.Logger; }

        public static string[] AllowedFileExtensions { get; } = RailworkerFiles.AllowedTextEditorFileExtensions;

        public TgPcDxViewer(string fileContents, string filename)
        {
            var ext = System.IO.Path.GetExtension(filename).TrimStart('.');
            ViewModel = new TgPcDxViewerViewModel
            {
                Filename = filename,
                Directory = App.RWLib!.TSPath
            };
            Init();
        }

        public TgPcDxViewer()
        {
            DataContext = ViewModel;
            ViewModel = new TgPcDxViewerViewModel
            {
                Filename = null,
                Directory = App.RWLib!.TSPath
            };
            Init();
        }

        private void Init()
        {
            InitializeComponent();

            SearchText = "";
            DataContext = ViewModel;
            Log = new LogShortcut(App.Logger);

            RefreshAvailableVehiclesDebounced = Utilities.Debounce(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    AvailableBlueprints.Items.Refresh();
                });
            });

            DataContext = ViewModel;
            Log = new LogShortcut(App.Logger);
        }

        public Task SaveFile()
        {
            return Task.Run(async () =>
            {
                //var xml = XDocument.Parse(ViewModel.FileContents);
                //var temporaryFile = await App.RWLib!.Serializer.SerializeWithSerzExe(xml);

                //string args = string.Format("/e, /select, \"{0}\"", temporaryFile);

                //ProcessStartInfo info = new ProcessStartInfo();
                //info.FileName = "explorer";
                //info.Arguments = args;
                //Process.Start(info);
            });
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TgPcDxListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Image image && image.DataContext is TgpcdxImageFile lazyImage)
            {
                var _ = lazyImage.LoadImageAsync();
            }
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // The binding will automatically update the ViewModel.ZoomLevel property
            // This event handler can be used for additional logic if needed
            if (AvailableBlueprints == null) return;
            AvailableBlueprints.Items.Refresh();
        }

        private void AvailableBlueprints_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AvailableBlueprints == null) return;
            ViewModel.HasSelectedItems = AvailableBlueprints.SelectedItems.Count > 0;
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (AvailableBlueprints.SelectedItems.Count == 0) return;

            // Show a loading indicator
            ViewModel.AssetLoadingInProgress = true;
            
            try
            {
                // Create a list to hold the file information
                var selectedFiles = new List<Dictionary<string, object?>>();
                
                // Process each selected file to gather metadata
                foreach (TgpcdxImageFile item in AvailableBlueprints.SelectedItems)
                {
                    // Get basic file info
                    var fileInfo = new FileInfo(item.Path);
                    
                    // Create a dictionary to hold all the file metadata
                    var fileData = new Dictionary<string, object?>
                    {
                        { "filename", item.Path },
                        { "filesize", fileInfo.Length },
                        { "width", item?.Tgpcdx?.Width },
                        { "height", item?.Tgpcdx?.Height },
                        { "name", item?.Tgpcdx?.Name },
                    };
                    
                    selectedFiles.Add(fileData);
                }

                // Create a SaveFileDialog to let the user choose where to save the JSON file
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json",
                    Title = "Export Selected Files",
                    DefaultExt = "json",
                    AddExtension = true
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Serialize the list to JSON
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    string jsonString = JsonSerializer.Serialize(selectedFiles, options);

                    // Write the JSON to the selected file
                    File.WriteAllText(saveFileDialog.FileName, jsonString);

                    MessageBox.Show(
                        $"Successfully exported {selectedFiles.Count} file(s) to {saveFileDialog.FileName}",
                        "Export Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error exporting files: {ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                // Hide the loading indicator
                ViewModel.AssetLoadingInProgress = false;
            }
        }
    }
}
