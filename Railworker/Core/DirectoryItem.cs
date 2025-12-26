using System.Collections.ObjectModel;
using System.IO;

namespace Railworker.Core
{
    public class DirectoryItem : ViewModel
    {
        private string _name = "";
        private string _path = "";
        private bool _populated;
        private bool _isSelected;
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
        public ObservableCollection<DirectoryItem> SubDirectories { get; set; }

        public DirectoryItem()
        {
            Populated = false;
            SubDirectories = new ObservableCollection<DirectoryItem>();
        }

        public void PopulateSubDirectories()
        {
            if (Populated) return;
            SubDirectories.Clear();
            var dirInfo = new DirectoryInfo(Path);

            foreach (var directory in dirInfo.GetDirectories())
            {
                var item = new DirectoryItem
                {
                    Name = directory.Name,
                    Path = directory.FullName
                };
                item.SubDirectories.Add(new DirectoryItem
                {
                    Name = "Loading...",
                    Path = "dummy"
                });

                SubDirectories.Add(item);
            }
            Populated = true;
        }
    }
}