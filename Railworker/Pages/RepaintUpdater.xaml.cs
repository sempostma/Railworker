using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.WindowsAPICodePack.Dialogs;
using Railworker.Core;
using RWLib;
using RWLib.Exceptions;
using RWLib.RWBlueprints.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Railworker.Windows
{
    /// <summary>
    /// Interaction logic for RepaintUpdater.xaml
    /// </summary>
    public partial class RepaintUpdater : Page
    {
        public class RepaintUpdaterViewModel : ViewModel
        {
            public bool ReadyToUpdate
            {
                get => !String.IsNullOrWhiteSpace(TemplateBinPath) 
                    && !String.IsNullOrWhiteSpace(RepaintSearchDir)
                    && Blueprints.Count > 0;

            }
            private string _templateBinPath = "";
            public string TemplateBinPath
            {
                get => _templateBinPath;
                set {
                    SetProperty(ref _templateBinPath, value);
                    OnPropertyChanged(new PropertyChangedEventArgs("ReadyToUpdate"));
                }
            }

            public ObservableCollection<IRWRailVehicleBlueprint> Blueprints { get; set; } = new ObservableCollection<IRWRailVehicleBlueprint>();

            private string _repaintSearchDir = "";
            public string RepaintSearchDir
            {
                get => _repaintSearchDir;
                set
                {
                    SetProperty(ref _repaintSearchDir, value);
                    OnPropertyChanged(new PropertyChangedEventArgs("ReadyToUpdate"));
                }
            }

            private int _downloadingProgress = 0;
            public int DownloadingProgress
            {
                get => _downloadingProgress;
                set
                {
                    SetProperty(ref _downloadingProgress, value);
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(CombinedLoadingProgress)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(LoadingBarVisible)));
                }
            }

            private int _visualizerProgress = 0;
            public int VisualizerProgress
            {
                get => _visualizerProgress;
                set
                {
                    SetProperty(ref _visualizerProgress, value);
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(CombinedLoadingProgress)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(LoadingBarVisible)));
                }
            }

            public int CombinedLoadingProgress
            {
                get => Math.Max(VisualizerProgress, DownloadingProgress);
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

            public RepaintUpdaterViewModel()
            {
                Blueprints.CollectionChanged += Blueprints_CollectionChanged;
            }

            private void Blueprints_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                OnPropertyChanged(new PropertyChangedEventArgs("ReadyToUpdate"));
            }
        }

        class KeepOrUpdateSettings
        {
            public bool AlwaysKeep { get; set; }
            public bool AlwaysUpdate { get; set; }
        }

        internal App App { get => (App)Application.Current; }
        internal Logger Logger { get => App.Logger; }

        RepaintUpdaterViewModel ViewModel;
        RepaintUpdaterPrompt Prompt;

        UpdateRepaintsJob? UpdaterJob;

        public RepaintUpdater()
        {
            ViewModel = new RepaintUpdaterViewModel();
            DataContext = ViewModel;
            Prompt = new RepaintUpdaterPrompt();
            InitializeComponent();
        }

        private List<IRWRailVehicleBlueprint> ToList(System.Collections.IList selectedItems)
        {
            var result = new List<IRWRailVehicleBlueprint>(selectedItems.Count);
            foreach (var item in selectedItems)
            {
                result.Add((IRWRailVehicleBlueprint)item);
            }
            return result;  
        }

        private async void UpdateRepaints_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var templateBlueprint = await App.RWLib!.BlueprintLoader.FromFilename(ViewModel.TemplateBinPath);

                UpdaterJob = new UpdateRepaintsJob(App.RWLib!, Prompt, (IRWRailVehicleBlueprint)templateBlueprint, ToList(BlueprintsList.SelectedItems));
            } catch(Exception ex)
            {
                Logger.Error(ex);
                MessageBox.Show(Railworker.Language.Resources.msg_error + ": " + ex.ToString(), Railworker.Language.Resources.msg_error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void FindRepaints_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var templateBlueprint = await App.RWLib!.BlueprintLoader.FromFilename(ViewModel.TemplateBinPath);

                var progress = new Progress<int>();
                var cancellationToken = new CancellationTokenSource();

                ViewModel.Blueprints.Clear();

                await foreach (var item in App.RWLib!.BlueprintLoader.ScanDirectory(ViewModel.RepaintSearchDir, progress, cancellationToken))
                {
                    // only allow same rolling stock type
                    if (item == null || item.XMLElementName != templateBlueprint.XMLElementName) continue;
                    if (item.BlueprintId.ToString() == templateBlueprint.BlueprintId.ToString()) continue;
                    ViewModel.Blueprints.Add((IRWRailVehicleBlueprint)item);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                MessageBox.Show(Railworker.Language.Resources.msg_error + ": " + ex.ToString(), Railworker.Language.Resources.msg_error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string Browse(bool isFolderPicker)
        {
            var dialog = new CommonOpenFileDialog();

            dialog.Title = Railworker.Language.Resources.browse;
            dialog.IsFolderPicker = isFolderPicker;
            try
            {
                dialog.DefaultDirectory = ((App)App.Current).RWLib.TSPath;
            }
            catch (TSPathInRegistryNotFoundException ex)
            {
                Logger.Debug(ex.Message!);
            }

            var result = dialog.ShowDialog();
            if (result != CommonFileDialogResult.Ok)
            {
                return "";
            }
            var path = dialog.FileName;
            if (path == null)
            {
                return "";
            }
            return path;
        }

        private void RepaintSearchDir_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RepaintSearchDir = Browse(true);
        }

        private void BrowseTemplateBinPath_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TemplateBinPath = Browse(false);
        }
    }
}
