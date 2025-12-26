using ComprehensiveRailworksArchiveNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Railworker.Pages
{
    /// <summary>
    /// Interaction logic for CRANAdmin.xaml
    /// </summary>
    public partial class CRANAdmin : Page
    {
        private IDriver driver;

        public CRANAdmin()
        {
            driver = ((App)App.Current).AppGlobals.CRANDriver;

            InitializeComponent();
        }

        private void BtnAddons_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)App.Current.MainWindow).OpenTab(new MainWindow.MainWindowViewModel.Tab
            {
                FrameContent = new CRANAddonsPage(),
                Header = Railworker.Language.Resources.cran_addons_page
            });
        }

        private void BtnFiles_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnAuthors_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
