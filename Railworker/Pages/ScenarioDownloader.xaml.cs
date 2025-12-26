using Railworker.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Railworker.Pages
{
    /// <summary>
    /// Interaction logic for ScenarioDownloader.xaml
    /// </summary>
    public partial class ScenarioDownloader : Page
    {
        internal App App { get => (App)Application.Current; }
        internal Logger Logger { get => App.Logger; }

        public class ResetMapWebViewMessage
        {
            public string Command { get; set; } = "RESET_MAP";
        }

        public class ScenarioDownloaderViewModel : ViewModel
        {
            private int _downloadingProgress = 0;
            public int DownloadingProgress
            {
                get => _downloadingProgress;
                set
                {
                    SetProperty(ref _downloadingProgress, value);
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(CombinedLoadingProgress)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(LoadingBarVisible)));
                }
            }

            private int _visualizerProgress = 0;
            public int VisualizerProgress
            {
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
                get => Math.Max(VisualizerProgress, DownloadingProgress);
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

        public ScenarioDownloaderViewModel ViewModel { get; private set; }

        public ScenarioDownloader()
        {
            ViewModel = new ScenarioDownloaderViewModel();
            DataContext = ViewModel;
            InitializeComponent();
            InitializeWebviewAsync();
        }

        async void InitializeWebviewAsync()
        {
            ScenarioDownloaderWebview.Source = new Uri("https://www.rail-sim.de/");
            await ScenarioDownloaderWebview.EnsureCoreWebView2Async(null);
            ScenarioDownloaderWebview.NavigationCompleted += ScenarioDownloaderWebview_NavigationCompleted;
            ScenarioDownloaderWebview.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
        }

        private void CoreWebView2_DownloadStarting(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2DownloadStartingEventArgs e)
        {
            e.DownloadOperation.BytesReceivedChanged += (_, __) =>
            {
                ViewModel.DownloadingProgress = (int)((ulong)e.DownloadOperation.BytesReceived / (e.DownloadOperation.TotalBytesToReceive ?? ulong.MaxValue) * 100);
            };
            e.DownloadOperation.StateChanged += (_, __) =>
            {
                if (e.DownloadOperation.State == Microsoft.Web.WebView2.Core.CoreWebView2DownloadState.Completed)
                {
                    var directoryName = System.IO.Path.GetDirectoryName(e.ResultFilePath);
                    if (directoryName != null)
                    {
                        var folderName = System.IO.Path.GetFileNameWithoutExtension(e.ResultFilePath);
                        var folderPath = System.IO.Path.Combine(directoryName, folderName);

                        ZipFile.OpenRead(e.ResultFilePath).ExtractToDirectory(folderPath); // fix for winrar and bad crc zips

                        if (Directory.Exists(Path.Combine(folderPath, "Assets")) || Directory.Exists(Path.Combine(folderPath, "Content")))
                        {
                            if (Directory.Exists(Path.Combine(folderPath, "Assets")))
                            {
                                Utilities.CopyFilesRecursively(Path.Combine(folderPath, "Assets"), Path.Combine(App.RWLib!.TSPath, "Assets"));
                            }
                            if (Directory.Exists(Path.Combine(folderPath, "Content")))
                            {
                                Utilities.CopyFilesRecursively(Path.Combine(folderPath, "Content"), Path.Combine(App.RWLib!.TSPath, "Content"));
                            }
                            MessageBox.Show(Railworker.Language.Resources.success, Railworker.Language.Resources.msg_message, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            foreach (var file in Directory.EnumerateFiles(folderPath))
                            {
                                if (System.IO.Path.GetExtension(file) == ".rwp")
                                {
                                    App.RWLib!.ReadRWPFile(file).Archive.ExtractToDirectory(folderPath);
                                    MessageBox.Show(Railworker.Language.Resources.success, Railworker.Language.Resources.msg_message, MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                            var psi = new ProcessStartInfo() { FileName = directoryName, UseShellExecute = true };
                            Process.Start(psi);
                        }
                    }
                }
            };
        }

        private void ScenarioDownloaderWebview_NavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            var uri = new Uri(ScenarioDownloaderWebview.Source.AbsoluteUri);
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

                    e.Response = ScenarioDownloaderWebview.CoreWebView2.Environment.CreateWebResourceResponse(
                        ms, 200, "OK", headers);
                }
                else if (requestPath.StartsWith("leaflet"))
                {
                    string resourceName = String.Format("Railworker.Resources.{0}", requestPath);
                    Stream stream = assembly.GetManifestResourceStream(resourceName)!;
                    WebView2ManagedStream ms = new WebView2ManagedStream(stream);
                    var headers = DetermineHeaders(requestPath);

                    e.Response = ScenarioDownloaderWebview.CoreWebView2.Environment.CreateWebResourceResponse(
                        ms, 200, "OK", headers);
                }
                else
                {
                    e.Response = ScenarioDownloaderWebview.CoreWebView2.Environment.CreateWebResourceResponse(
                        null, 404, Railworker.Language.Resources.not_found, "");
                }
            }
            else
            {
                e.Response = ScenarioDownloaderWebview.CoreWebView2.Environment.CreateWebResourceResponse(
                    null, 404, Railworker.Language.Resources.not_found, "");
            }
        }

        private void InstallScenario_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
