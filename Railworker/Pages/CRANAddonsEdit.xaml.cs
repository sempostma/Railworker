using ComprehensiveRailworksArchiveNetwork;
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
using static Railworker.Pages.CRANNewAddon;

namespace Railworker.Pages
{
    /// <summary>
    /// Interaction logic for CRANAddonsEdit.xaml
    /// </summary>
    public partial class CRANAddonsEdit : Page
    {
        public class CRANAddonsEditViewModel : ViewModel
        {
            public ObservableCollection<Author> Authors { get; set; } = new ObservableCollection<Author>();

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

            private Addon? _addon;
            public required Addon Addon {
                get => _addon!;
                set
                {
                    SetProperty(ref _addon, value);
                }
            }

            public CRANAddonsEditViewModel()
            {
            }
        }

        CRANAddonsEditViewModel ViewModel;
        private IDriver driver;

        public CRANAddonsEdit(Addon addon)
        {
            driver = ((App)App.Current).AppGlobals.CRANDriver;
            ViewModel = new CRANAddonsEditViewModel
            {
                Addon = addon,
            };
            DataContext = ViewModel;
            LoadAuthors();
            InitializeComponent();
        }

        private async void LoadAuthors()
        {
            // Simulate fetching authors, replace with actual data access code
            ViewModel.Authors.Clear();
            ViewModel.LoadingProgress = 1;
            ViewModel.LoadingInformation = Railworker.Language.Resources.loading_authors;
            var searchTask = driver.SearchForAuthors("", new ComprehensiveRailworksArchiveNetwork.Drivers.SearchOptions { });

            await foreach (var author in searchTask)
            {
                if (author == null) continue;

                ViewModel.Authors.Add(author);
            }
            ViewModel.LoadingProgress = 0;
        }
    }
}
