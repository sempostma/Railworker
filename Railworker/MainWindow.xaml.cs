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
            private string _windowTitle = "Railworker";
            public string WindowTitle { get => _windowTitle; set => SetProperty(ref _windowTitle, value); }
            private Page? _frameContent;
            public Page? FrameContent { get => _frameContent; set => SetProperty(ref _frameContent, value); }
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
            new RoutesAndScenariosWindow().Show();
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

                ViewModel.FrameContent = new FileEditor(ViewModel.FileContents, path);
            }
        }

        private void MenuItem_Edit_Create_Vehicle_Variation(object sender, RoutedEventArgs e)
        {
            new VehicleVariationCreator((IRWRailVehicleBlueprint)ViewModel.FileBlueprint!).Show();
        }
    }
}
