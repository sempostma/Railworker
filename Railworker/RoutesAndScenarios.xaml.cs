using Microsoft.VisualBasic.Logging;
using RWLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Railworker.Core;

namespace Railworker
{
    /// <summary>
    /// Interaction logic for Routes.xaml
    /// </summary>
    public partial class RoutesAndScenariosWindow : Window
    {
        internal App App { get => (App)Application.Current; }
        internal Logger Logger { get => App.Logger; }

        public class RoutesAndScenariosWindowViewModel : ViewModel
        {
            public ObservableCollection<Route> Routes { get; } = new ObservableCollection<Route>();
            public ObservableCollection<Scenario> Scenarios { get; } = new ObservableCollection<Scenario>();

            private int _routesLoadingProgress = 0;
            public int RoutesLoadingProgress { 
                get => _routesLoadingProgress; 
                set
                {
                    SetProperty(ref _routesLoadingProgress, value);
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(CombinedLoadingProgress)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(LoadingBarVisible)));
                }
            }

            private int _scenariosLoadingProgress = 0;
            public int ScenariosLoadingProgress { 
                get => _scenariosLoadingProgress;
                set
                {
                    SetProperty(ref _scenariosLoadingProgress, value);
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(CombinedLoadingProgress)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(LoadingBarVisible)));
                }
            }

            public int CombinedLoadingProgress
            {
                get => Math.Max(ScenariosLoadingProgress, RoutesLoadingProgress);
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
        }

        public RoutesAndScenariosWindowViewModel ViewModel { get; private set; }

        public RoutesAndScenariosWindow()
        {
            ViewModel = new RoutesAndScenariosWindowViewModel();
            DataContext = ViewModel;
            LoadRoutes();
            InitializeComponent();
        }

        protected void LoadRoutes()
        {
            IProgress<int> progress = new Progress<int>(value => { ViewModel.RoutesLoadingProgress = value; });
            ViewModel.Routes.Clear();
            ViewModel.LoadingInformation = Railworker.Language.Resources.loading_routes;

            Task.Run(async () =>
            {
                try
                {
                    var rwRoutes = App.RWLib!.RouteLoader.LoadRoutes();

                    int counter = 0;

                    var routes = new List<Route>();

                    await foreach (var route in Route.FromRWRoutes(rwRoutes))
                    {
                        routes.Add(route);
                        progress.Report(100 - (int)Math.Round(5.0 / ++counter * 100.0));
                    }

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var route in routes)
                        {
                            try
                            {
                                route.PropertyChanged += Route_PropertyChanged;
                                ViewModel.Routes.Add(route);
                            }
                            catch (Exception)
                            {

                            }
                        }
                    });

                    Logger.Debug($"{counter} routes loaded");

                    progress.Report(0);
                } catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        MessageBox.Show(
                        ex.Message,
                        Railworker.Language.Resources.msg_message,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

        protected void LoadScenarios()
        {
            if (RoutesListView.SelectedItem == null) return;
            Route route = (RoutesListView.SelectedItem as Route)!;

            IProgress<int> progress = new Progress<int>(value => { ViewModel.ScenariosLoadingProgress = value; });
            ViewModel.LoadingInformation = Railworker.Language.Resources.loading_scenarios;
            ViewModel.Scenarios.Clear();

            Task.Run(async () =>
            {
                try
                {
                    int counter = 0;

                    var rwScenarios = App.RWLib!.RouteLoader.LoadScenarios(route.Guid);
                    var scenarios = new List<Scenario>();

                    await foreach (var scenario in Scenario.FromRWScenarios(rwScenarios))
                    {
                        scenarios.Add(scenario);
                        progress.Report(100 - (int)Math.Round(5.0 / ++counter * 100.0));
                    }

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var scenario in scenarios)
                        {
                            try
                            {
                                scenario.PropertyChanged += Scenario_PropertyChange;
                                ViewModel.Scenarios.Add(scenario);
                            }
                            catch (Exception)
                            {

                            }
                        }
                    });
                } catch(Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        MessageBox.Show(
                        ex.Message,
                        Railworker.Language.Resources.msg_message,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }

                progress.Report(0);
            });
        }

        private void Scenario_PropertyChange(object? sender, PropertyChangedEventArgs e)
        {
        }

        private void Route_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsFavorite")
            {
                Route route = (sender as Route)!;
                StringCollection favorite = Properties.Settings.Default.FavoriteRoutes ?? new StringCollection();
                if (Properties.Settings.Default.FavoriteRoutes == null) Properties.Settings.Default.FavoriteRoutes = favorite;
                if (route.IsFavorite)
                {
                    Logger.Debug($"Adding {route.Name} to favorite..");
                    favorite.Add(route.Guid);
                }
                else
                {
                    Logger.Debug($"Removing {route.Name} from favorite..");
                    favorite.Remove(route.Guid);
                }
                Properties.Settings.Default.Save();
            }
        }

        private void RoutesListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            LoadScenarios();
        }

        private void OpenScenarioDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (ScenariosListView.SelectedItem == null) return;
            Scenario scenario = (ScenariosListView.SelectedItem as Scenario)!;

            var dir = Path.Combine(App.RWLib!.TSPath, "Content", "Routes", scenario.RouteGuid, "Scenarios", scenario.Guid);
            System.Diagnostics.Process.Start("explorer.exe", dir);
        }

        private void EditScenarioButton_Click(object sender, RoutedEventArgs e)
        {
            if (ScenariosListView.SelectedItem == null) return;
            Scenario scenario = (ScenariosListView.SelectedItem as Scenario)!;
            new RollingStockReplacement(scenario).Show();
        }
    }
}
