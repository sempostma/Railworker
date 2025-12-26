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
using Microsoft.Web.WebView2.Core;
using System.Reflection;
using System.Text.Json;
using RWLib.Tracks;
using System.Collections.Concurrent;
using System.Security.RightsManagement;
using RWLib.RWBlueprints.Components;
using static System.Net.WebRequestMethods;
using Railworker.Core;

namespace Railworker
{
    /// <summary>
    /// Interaction logic for Routes.xaml
    /// </summary>
    public partial class RouteTracksBinVisualizerWindow : Window
    {
        internal App App { get => (App)Application.Current; }
        internal Logger Logger { get => App.Logger; }

        public class ResetMapWebViewMessage
        {
            public string Command { get; set; } = "RESET_MAP";
        }

        public class AddRibbonWebViewMessage
        {
            public string Command { get; set; } = "ADD_FEATURE";
            public GeoJsonAdapter.Feature? Feature { get; internal set; }
        }

        private class RouteInfoMessage
        {
            public string Command { get; set; } = "ROUTE_INFO";
            public string LocalizedName { get; set; } = "";
            public RWDisplayName? DisplayName { get; set; }
            public string Guid { get; set; } = "";
            public RWRouteOrigin? Origin { get; set; }
        }

        public class RouteTracksBinVisualizerWindowViewModel : ViewModel
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

            private int _visualizerProgress = 0;
            public int VisualizerProgress { 
                get => _visualizerProgress;
                set
                {
                    SetProperty(ref _visualizerProgress, value);
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(CombinedLoadingProgress)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(LoadingBarVisible)));
                }
            }

            public int CombinedLoadingProgress
            {
                get => Math.Max(VisualizerProgress, RoutesLoadingProgress);
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

        public RouteTracksBinVisualizerWindowViewModel ViewModel { get; private set; }

        public RouteTracksBinVisualizerWindow()
        {
            ViewModel = new RouteTracksBinVisualizerWindowViewModel();
            DataContext = ViewModel;
            LoadRoutes();
            InitializeComponent();
            InitializeWebviewAsync();
        }

        async void InitializeWebviewAsync()
        {
            await GeoJsonVisualizer.EnsureCoreWebView2Async(null);
            GeoJsonVisualizer.CoreWebView2.AddWebResourceRequestedFilter("https://route_visualizer_leaflet/*", CoreWebView2WebResourceContext.All);
            GeoJsonVisualizer.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            GeoJsonVisualizer.Source = new Uri("https://route_visualizer_leaflet/");
        }

        private string DetermineHeaders(String uri)
        {
            string headers = "";
            if (uri.EndsWith(".html"))
            {
                headers = "Content-Type: text/html";
            }
            else if (uri.EndsWith(".jpg"))
            {
                headers = "Content-Type: image/jpeg";
            }
            else if (uri.EndsWith(".png"))
            {
                headers = "Content-Type: image/png";
            }
            else if (uri.EndsWith(".css"))
            {
                headers = "Content-Type: text/css";
            }
            else if (uri.EndsWith(".js"))
            {
                headers = "Content-Type: application/javascript";
            }
            return headers;
        }

        private void CoreWebView2_WebResourceRequested(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs e)
        {
            string runningPath = AppDomain.CurrentDomain.BaseDirectory;
            var origin = "https://route_visualizer_leaflet/";

            var assembly = Assembly.GetExecutingAssembly();
            var resourcePaths = assembly.GetManifestResourceNames();

            if (e.Request.Uri.StartsWith(origin))
            {
                var requestPath = e.Request.Uri.Substring(origin.Length).Replace('/', '.');

                if (requestPath == "")
                {
                    Stream stream = assembly.GetManifestResourceStream("Railworker.Resources.RouteVisualizerLefalet.html")!;
                    WebView2ManagedStream ms = new WebView2ManagedStream(stream);
                    var headers = "Content-Type: text/html";

                    e.Response = GeoJsonVisualizer.CoreWebView2.Environment.CreateWebResourceResponse(
                        ms, 200, "OK", headers);
                } else if (requestPath.StartsWith("leaflet"))
                {
                    string resourceName = String.Format("Railworker.Resources.{0}", requestPath);
                    Stream stream = assembly.GetManifestResourceStream(resourceName)!;
                    WebView2ManagedStream ms = new WebView2ManagedStream(stream);
                    var headers = DetermineHeaders(requestPath);

                    e.Response = GeoJsonVisualizer.CoreWebView2.Environment.CreateWebResourceResponse(
                        ms, 200, "OK", headers);
                }
                else
                {
                    e.Response = GeoJsonVisualizer.CoreWebView2.Environment.CreateWebResourceResponse(
                                        null, 404, Railworker.Language.Resources.not_found, "");
                }
            } else
            {
                e.Response = GeoJsonVisualizer.CoreWebView2.Environment.CreateWebResourceResponse(
                                                        null, 404, Railworker.Language.Resources.not_found, "");
            }
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

        protected async void ProcessTracksBin()
        {
            var routes = RoutesListView.SelectedItems.Cast<Route>().ToList();
            if (routes.Count() == 0) return;

            IProgress<int> progress = new Progress<int>(value => { ViewModel.VisualizerProgress = value; });
            progress.Report(10);
            ViewModel.LoadingInformation = Railworker.Language.Resources.processing_tracks_bin;

            //GeoJsonVisualizer.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new ResetMapWebViewMessage()));
            var messagesBag = new List<string>();
            var messagesLock = new object();

            var flushQueue = new Action(() =>
            {
                List<string> list;
                lock(messagesLock)
                {
                    list = messagesBag;
                    messagesBag = new List<string>();
                }

                Application.Current.Dispatcher.Invoke(delegate
                {
                    foreach (var item in list)
                    {
                        GeoJsonVisualizer.CoreWebView2.PostWebMessageAsJson(item);
                    }
                });
            });

            await Parallel.ForEachAsync(routes.Select((value, i) => (value, i)), async (item, token) =>
            {
                var index = item.i;
                var route = item.value;
                var tiles = App.RWLib!.TracksBinParser.GetTrackTiles(route.Guid);

                var routeInfoMessage = new RouteInfoMessage
                {
                    LocalizedName = route.Name,
                    DisplayName = route.RWRoute!.DisplayName!,
                    Guid = route.Guid,
                    Origin = route.RWRoute.RouteOrigin
                };

                var geoJsonAdapter = new GeoJsonAdapter(route.RWRoute.RouteOrigin);

                lock (messagesLock)
                {
                    messagesBag.Add(JsonSerializer.Serialize(routeInfoMessage));
                }

                foreach (var tile in tiles)
                {
                    var ribbons = App.RWLib!.TracksBinParser.ProcessTrackTile(tile);

                    await foreach (var ribbon in ribbons)
                    {
                        foreach (var feature in geoJsonAdapter.ProcessRWTrackRibbon(ribbon))
                        {
                            var message = new AddRibbonWebViewMessage
                            {
                                Feature = feature
                            };
                            var json = JsonSerializer.Serialize(message);

                            lock (messagesLock)
                            {
                                messagesBag.Add(json);
                            }

                            if (messagesBag.Count > 100) flushQueue();
                        }
                    }
                }

                progress.Report((int)Math.Ceiling((float)index / routes.Count() * 100));
                token.ThrowIfCancellationRequested();

                flushQueue();
            });

            ViewModel.LoadingInformation = Railworker.Language.Resources.loading_routes;
            ViewModel.VisualizerProgress = 0;
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
            
        }

        private void OpenScenarioDirectory_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EditScenarioButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ProcessTracksBin_Click(object sender, RoutedEventArgs e)
        {
            ProcessTracksBin();
        }


    }
}
