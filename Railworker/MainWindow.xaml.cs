using Microsoft.WindowsAPICodePack.Dialogs;
using Railworker.Pages;
using Railworker.Properties;
using RWLib.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.IO;
using Railworker.Core;
using RWLib;
using RWLib.RWBlueprints.Components;
using RWLib.RWBlueprints.Interfaces;
using Microsoft.Web.WebView2.Core;
using Railworker.Windows;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Railworker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        internal App App { get => (App)Application.Current; }
        internal Logger Logger { get => App.Logger; }


        public class MainWindowViewModel : ViewModel
        {
            public class Tab
            {
                public required string Header { get; set; }
                public required Page FrameContent { get; set; }
            }

            public ObservableCollection<Tab> Tabs { get; set; } = new ObservableCollection<Tab>();

            private string _windowTitle = "Railworker";
            public string WindowTitle { get => _windowTitle; set => SetProperty(ref _windowTitle, value); }
            public string _fileContents = "";
            public string FileContents { get => _fileContents; set => SetProperty(ref _fileContents, value); }
            private bool _fileIsTSBin = false;
            public bool FileIsTSBin { get => _fileIsTSBin; set => SetProperty(ref _fileIsTSBin, value); }
            private bool _fileIsBlueprint = false;
            public bool FileIsBlueprint { get => _fileIsBlueprint; set => SetProperty(ref _fileIsBlueprint, value); }
            public RWBlueprint? _fileBlueprint;
            public RWBlueprint? FileBlueprint { get => _fileBlueprint; set => SetProperty(ref _fileBlueprint, value); }
            private bool _fileBlueprintIsRailVehicle = false;
            public bool FileBlueprintIsRailVehicle
            {
                get => _fileBlueprintIsRailVehicle; 
                set
                {
                    SetProperty(ref _fileBlueprintIsRailVehicle, value);
                    OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("CreateVehicleVariationVisibility"));
                }
            }

            public Visibility CreateVehicleVariationVisibility => FileBlueprintIsRailVehicle ? Visibility.Visible : Visibility.Collapsed;

            public Visibility AddAsConsistButton => ActiveConsist != null ? Visibility.Visible : Visibility.Collapsed;

            public Consist? _activeConsist = null;
            public Consist? ActiveConsist { 
                get => _activeConsist;
                set
                {
                    SetProperty(ref _activeConsist, value);
                    OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("AddAsConsistButton"));
                }
            }
        }

        public MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            ViewModel = new MainWindowViewModel();
            ViewModel.WindowTitle = "Railworker " + Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "";
            DataContext = ViewModel;

            InitializeComponent();
        }

        private void MenuItem_Edit_Settings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().Show();
        }

        private void MenuItem_Scenarios_Show_List_Click(object sender, RoutedEventArgs e)
        {
            OpenTab(new MainWindowViewModel.Tab
            {
                FrameContent = new RoutesAndScenariosWindow(),
                Header = Railworker.Language.Resources.routes_and_scenarios
            });
        }

        private void MenuItem_Routes_Route_Visualizer_Click(object sender, RoutedEventArgs e)
        {
            new RouteTracksBinVisualizerWindow().Show();
        }

        private async void MenuItem_File_Open_File_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.Title = Railworker.Language.Resources.open_file;
                dialog.DefaultDirectory = App.RWLib!.TSPath;
                dialog.RestoreDirectory = true;

                var extensions = string.Join(",", FileEditor.AllowedFileExtensions.Select(x => "*." + x));
                dialog.Filters.Add(new CommonFileDialogFilter("TS Files", extensions));

                var dialogResult = dialog.ShowDialog();
                if (dialogResult != CommonFileDialogResult.Ok)
                {
                    return;
                }
                var path = dialog.FileName;
                if (path == null)
                {
                    return;
                }
                if (!File.Exists(path))
                {
                    Logger.Debug(path + " file does not exist");
                    MessageBox.Show(Railworker.Language.Resources.file_does_not_exist, Railworker.Language.Resources.msg_error, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var tsBase = new Uri(App.RWLib!.TSPath);

                if (tsBase.IsBaseOf(new Uri(path)) == false)
                {
                    Logger.Warning(path + " is outside the TS directory");
                }

                try
                {
                    var result = await RailworkerFiles.OpenFileForTextEditor(path, App.RWLib);
                    ViewModel.FileContents = result.Text;

                    var isTSBin = result.FileContentType == RailworkerFiles.FileContentType.TSBin;
                    var xml = isTSBin ? App.RWLib.Serializer.ParseXMLSafe(result.Text) : null;
                    var isBlueprint = isTSBin && xml != null && xml.Root!.Name == "cBlueprintLoader";
                    var blueprint = isBlueprint ? await App.RWLib.BlueprintLoader.FromFilename(path) : null;
                    var isRailVehicle = blueprint is IRWRailVehicleBlueprint;

                    ViewModel.FileIsTSBin = isTSBin;
                    ViewModel.FileIsBlueprint = isBlueprint;
                    ViewModel.FileBlueprint = blueprint;
                    ViewModel.FileBlueprintIsRailVehicle = isRailVehicle;

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    MessageBox.Show(Railworker.Language.Resources.failed_to_read_file + ": " + ex.ToString(), Railworker.Language.Resources.msg_error, MessageBoxButton.OK, MessageBoxImage.Error);
                    throw ex;
                }

                OpenTab(new MainWindowViewModel.Tab
                {
                    FrameContent = new FileEditor(ViewModel.FileContents, path),
                    Header = System.IO.Path.GetFileName(path)
                });
            }
        }

        private void MenuItem_Edit_Create_Vehicle_Variation(object sender, RoutedEventArgs e)
        {
            OpenTab(new MainWindowViewModel.Tab
            {
                FrameContent = new VehicleVariationGenerator((IRWRailVehicleBlueprint)ViewModel.FileBlueprint!),
                Header = Railworker.Language.Resources.scenario_downloader
            });
        }

        private void MenuItem_Scenarios_Downloader_Click(object sender, RoutedEventArgs e)
        {
            OpenTab(new MainWindowViewModel.Tab
            {
                FrameContent = new ScenarioDownloader(),
                Header = Railworker.Language.Resources.scenario_downloader
            });
        }

        private void MenuItem_RollingStock_RepaintUpdater(object sender, RoutedEventArgs e)
        {
            OpenTab(new MainWindowViewModel.Tab
            {
                FrameContent = new RepaintUpdater(),
                Header = Railworker.Language.Resources.repaint_updater
            });
        }

        public void OpenTab(MainWindowViewModel.Tab tab)
        {
            ViewModel.Tabs.Add(tab);
            TabCtrl.SelectedItem = tab;
        }

        public void CloseTab(Page page)
        {
            var tabs = ViewModel.Tabs.Where(t => t.FrameContent == page).ToArray();
            foreach (var t in tabs)
            {
                ViewModel.Tabs.Remove(t);
            }
        } 

        private void MenuItem_File_RWP_Packager_Click(object sender, RoutedEventArgs e)
        {
            OpenTab(new MainWindowViewModel.Tab
            {
                FrameContent = new RWPPackager(),
                Header = Railworker.Language.Resources.rwp_packager
            });
        }

        private void Tab_Close(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var tab = (MainWindowViewModel.Tab)button.DataContext;
            ViewModel.Tabs.Remove(tab);
        }

        private void MenuItem_File_Save_Click(object sender, RoutedEventArgs e)
        {
            var tab = TabCtrl.SelectedItem as MainWindowViewModel.Tab;
            if (tab == null) return;
            switch(tab.FrameContent)
            {
                case FileEditor fileEditor:
                    {
                        fileEditor.SaveFile();
                    }
                    break;
            }
        }

        private void MenuItem_Edit_Create_Preload(object sender, RoutedEventArgs e)
        {
            OpenTab(new MainWindowViewModel.Tab
            {
                FrameContent = new ConsistCreator(ViewModel.ActiveConsist!),
                Header = Railworker.Language.Resources.add_as_preload_consist
            });
        }

        private void TabCtrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tabContent = TabCtrl.SelectedContent as MainWindowViewModel.Tab;
            if (tabContent == null) return;
            var page = tabContent.FrameContent;

            if (page is RollingStockReplacement)
            {
                var rsr = (RollingStockReplacement)page;
                ViewModel.ActiveConsist = rsr.ViewModel.SelectedConsist;
            } else
            {
                ViewModel.ActiveConsist = null;
            }
            Debug.WriteLine(tabContent);
        }

        private void MenuItem_Management(object sender, RoutedEventArgs e)
        {
            OpenTab(new MainWindowViewModel.Tab
            {
                FrameContent = new CRANAdmin(),
                Header = Railworker.Language.Resources.cran_admin
            });
        }

        private void MenuItem_Preloads(object sender, RoutedEventArgs e)
        {
            OpenTab(new MainWindowViewModel.Tab
            {
                FrameContent = new PreloadsPage(),
                Header = Railworker.Language.Resources.preloads
            });
        }

        private void MenuItem_Graphics_TgPcDxViewer_Click(object sender, RoutedEventArgs e)
        {
            OpenTab(new MainWindowViewModel.Tab
            {
                FrameContent = new TgPcDxViewer(),
                Header = Railworker.Language.Resources.tgpcdx_viewer
            });
        }

        private void MenuItem_Graphics_CompositionEditor_Click(object sender, RoutedEventArgs e)
        {
            OpenTab(new MainWindowViewModel.Tab
            {
                FrameContent = new Compositions(),
                Header = "Random Skins"
            });
        }
        
        private void MenuItem_Graphics_SingleCompositionEditor_Click(object sender, RoutedEventArgs e)
        {
            OpenTab(new MainWindowViewModel.Tab
            {
                FrameContent = new CompositionEditor(),
                Header = "Composition Editor"
            });
        }
        
        private void MenuItem_Graphics_RandomSkins_Click(object sender, RoutedEventArgs e)
        {
            OpenTab(new MainWindowViewModel.Tab
            {
                FrameContent = new Compositions(),
                Header = "Random Skins"
            });
        }

        private void MenuItem_TrainPerformanceCalculator_Click(object sender, RoutedEventArgs e)
        {
            OpenTab(new MainWindowViewModel.Tab
            {
                FrameContent = new TrainPerformanceCalculator(),
                Header = "Train Performance"
            });
        }
    }
}
