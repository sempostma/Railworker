using Microsoft.WindowsAPICodePack.Dialogs;
using Railworker.Core;
using Railworker.UserControls;
using RWLib;
using RWLib.Interfaces;
using RWLib.RWBlueprints;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.Json;
using System.Threading;
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
using System.Xml.Serialization;
using static Railworker.UserControls.AssetExplorer;

namespace Railworker.Pages
{
    /// <summary>
    /// Interaction logic for PreloadsPage.xaml
    /// </summary>
    public partial class PreloadsPage : Page
    {
        internal App App { get => (App)Application.Current; }
        internal Logger Logger { get => App.Logger; }

        public class Dependency
        {
            public required string Provider { get; set; }
            public required string Product { get; set; }
        }

        public class PreloadEntry
        {
            public required RWPreloadEntry Entry { get; set; }
            public string Name
            {
                get
                {
                    if (Entry.Found) return ((RWPreloadEntryFound)Entry).Blueprint.BinPath;
                    else return Entry.BlueprintName.ToString();
                }
            }
        }

        public class PreloadsPageViewModel : ViewModel
        {
            public ObservableCollection<PreloadEntry> PreloadEntries { get; set; } = new ObservableCollection<PreloadEntry>();
            public ObservableCollection<Dependency> Dependencies { get; set; } = new ObservableCollection<Dependency>();

            private int _preloadLoadingProgress = 0;
            public int PreloadLoadingProgress
            {
                get => _preloadLoadingProgress;
                set
                {
                    SetProperty(ref _preloadLoadingProgress, value);
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(CombinedLoadingProgress)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(LoadingBarVisible)));
                }
            }

            public int CombinedLoadingProgress
            {
                get => Math.Max(PreloadLoadingProgress, 0);
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

            private bool _vehicleScanInProgress = false;
            public bool VehicleScanInProgress
            {
                get => _vehicleScanInProgress;
                set
                {
                    SetProperty(ref _vehicleScanInProgress, value);
                }
            }

            public PreloadsPageViewModel()
            {
            }
        }

        public class CachedResult {
            public List<Dependency> Dependencies { get; set; } = new List<Dependency>();
            public List<PreloadEntry> PreloadEntries { get; set; } = new List<PreloadEntry>();
        }

        public PreloadsPageViewModel ViewModel { get; private set; }

        private LogShortcut Log;
        private CancellationTokenSource loadPreloadsCancellationSource = new CancellationTokenSource();
        private Dictionary<AssetExplorer.Asset, CachedResult> preloadsCache = new System.Collections.Generic.Dictionary<Asset, CachedResult>();

        public PreloadsPage()
        {
            ViewModel = new PreloadsPageViewModel();
            InitializeComponent();
            AssetExplorer.Mapper = MapBlueprintToPreloads;
            AssetExplorer.AvailableBlueprints.SelectionChanged += AvailableBlueprints_SelectionChanged;
            AssetExplorer.ViewModel.SelectionMode = SelectionMode.Extended;
            DataContext = ViewModel;
            Log = new LogShortcut(App.Logger);
        }

        private void AvailableBlueprints_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var _ = LoadPreloads();
        }

        public void AddDependency(Dependency dep, List<Dependency> depsCache)
        {
            var func = new Func<Dependency, bool>(x => x.Provider.ToLower() == dep.Provider.ToLower() && x.Product.ToLower() == dep.Product.ToLower());
            if (!ViewModel.Dependencies.Any(func)) ViewModel.Dependencies.Add(dep);
            if (!depsCache.Any(func)) depsCache.Add(dep);
        }

        public async Task LoadPreloads()
        {
            loadPreloadsCancellationSource.Cancel();
            loadPreloadsCancellationSource = new CancellationTokenSource();
            ViewModel.PreloadLoadingProgress = 0;
            var counter = 0;
            IProgress<bool> progress = new Progress<bool>(_ =>
            {
                if (loadPreloadsCancellationSource.IsCancellationRequested) return;
                double fraction = 1 - 10.0 / (10.0 + ++counter);
                var p = (int)Math.Round(fraction * 100);
                ViewModel.PreloadLoadingProgress = p;
            });
            ViewModel.Dependencies.Clear();
            ViewModel.PreloadEntries.Clear();
            AssetExplorer.Asset[] assets = new AssetExplorer.Asset[AssetExplorer.AvailableBlueprints.SelectedItems.Count];
            AssetExplorer.AvailableBlueprints.SelectedItems.CopyTo(assets, 0);
            await Task.Run(async () =>
            {
                foreach (AssetExplorer.Asset item in assets)
                {
                    if (loadPreloadsCancellationSource.IsCancellationRequested) break;
                    progress.Report(true);
                    await LoadPreload(item);
                }
            }, loadPreloadsCancellationSource.Token);
            if (loadPreloadsCancellationSource.IsCancellationRequested) return;
            Application.Current.Dispatcher.Invoke(delegate
            {
                ViewModel.PreloadLoadingProgress = 0;
            });
        }

        public class PreloadListItem
        {
            public List<Dependency> Dependencies { get; set; } = new List<Dependency>();
            public List<PreloadEntry> PreloadEntrys { get; set; } = new List<PreloadEntry>();
            public string Name { get; set; } = "";
        }

        public async Task<PreloadListItem> GetPreloadEntriesAndDeps(AssetExplorer.Asset asset)
        {
            var result = new PreloadListItem
            {
                Name = asset.Name
            };

            void addDependency(Dependency dep)
            {
                if (result!.Dependencies.Any(x => x.Provider.ToLower() == dep.Provider.ToLower() && x.Product.ToLower() == dep.Product.ToLower()) == false)
                {
                    result.Dependencies.Add(dep);
                }
            }

            if (preloadsCache.ContainsKey(asset))
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    var cache = preloadsCache[asset];
                    foreach (var dep in cache.Dependencies)
                    {
                        if (dep.Product == "" || dep.Provider == "") continue;
                        if (result.Dependencies.Any(x => x.Provider.ToLower() == dep.Provider.ToLower() && x.Product.ToLower() == dep.Product.ToLower()) == false)
                        {
                            result.Dependencies.Add(dep);
                        }
                    }
                    foreach (var entry in cache.PreloadEntries)
                    {
                        result.PreloadEntrys.Add(entry);
                    }
                });
                return result;
            }

            await foreach (var entry in this.App.RWLib!.BlueprintLoader.FromPreload((RWConsistBlueprintAbstract)asset.Blueprint))
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    var pEntry = new PreloadEntry { Entry = entry };
                    ViewModel.PreloadEntries.Add(pEntry);
                    result.PreloadEntrys.Add(pEntry);
                    addDependency(new Dependency
                    {
                        Provider = entry.BlueprintName.Provider,
                        Product = entry.BlueprintName.Product
                    });
                    if (entry is RWPreloadEntryFound)
                    {
                        var foundEntry = (RWPreloadEntryFound)entry;
                        var cargoDef = foundEntry.Blueprint.Xml.Descendants("CargoBlueprintID").FirstOrDefault();
                        var provider = cargoDef?.Descendants("Provider").FirstOrDefault()?.Value ?? "";
                        var product = cargoDef?.Descendants("Product").FirstOrDefault()?.Value ?? "";
                        addDependency(new Dependency
                        {
                            Provider = provider,
                            Product = product
                        });

                        foreach (var child in foundEntry.Blueprint.Xml.Descendants("cEntityContainerBlueprint-sChild"))
                        {
                            var cProvider = child.Descendants("Provider").FirstOrDefault()?.Value ?? "";
                            var cProduct = child.Descendants("Product").FirstOrDefault()?.Value ?? "";
                            addDependency(new Dependency
                            {
                                Provider = cProvider,
                                Product = cProduct
                            });
                        }
                    }
                });
            }

            preloadsCache[asset] = new CachedResult
            {
                Dependencies = result.Dependencies,
                PreloadEntries = result.PreloadEntrys
            };

            return result;
        }

        public async Task LoadPreload(AssetExplorer.Asset asset)
        {
            try
            {
                var preload = await GetPreloadEntriesAndDeps(asset);
                Application.Current.Dispatcher.Invoke(delegate
                {
                    foreach (var dep in preload.Dependencies)
                    {
                        if (ViewModel.Dependencies.Any(x => x.Provider.ToLower() == dep.Provider.ToLower() && x.Product.ToLower() == dep.Product.ToLower()) == false)
                        {
                            ViewModel.Dependencies.Add(dep);
                        }
                    }
                    foreach (var entry in preload.PreloadEntrys)
                    {
                        ViewModel.PreloadEntries.Add(entry);
                    }
                });
                return;
            }
            catch(Exception ex)
            {
                Log.Error(ex.ToString());
                Application.Current.Dispatcher.Invoke(delegate
                {
                    MessageBox.Show(
                        ex.Message,
                        Railworker.Language.Resources.msg_message,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                });
            }
        }

        public IEnumerable<AssetExplorer.Asset> MapBlueprintToPreloads(RWBlueprint blueprint)
        {
            if (blueprint is RWConsistBlueprintAbstract)
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(blueprint.BlueprintIDPath);
                if (blueprint is RWConsistFragmentBlueprint) name += " (Fragment)";

                yield return new AssetExplorer.Asset
                {
                    Blueprint = blueprint,
                    Name = name
                };
            }
        }

        class LogShortcut
        {
            private IRWLogger logger;

            public LogShortcut(IRWLogger logger)
            {
                this.logger = logger;
            }
            public void Debug(params string[] args)
            {
                logger?.Log(RWLogType.Debug, String.Format(args[0], args.Skip(1)));
            }
            public void Error(params string[] args)
            {
                logger?.Log(RWLogType.Error, String.Format(args[0], args.Skip(1)));
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ConsistsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DependenciesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        public class PreloadsRoot
        {
            public List<PreloadListItem> PreloadListItems { get; set; } = new List<PreloadListItem>();
        }

        private async Task Export(string path)
        {
            using (var file = File.Open(path, FileMode.Create))
            {
                AssetExplorer.Asset[] assets = new AssetExplorer.Asset[AssetExplorer.AvailableBlueprints.SelectedItems.Count];
                AssetExplorer.AvailableBlueprints.SelectedItems.CopyTo(assets, 0);
                var root = new PreloadsRoot();
                foreach (AssetExplorer.Asset item in assets)
                {
                    root.PreloadListItems.Add(await GetPreloadEntriesAndDeps(item));
                }

                if (System.IO.Path.GetExtension(path).ToLower() == ".xml")
                {
                    var serializer = new XmlSerializer(root.GetType(), new[] { typeof(RWPreloadEntryFound), typeof (RWPreloadEntryNotFound) });
                    serializer.Serialize(file, root);
                }
                else if (System.IO.Path.GetExtension(path).ToLower() == ".html")
                {
                    using (var writer = new StreamWriter(file))
                    {
                        writer.WriteLine("<table>");
                        writer.WriteLine("<tr>");
                        writer.WriteLine("<th>");
                        writer.WriteLine("Train");
                        writer.WriteLine("</th>");
                        writer.WriteLine("<th>");
                        writer.WriteLine("Dependencies");
                        writer.WriteLine("</th>");
                        writer.WriteLine("</tr>");

                        foreach (var item in root.PreloadListItems)
                        {
                            writer.WriteLine("<tr>");
                            writer.WriteLine("<td>");
                            writer.WriteLine(item.Name);
                            writer.WriteLine("</td>");
                            writer.WriteLine("<td>");

                            foreach (var deps in item.Dependencies)
                            {
                                writer.WriteLine(deps.Provider + "->" + deps.Product);
                                writer.WriteLine("<br>");
                            }
                            
                            writer.WriteLine("</td>");
                            writer.WriteLine("</tr>");
                        }

                        writer.WriteLine("</table>");
                    }
                }
                else
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    JsonSerializer.Serialize(file, root, options);
                }
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            CommonSaveFileDialog dialog = new CommonSaveFileDialog();

            dialog.Filters.Add(new CommonFileDialogFilter("XML", "*.xml"));
            dialog.Filters.Add(new CommonFileDialogFilter("JSON", "*.json"));
            dialog.Filters.Add(new CommonFileDialogFilter("HTML", "*.html"));

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                var path = dialog.FileName;

                if (path != null)
                {
                    var _ = Export(path);
                }
            }

        }
    }
}
