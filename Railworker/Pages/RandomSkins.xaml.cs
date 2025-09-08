using Microsoft.Win32;
using RWLib;
using RWLib.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
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
    /// Interaction logic for RandomSkins.xaml
    /// </summary>
    public partial class RandomSkins : Page
    {
        private RandomSkinsViewModel _viewModel;
        private RWLibrary _rwLib;
        private string _currentFilePath;

        public RandomSkins()
        {
            InitializeComponent();
            _rwLib = ((App)Application.Current).RWLib;
            _viewModel = new RandomSkinsViewModel();
            DataContext = _viewModel;
        }

        #region Event Handlers

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Open RandomSkins File"
            };
            
            // Set the initial directory if we have a saved one
            if (!string.IsNullOrEmpty(SharedDirectories.LastJsonDirectory) && Directory.Exists(SharedDirectories.LastJsonDirectory))
            {
                openFileDialog.InitialDirectory = SharedDirectories.LastJsonDirectory;
            }

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _currentFilePath = openFileDialog.FileName;
                    // Save the directory for next time
                    SharedDirectories.LastJsonDirectory = System.IO.Path.GetDirectoryName(_currentFilePath);
                    
                    string jsonContent = File.ReadAllText(_currentFilePath);
                    var randomSkinGroups = RandomSkinGroup.FromJson(jsonContent);
                    
                    if (randomSkinGroups != null && randomSkinGroups.Count > 0)
                    {
                        _viewModel.LoadRandomSkinGroups(randomSkinGroups);
                        StatusText.Text = $"Loaded {randomSkinGroups.Count} random skin groups from: {_currentFilePath}";
                    }
                    else
                    {
                        MessageBox.Show("No random skin groups found in the file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    Title = "Save RandomSkins File"
                };
                
                // Set the initial directory if we have a saved one
                if (!string.IsNullOrEmpty(SharedDirectories.LastJsonDirectory) && Directory.Exists(SharedDirectories.LastJsonDirectory))
                {
                    saveFileDialog.InitialDirectory = SharedDirectories.LastJsonDirectory;
                }

                if (saveFileDialog.ShowDialog() == true)
                {
                    _currentFilePath = saveFileDialog.FileName;
                    // Save the directory for next time
                    SharedDirectories.LastJsonDirectory = System.IO.Path.GetDirectoryName(_currentFilePath);
                }
                else
                {
                    return;
                }
            }

            try
            {
                var randomSkinGroups = _viewModel.RandomSkinGroups.ToList();
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                string jsonContent = JsonSerializer.Serialize(randomSkinGroups, options);
                File.WriteAllText(_currentFilePath, jsonContent);
                
                StatusText.Text = $"Saved {randomSkinGroups.Count} random skin groups to: {_currentFilePath}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddRandomSkinGroupButton_Click(object sender, RoutedEventArgs e)
        {
            var newRandomSkinGroup = new RandomSkinGroup
            {
                Id = $"new_group_{DateTime.Now.Ticks}",
                RandomSkins = new List<RandomSkin>()
            };
            
            _viewModel.RandomSkinGroups.Add(newRandomSkinGroup);
            StatusText.Text = "Added new random skin group";
            
            // Open the editor for the new random skin group
            OpenRandomSkinGroupEditor(newRandomSkinGroup);
        }

        private void RemoveRandomSkinGroupButton_Click(object sender, RoutedEventArgs e)
        {
            if (RandomSkinGroupsListView.SelectedItem is RandomSkinGroup selectedGroup)
            {
                var result = MessageBox.Show($"Are you sure you want to remove the random skin group '{selectedGroup.Id}'?", 
                    "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.RandomSkinGroups.Remove(selectedGroup);
                    StatusText.Text = $"Removed random skin group: {selectedGroup.Id}";
                }
            }
            else
            {
                MessageBox.Show("Please select a random skin group to remove.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RandomSkinGroupsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This method is left empty for now
        }

        private void EditRandomSkinGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is RandomSkinGroup randomSkinGroup)
            {
                OpenRandomSkinGroupEditor(randomSkinGroup);
            }
        }

        #endregion

        #region Helper Methods

        private void OpenRandomSkinGroupEditor(RandomSkinGroup randomSkinGroup)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.OpenTab(new MainWindow.MainWindowViewModel.Tab
            {
                FrameContent = new RandomSkinGroupEditor(randomSkinGroup),
                Header = $"Edit: {randomSkinGroup.Id}"
            });
        }

        #endregion

        #region ViewModel

        public class RandomSkinsViewModel : INotifyPropertyChanged
        {
            private ObservableCollection<RandomSkinGroup> _randomSkinGroups;

            public ObservableCollection<RandomSkinGroup> RandomSkinGroups
            {
                get => _randomSkinGroups;
                set => SetProperty(ref _randomSkinGroups, value);
            }

            public RandomSkinsViewModel()
            {
                RandomSkinGroups = new ObservableCollection<RandomSkinGroup>();
            }

            public void LoadRandomSkinGroups(List<RandomSkinGroup> randomSkinGroups)
            {
                RandomSkinGroups.Clear();
                foreach (var group in randomSkinGroups)
                {
                    RandomSkinGroups.Add(group);
                }
            }

            #region INotifyPropertyChanged

            public event PropertyChangedEventHandler PropertyChanged;

            protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
            {
                if (EqualityComparer<T>.Default.Equals(field, value)) return false;
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }

            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

        #endregion
    }
}

    #endregion
}
