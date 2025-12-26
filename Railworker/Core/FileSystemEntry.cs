using RWLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;

namespace Railworker.Core
{
    public class FileSystemEntry : ViewModel
    {
        private string _name = "";
        private string _path = "";
        private bool _populated;
        private bool _isSelected;
        private bool? _isChecked;
        private bool _isFile;
        private bool _isDummy;
        private bool _isExpanded;

        public FileSystemEntry? Parent { get; set; }
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }
        public bool Populated
        {
            get => _populated;
            set => SetProperty(ref _populated, value);
        }
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        public bool? IsChecked
        {
            get => _isChecked;
            set {
                SetIsChecked(value);
            }
        }
        public bool IsFile
        {
            get => _isFile;
            set => SetProperty(ref _isFile, value);
        }
        public bool IsDummy
        {
            get => _isDummy;
            set => SetProperty(ref _isDummy, value);
        }
        public Visibility Visibility => _isDummy ? Visibility.Collapsed : Visibility.Visible;
        public ObservableCollection<FileSystemEntry> SubEntries { get; set; }

        public FileSystemEntry()
        {
            Populated = false;
            SubEntries = new ObservableCollection<FileSystemEntry>();
        }

        private void SetIsChecked(bool? setChecked, bool fromParent = false, bool fromChild = false)
        {
            var realNewValue = setChecked == null && !fromChild ? !_isChecked : setChecked;
            if (realNewValue == _isChecked) return;
            SetProperty(ref _isChecked, realNewValue);
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(IsChecked)));
            if (_isFile == false && !fromChild)
            {
                foreach (var entry in SubEntries)
                {
                    entry.SetIsChecked(realNewValue, true);
                }
            }
            if (!fromParent && Parent != null && Parent.IsChecked != realNewValue)
            {
                var allEqual = Parent.SubEntries.Skip(1).All(x => x.IsChecked == realNewValue);
                bool? value = allEqual ? realNewValue : null;
                Parent.SetIsChecked(value, false, true);
            }
        }

        private bool IsRoutesDir()
        {
            return !this.IsDummy 
                && !this.IsFile 
                && this.Name == "Routes" 
                && Parent?.Name == "Content" 
                && Parent?.Parent?.Parent == null;
        }

        private bool IsScenariosDir()
        {
            return this.Name == "Scenarios"
                && this.Parent?.Parent?.IsRoutesDir() == true;
        }

        public void PopulateSubDirectories(RWLibrary lib)
        {
            if (Populated || IsDummy || IsFile) return;
            if (Parent?.Parent != null) App.Current.Dispatcher.Invoke(() => SubEntries.Clear());

            var dirInfo = new DirectoryInfo(Path);
            var list = new List<FileSystemEntry>();
            var nameDictionary = new Dictionary<string, string>();

            if (IsScenariosDir())
            {
                var routeGuid = System.IO.Path.GetDirectoryName(Path);
                if (routeGuid == null) throw new DirectoryNotFoundException("Could not find directory Name of: " + Path);
                // this is a scenario
                foreach (var scenario in lib.RouteLoader.LoadScenarios(routeGuid).ToBlockingEnumerable())
                {
                    if (scenario.DisplayName != null) nameDictionary.Add(scenario.guid, Utilities.DetermineDisplayName(scenario.DisplayName));
                }
            }

            if (IsRoutesDir())
            {
                // this is a route
                foreach(var route in lib.RouteLoader.LoadRoutes().ToBlockingEnumerable())
                {
                    if (route.DisplayName != null) nameDictionary.Add(route.guid, Utilities.DetermineDisplayName(route.DisplayName));
                }
            }

            foreach (var directory in dirInfo.GetDirectories())
            {
                var name = directory.Name;

                if (IsRoutesDir() || IsScenariosDir())
                {
                    if (nameDictionary.ContainsKey(directory.Name))
                        name = $"{nameDictionary[directory.Name]} ({directory.Name})";
                    else
                        name = $"{Railworker.Language.Resources.unknown_name} ({directory.Name})";
                }

                var item = new FileSystemEntry
                {
                    Name = name,
                    Path = directory.FullName,
                    IsFile = false,
                    IsChecked = IsChecked,
                    Parent = this
                };
                item.SubEntries.Add(new FileSystemEntry
                {
                    Name = "Loading...",
                    Path = "dummy",
                    IsFile = true,
                    IsChecked = IsChecked,
                    IsDummy = true,
                    Parent = item
                });
                list.Add(item);
            }
            list.Sort((a, b) => String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            var fileList = new List<FileSystemEntry>();
            foreach (var file in dirInfo.GetFiles())
            {
                var item = new FileSystemEntry
                {
                    Name = file.Name,
                    Path = file.FullName,
                    IsFile = true,
                    IsChecked = IsChecked,
                    Parent = this
                };

                fileList.Add(item);
            }
            fileList.Sort((a, b) => String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            list.AddRange(fileList);
            Populated = true;
            App.Current.Dispatcher.Invoke(() =>
            {
                SubEntries = new ObservableCollection<FileSystemEntry>(list);
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(SubEntries)));
            });
        }

        class FileSystemIterator : IEnumerator<string>
        {
            public FileSystemIterator(FileSystemEntry entry)
            {

            }

            public string Current => throw new NotImplementedException();

            object IEnumerator.Current => throw new NotImplementedException();

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public bool MoveNext()
            {
                throw new NotImplementedException();
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<string> EnumerateFiles()
        {
            var filter = new Func<FileSystemEntry, bool>(f => !f.IsDummy && f.IsChecked != false);

            var tree = SubEntries.Where(filter).RecursiveSelect(f => f.SubEntries.Where(filter));

            foreach (var file in tree)
            {
                if (file.IsFile)
                {
                    yield return file.Path;
                }
                else if (file.Populated == false && !file.IsFile)
                {
                    foreach (var f in Directory.EnumerateFiles(file.Path, "*.*", SearchOption.AllDirectories))
                    {
                        yield return System.IO.Path.Combine(file.Path, f);
                    }
                }
            }
        }
    }
}