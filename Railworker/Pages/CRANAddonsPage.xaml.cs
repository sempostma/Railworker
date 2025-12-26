using ComprehensiveRailworksArchiveNetwork;
using Railworker.Language;
using Railworker.Windows;
using RWLib.RWBlueprints.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using static Railworker.Windows.RepaintUpdater;

namespace Railworker.Pages
{
    /// <summary>
    /// Interaction logic for CRANAddonsPage.xaml
    /// </summary>
    public partial class CRANAddonsPage : Page
    {
        public class CranAddonsViewModel : ViewModel
        {
            public ObservableCollection<Addon> Addons { get; set; } = new ObservableCollection<Addon>();

            private int _loadingProgress = 0;
            public int LoadingProgress
            {
                get => _loadingProgress;
                set
                {
                    SetProperty(ref _loadingProgress, value);
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(LoadingBarVisible)));
                }
            }

            public Visibility LoadingBarVisible
            {
                get => LoadingProgress > 0 ? Visibility.Visible : Visibility.Hidden;
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

            public CranAddonsViewModel()
            {
                Addons.CollectionChanged += Addons_CollectionChanged; ;
            }

            private void Addons_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                
            }
        }

        CranAddonsViewModel ViewModel = new CranAddonsViewModel();
        private IDriver driver;

        public CRANAddonsPage()
        {
            ViewModel = new CranAddonsViewModel();
            DataContext = ViewModel;
            driver = ((App)App.Current).AppGlobals.CRANDriver;
            InitializeComponent();
        }

        public async void LoadAddons()
        {
            ViewModel.Addons.Clear();
            ViewModel.LoadingProgress = 1;
            ViewModel.LoadingInformation = Railworker.Language.Resources.searching_for_addons;
            var searchTask = driver.SearchForAddons("", new ComprehensiveRailworksArchiveNetwork.Drivers.SearchOptions { });

            await foreach(var addon in searchTask)
            {
                if (addon == null) continue;

                ViewModel.Addons.Add(addon);
            }
            ViewModel.LoadingProgress = 0;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)App.Current.MainWindow).OpenTab(new MainWindow.MainWindowViewModel.Tab
            {
                FrameContent = new CRANNewAddon(),
                Header = Railworker.Language.Resources.cran_new_addon
            });
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadAddons();
        }
    }
}
