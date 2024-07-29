using Microsoft.WindowsAPICodePack.Dialogs;
using Railworker.Core;
using Railworker.Properties;
using RWLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
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
    /// Interaction logic for RWPPackager.xaml
    /// </summary>
    public partial class RWPPackager : Page
    {
        public class RWPPackagerViewModel : ViewModel
        {
            public ObservableCollection<FileSystemEntry> FileSystemEntries { get; set; } = new ObservableCollection<FileSystemEntry>();
            public String FileExtensionsToIgnore { get; set; } = "dds,psd,bak,pak,tgt,cost,pak";

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

            private RWPackageInfo.LicenseType _license;
            public RWPackageInfo.LicenseType License
            {
                get => _license;
                set => SetProperty(ref _license, value);
            }

            public int LicenseSelectedIndex
            {
                get => (int)License;
                set => License = (RWPackageInfo.LicenseType)value;
            }

            public string Author { get; set; } = "";
        }

        internal App App { get => (App)Application.Current; }
        internal Logger Logger { get => App.Logger; }

        RWPPackagerViewModel ViewModel { get; set; } = new RWPPackagerViewModel();

        private bool didPressEnter = false;
        private string searchTerm = "";
        private DateTime lastSearch = DateTime.Now;
        private FileSystemEntry? rootNode;

        public RWPPackager()
        {
            DataContext = ViewModel;
            InitializeTreeView();
            InitializeComponent();
        }

        public void InitializeTreeView()
        {
            this.rootNode = new FileSystemEntry
            {
                Name = "Railworks",
                Path = System.IO.Path.Combine(Settings.Default.TsPath),
                IsFile = false,
                IsDummy = false,
                IsChecked = false,
                IsSelected = false,
                Populated = true,
                SubEntries = new ObservableCollection<FileSystemEntry>
                {
                    new FileSystemEntry
                    {
                        Name = "Assets",
                        Path = System.IO.Path.Combine(Settings.Default.TsPath, "Assets"),
                        IsFile = false,
                        IsDummy = false,
                        IsChecked = false,
                        IsSelected = false,
                    },
                    new FileSystemEntry
                    {
                        Name = "Content",
                        Path = System.IO.Path.Combine(Settings.Default.TsPath, "Content"),
                        IsFile = false,
                        IsDummy = false,
                        IsChecked = false,
                        IsSelected = false,
                    },
                    new FileSystemEntry
                    {
                        Name = "Manuals",
                        Path = System.IO.Path.Combine(Settings.Default.TsPath, "Manuals"),
                        IsFile = false,
                        IsDummy = false,
                        IsChecked = false,
                        IsSelected = false
                    }
                }
            };

            foreach (FileSystemEntry item in rootNode.SubEntries)
            {
                item.SubEntries.Add(new FileSystemEntry
                {
                    Name = "Loading...",
                    Path = "dummy",
                    IsFile = true,
                    IsChecked = false,
                    IsDummy = true,
                    Parent = item
                });
                item.Parent = rootNode;
                ViewModel.FileSystemEntries.Add(item);
            }
        }

        private void DirectoryTree_TextInput(object sender, TextCompositionEventArgs e)
        {
            var selectedItem = DirectoryTree.SelectedItem as FileSystemEntry;

            if (selectedItem == null) return;

            if (e.Text == "\b")
            {
                if (selectedItem == null)
                {
                    return;
                }
                selectedItem.IsChecked = false;

                return;

            }
            if (e.Text == "\r")
            {
                if (selectedItem == null)
                {
                    return;
                }

                didPressEnter = true;
                selectedItem.IsChecked = true;

                if (!selectedItem.IsFile)
                {
                    selectedItem.IsExpanded = true;
                }

                return;
            }

            if ((DateTime.Now - lastSearch).Seconds > 1)
            {
                searchTerm = "";
            }

            lastSearch = DateTime.Now;
            searchTerm += e.Text;

            List<FileSystemEntry> searchItems;

            if (selectedItem == null)
            {
                searchItems = DirectoryTree.Items.Cast<FileSystemEntry>().ToList();
            }
            else if (didPressEnter)
            {
                searchItems = selectedItem.SubEntries.ToList();
            }
            else
            {
                ItemsControl? parent = ParentContainerFromItem(DirectoryTree, selectedItem);

                if (parent is TreeViewItem)
                {
                    searchItems = ((TreeViewItem)parent).Items.Cast<FileSystemEntry>().ToList();
                }
                else
                {
                    searchItems = DirectoryTree.Items.Cast<FileSystemEntry>().ToList();
                }
            }

            var firstItem = searchItems.FirstOrDefault(item => item.Name.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase));

            if (firstItem == null)
            {
                searchTerm = e.Text;
                firstItem = searchItems.FirstOrDefault(item => item.Name.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (firstItem != null)
            {
                TreeViewItem? firstItemTvi = (TreeViewItem?)ContainerFromItem(DirectoryTree, firstItem);
                if (firstItemTvi != null)
                {
                    firstItem.IsSelected = true;
                    firstItemTvi.IsSelected = true;
                    firstItemTvi.BringIntoView();
                }
            }

            didPressEnter = false;
        }

        public void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem)
            {
                var tvi = (TreeViewItem)sender;
                tvi.IsSelected = true;
                tvi.BringIntoView();
                e.Handled = true;
            }
        }

        public DependencyObject? ContainerFromItem(ItemsControl itemsControl, object value)
        {
            var dp = itemsControl.ItemContainerGenerator.ContainerFromItem(value);

            if (dp != null)
                return dp;

            foreach (var item in itemsControl.Items)
            {
                var currentTreeViewItem = itemsControl.ItemContainerGenerator.ContainerFromItem(item);

                if (currentTreeViewItem is ItemsControl == false)
                {
                    continue;
                }

                var childDp = ContainerFromItem((ItemsControl)currentTreeViewItem, value);

                if (childDp != null)
                    return childDp;
            }
            return null;
        }

        public ItemsControl? ParentContainerFromItem(ItemsControl parent, FileSystemEntry child)
        {
            if (parent.Items.Contains(child))
                return parent;

            foreach (FileSystemEntry entry in parent.Items)
            {
                DependencyObject item = parent.ItemContainerGenerator.ContainerFromItem(entry);
                if (item is ItemsControl == false) continue;
                ItemsControl? result = ParentContainerFromItem((ItemsControl)item, child);
                if (result != null) return result;
            }
            return null;
        }

        public async Task TreeViewExpanded(TreeViewItem tvi, FileSystemEntry selected)
        {
            if (selected.IsFile) return;
            await Task.Run(() => selected.PopulateSubDirectories(App.RWLib!));

            ItemsControl? parent = ParentContainerFromItem(DirectoryTree, selected);

            int index;
            if (parent is TreeViewItem)
            {
                index = parent.Items.IndexOf(selected);
                if (index >= 0 && index + 1 < parent.Items.Count)
                {
                    TreeViewItem childTvi = (TreeViewItem)parent.ItemContainerGenerator.ContainerFromItem(parent.Items[index + 1]);
                    childTvi.BringIntoView();
                }
            }
            else
            {
                index = DirectoryTree.Items.IndexOf(selected);
                if (index >= 0 && index + 1 < DirectoryTree.Items.Count)
                {
                    TreeViewItem childTvi = (TreeViewItem)DirectoryTree.ItemContainerGenerator.ContainerFromItem(DirectoryTree.Items[index + 1]);
                    childTvi.BringIntoView();
                }
            }
            tvi.BringIntoView();
        }

        public void TreeView_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem tvi = (TreeViewItem)e.OriginalSource;
            FileSystemEntry selected = (FileSystemEntry)tvi.Header;

            var _ = TreeViewExpanded(tvi, selected);
        }

        private void DirectoryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Debug.WriteLine(sender);
            Debug.WriteLine(DirectoryTree);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var _ = Save();
        }

        private async Task Save()
        {
            var fileExtensionsToIgnore = ViewModel.FileExtensionsToIgnore.Split(',').Select(x => x.TrimStart('.').Trim()).ToHashSet();
            if (rootNode == null) return;

            CommonSaveFileDialog dialog = new CommonSaveFileDialog();

            dialog.Filters.Add(new CommonFileDialogFilter("Railworks package", "*.rwp"));
            dialog.Filters.Add(new CommonFileDialogFilter("Zip", "*.zip"));
            dialog.Filters.Add(new CommonFileDialogFilter("Package info", "*.pi"));

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                var path = dialog.FileName;
                if (path != null)
                {
                    var filenames = rootNode.EnumerateFiles()
                                .Where(x => !fileExtensionsToIgnore.Contains(System.IO.Path.GetExtension(x).TrimStart('.')))
                                .Select(x => System.IO.Path.GetRelativePath(App.RWLib!.TSPath, x))
                                .ToArray();
                    var packageInfo = new RWPackageInfo
                    {
                        Author = ViewModel.Author,
                        FileNames = filenames,
                        License = ViewModel.License,
                        Name = System.IO.Path.GetFileNameWithoutExtension(path).Replace("_", " ")
                    };

                    if (System.IO.Path.GetExtension(path) == ".zip")
                    {
                        App.RWLib!.WriteZipFile(packageInfo, path);
                        return;
                    }

                    if (System.IO.Path.GetExtension(path) == ".pi")
                    {
                        var hasher = MD5.Create();
                        var noop = new NoOpWriteStream();
                        using (CryptoStream crypto = new CryptoStream(noop, hasher, CryptoStreamMode.Write))
                        {
                            App.RWLib!.WriteRWPFile(packageInfo, crypto);
                        }
                        await App.RWLib.WritePackageInfoFile(new RWPackageInfoWithMD5
                        {
                            Author = packageInfo.Author,
                            FileNames = packageInfo.FileNames,
                            License = packageInfo.License,
                            Md5 = Convert.ToHexString(hasher.Hash!),
                            Name = packageInfo.Name
                        }, path);
                    }
                    else
                    {
                        App.RWLib!.WriteRWPFile(packageInfo, path);
                    }

                    string args = string.Format("/e, /select, \"{0}\"", path);

                    ProcessStartInfo info = new ProcessStartInfo();
                    info.FileName = "explorer";
                    info.Arguments = args;
                    Process.Start(info);
                }
            }
        }
    }
}
