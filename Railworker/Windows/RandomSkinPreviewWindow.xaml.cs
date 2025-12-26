using RWLib;
using RWLib.Graphics;
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

namespace Railworker.Pages
{
    /// <summary>
    /// Interaction logic for RandomSkinPreviewWindow.xaml
    /// </summary>
    public partial class RandomSkinPreviewWindow : Window
    {
        private RandomSkin _randomSkin;
        private List<Composition> _compositions;
        private RWLibrary _rwLib;

        public string RandomSkinName => _randomSkin?.Name ?? "Unknown";

        public RandomSkinPreviewWindow(RandomSkin randomSkin, List<Composition> compositions, RWLibrary rwLib)
        {
            InitializeComponent();
            _randomSkin = randomSkin;
            _compositions = compositions;
            _rwLib = rwLib;
            DataContext = this;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
