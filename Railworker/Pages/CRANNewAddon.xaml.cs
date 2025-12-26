using ComprehensiveRailworksArchiveNetwork;
using RWLib.Scenario;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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

namespace Railworker.Pages
{
    /// <summary>
    /// Interaction logic for CRANNewAddon.xaml
    /// </summary>
    public partial class CRANNewAddon : Page
    {
        public class CRANNewAddonViewModel : ViewModel
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

            public CRANNewAddonViewModel()
            {
            }

            private void Addons_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {

            }
        }

        CRANNewAddonViewModel ViewModel;
        private IDriver driver;

        public CRANNewAddon()
        {
            driver = ((App)App.Current).AppGlobals.CRANDriver;
            ViewModel = new CRANNewAddonViewModel();
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

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cmbAuthor.SelectedItem as Author == null) return;

            // Implement saving logic here
            // Validate input, create an addon object, and save it
            Addon newAddon = new Addon
            {
                Era = AddonEra.IV,
                Type = AddonType.Other,
                Variants = new List<AddonVariant>
                {
                    new AddonVariant
                    {
                        Description = "Default",
                        Guid = Guid.NewGuid(),
                        Label = "Default",
                        Versions = new List<AddonVersion>
                        {
                            new AddonVersion
                            {
                                PendingApproval = false,
                                Changes = new List<string>(),
                                Dependencies = new List<Dependency>(),
                                FileList = txtFiles.Text.Split("\r\n").ToList(),
                                InstallerFiles = new List<ExeFile>(),
                                PostInstallationTask = new List<ComprehensiveRailworksArchiveNetwork.Tasks.InstallationTask>(),
                                PreInstallationTask = new List<ComprehensiveRailworksArchiveNetwork.Tasks.InstallationTask>(),
                                ReadmeFiles = new List<ReadmeFile>(),
                                RWPFiles = new List<RWPFile>(),
                                Submitted = true,
                                Url = txtUrl.Text,
                                VersionNumber = new VersionNumber
                                {
                                    Major = 1,
                                    Minor = 0,
                                    Patch = 0
                                }
                            }
                        }
                    }
                },
                Credits = new List<Collaborator>(),
                Guid = Guid.NewGuid(),
                Name = txtName.Text,
                Description = txtDescription.Text,
                Author = (Author)cmbAuthor.SelectedItem,
                IsOptional = chkOptional.IsChecked ?? false
            };

            // Assume a method to save this addon
            newAddon = await driver.SaveAddon(newAddon);
            ((MainWindow)App.Current.MainWindow).OpenTab(new MainWindow.MainWindowViewModel.Tab
            {
                FrameContent = new CRANAddonsEdit(newAddon),
                Header = Railworker.Language.Resources.cran_addon_edit
            });
            ((MainWindow)App.Current.MainWindow).CloseTab(this);
            MessageBox.Show("Addon saved successfully!");
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)App.Current.MainWindow).CloseTab(this);
        }
    }
}
