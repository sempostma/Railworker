using Railworker.Pages;
using RWLib;
using RWLib.RWBlueprints.Interfaces;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Railworker.Windows
{
    /// <summary>
    /// Interaction logic for VehicleVariationCreator.xaml
    /// </summary>
    public partial class VehicleVariationCreator : Window
    {
        public class VehicleVariationCreatorViewModel : ViewModel
        {
            private Page? _frameContent;
            public Page? FrameContent { get => _frameContent; set => SetProperty(ref _frameContent, value); }

        }

        public VehicleVariationCreatorViewModel ViewModel;

        public IRWRailVehicleBlueprint? RWBlueprint { get; set; }

        public VehicleVariationCreator(IRWRailVehicleBlueprint rWBlueprint)
        {
            RWBlueprint = rWBlueprint;
            ViewModel = new VehicleVariationCreatorViewModel();
            var page = new VehicleVariationGenerator(RWBlueprint);
            ViewModel.FrameContent = page;
            DataContext = ViewModel;
            InitializeComponent();

        }
    }
}
