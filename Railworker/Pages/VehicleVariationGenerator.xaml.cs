using Railworker.Core;
using RWLib.RWBlueprints.Components;
using RWLib.RWBlueprints.Interfaces;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Railworker.Pages
{
    /// <summary>
    /// Interaction logic for VehicleVariationGenerator.xaml
    /// </summary>
    public partial class VehicleVariationGenerator : Page
    {
        public class VehicleVariationGeneratorViewModel : ViewModel
        {
            public RWDisplayName? _displayName = null;
            public RWDisplayName? DisplayName {get => _displayName; set => SetProperty(ref _displayName, value); }

            public string SuggestedDisplayName => Utilities.DetermineDisplayName(Blueprint.DisplayName);

            public required IRWRailVehicleBlueprint Blueprint { get;  set; }

            public string _filename = "";
            public string Filename { get => _filename; set => SetProperty(ref _filename, value); }

            public string SuggestedFilename => Path.GetFileName(Blueprint.BlueprintId.Path) + ".bin";

            public string _directory = "";
            public string Directory { get => _filename; set => SetProperty(ref _filename, value); }
        }

        public VehicleVariationGeneratorViewModel ViewModel;

        public VehicleVariationGenerator(IRWRailVehicleBlueprint Blueprint)
        {
            ViewModel = new VehicleVariationGeneratorViewModel
            {
                Blueprint = Blueprint,
                DisplayName = Blueprint.DisplayName
            };
            DataContext = ViewModel;
            InitializeComponent();

            ViewModel.DisplayName = Blueprint.DisplayName;
        }
    }
}
