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
    /// Interaction logic for Compositions.xaml
    /// </summary>
    public partial class Compositions : Page
    {
        private CompositionsViewModel _viewModel;
        private RWLibrary _rwLib;
        private string _currentCompositionsFilePath;
        private string _currentRandomSkinsFilePath;

        public Compositions()
        {
            InitializeComponent();
            _rwLib = ((App)Application.Current).RWLib;
            _viewModel = new CompositionsViewModel();
            DataContext = _viewModel;
        }

        #region Event Handlers

        private void OpenCompositionsFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Open Compositions File"
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
                    _currentCompositionsFilePath = openFileDialog.FileName;
                    // Save the directory for next time
                    SharedDirectories.LastJsonDirectory = System.IO.Path.GetDirectoryName(_currentCompositionsFilePath);
                    
                    string jsonContent = File.ReadAllText(_currentCompositionsFilePath);
                    var compositions = Composition.FromJson(jsonContent);
                    
                    if (compositions != null && compositions.Count > 0)
                    {
                        _viewModel.LoadCompositions(compositions);
                        StatusText.Text = $"Loaded {compositions.Count} compositions from: {_currentCompositionsFilePath}";
                        UpdateMissingCompositionsStatus();
                    }
                    else
                    {
                        MessageBox.Show("No compositions found in the file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading compositions file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveCompositionsFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentCompositionsFilePath) || !File.Exists(_currentCompositionsFilePath))
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    Title = "Save Compositions File"
                };
                
                // Set the initial directory if we have a saved one
                if (!string.IsNullOrEmpty(SharedDirectories.LastJsonDirectory) && Directory.Exists(SharedDirectories.LastJsonDirectory))
                {
                    saveFileDialog.InitialDirectory = SharedDirectories.LastJsonDirectory;
                }

                if (saveFileDialog.ShowDialog() == true)
                {
                    _currentCompositionsFilePath = saveFileDialog.FileName;
                    // Save the directory for next time
                    SharedDirectories.LastJsonDirectory = System.IO.Path.GetDirectoryName(_currentCompositionsFilePath);
                }
                else
                {
                    return;
                }
            }

            try
            {
                var compositions = _viewModel.Compositions.ToList();
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                string jsonContent = JsonSerializer.Serialize(compositions, options);
                File.WriteAllText(_currentCompositionsFilePath, jsonContent);
                
                StatusText.Text = $"Saved {compositions.Count} compositions to: {_currentCompositionsFilePath}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving compositions file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenRandomSkinsFileButton_Click(object sender, RoutedEventArgs e)
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
                    _currentRandomSkinsFilePath = openFileDialog.FileName;
                    // Save the directory for next time
                    SharedDirectories.LastJsonDirectory = System.IO.Path.GetDirectoryName(_currentRandomSkinsFilePath);
                    
                    string jsonContent = File.ReadAllText(_currentRandomSkinsFilePath);
                    var randomSkinGroups = RandomSkinGroup.FromJson(jsonContent);
                    
                    if (randomSkinGroups != null && randomSkinGroups.Count > 0)
                    {
                        _viewModel.LoadRandomSkinGroups(randomSkinGroups);
                        StatusText.Text = $"Loaded {randomSkinGroups.Count} random skin groups from: {_currentRandomSkinsFilePath}";
                        UpdateMissingCompositionsStatus();
                    }
                    else
                    {
                        MessageBox.Show("No random skin groups found in the file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading random skins file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveRandomSkinsFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentRandomSkinsFilePath) || !File.Exists(_currentRandomSkinsFilePath))
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
                    _currentRandomSkinsFilePath = saveFileDialog.FileName;
                    // Save the directory for next time
                    SharedDirectories.LastJsonDirectory = System.IO.Path.GetDirectoryName(_currentRandomSkinsFilePath);
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
                File.WriteAllText(_currentRandomSkinsFilePath, jsonContent);
                
                StatusText.Text = $"Saved {randomSkinGroups.Count} random skin groups to: {_currentRandomSkinsFilePath}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving random skins file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddCompositionButton_Click(object sender, RoutedEventArgs e)
        {
            var newComposition = new Composition
            {
                Id = $"new_composition_{DateTime.Now.Ticks}",
                Name = "New Composition",
                BasePath = "",
                FullSkinsAmount = 36,
                ComposedImageWidth = 2048,
                ComposedImageHeight = 2048,
                InputImageResizeWidth = 512,
                InputImageResizeHeight = 512,
                StylusXInterval = 512,
                StylusYInterval = 227,
                ComposedImageColumns = 4,
                ComposedImageRows = 4,
                OutputScaleX = 1.0f,
                OutputScaleY = 1.0f,
                Projections = new List<Composition.Projection>()
            };
            
            _viewModel.Compositions.Add(newComposition);
            StatusText.Text = "Added new composition";
            
            // Open the editor for the new composition
            OpenCompositionEditor(newComposition);
        }

        private void RemoveCompositionButton_Click(object sender, RoutedEventArgs e)
        {
            if (CompositionsListView.SelectedItem is Composition selectedComposition)
            {
                var result = MessageBox.Show($"Are you sure you want to remove the composition '{selectedComposition.Name}'?", 
                    "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.Compositions.Remove(selectedComposition);
                    StatusText.Text = $"Removed composition: {selectedComposition.Id}";
                    UpdateMissingCompositionsStatus();
                }
            }
            else
            {
                MessageBox.Show("Please select a composition to remove.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    UpdateMissingCompositionsStatus();
                }
            }
            else
            {
                MessageBox.Show("Please select a random skin group to remove.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CompositionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This method is left empty for now
        }

        private void RandomSkinGroupsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This method is left empty for now
        }

        private void EditComposition_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Composition composition)
            {
                OpenCompositionEditor(composition);
            }
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

        private void OpenCompositionEditor(Composition composition)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.OpenTab(new MainWindow.MainWindowViewModel.Tab
            {
                FrameContent = new CompositionEditor(composition),
                Header = $"Edit: {composition.Name}"
            });
        }

        private void OpenRandomSkinGroupEditor(RandomSkinGroup randomSkinGroup)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.OpenTab(new MainWindow.MainWindowViewModel.Tab
            {
                FrameContent = new RandomSkinGroupEditor(randomSkinGroup, _viewModel.Compositions.ToList()),
                Header = $"Edit: {randomSkinGroup.Id}"
            });
        }

        private void UpdateMissingCompositionsStatus()
        {
            var missingCompositions = new List<string>();
            
            foreach (var group in _viewModel.RandomSkinGroups)
            {
                foreach (var randomSkin in group.RandomSkins)
                {
                    if (!string.IsNullOrEmpty(randomSkin.Composition) && 
                        !_viewModel.Compositions.Any(c => c.Id == randomSkin.Composition))
                    {
                        if (!missingCompositions.Contains(randomSkin.Composition))
                        {
                            missingCompositions.Add(randomSkin.Composition);
                        }
                    }
                }
            }

            if (missingCompositions.Count > 0)
            {
                StatusText.Text += $" | Missing compositions: {string.Join(", ", missingCompositions)}";
            }
        }

        #endregion

        #region ViewModel

        public class CompositionsViewModel : INotifyPropertyChanged
        {
            private ObservableCollection<Composition> _compositions;
            private ObservableCollection<RandomSkinGroup> _randomSkinGroups;

            public ObservableCollection<Composition> Compositions
            {
                get => _compositions;
                set => SetProperty(ref _compositions, value);
            }

            public ObservableCollection<RandomSkinGroup> RandomSkinGroups
            {
                get => _randomSkinGroups;
                set => SetProperty(ref _randomSkinGroups, value);
            }

            public CompositionsViewModel()
            {
                Compositions = new ObservableCollection<Composition>();
                RandomSkinGroups = new ObservableCollection<RandomSkinGroup>();
            }

            public void LoadCompositions(List<Composition> compositions)
            {
                Compositions.Clear();
                foreach (var composition in compositions)
                {
                    Compositions.Add(composition);
                }
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

        #endregion
    }

    #region Converters

    public class TotalSkinsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RandomSkinGroup group)
            {
                return group.RandomSkins.Sum(rs => rs.Skins.Count);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MissingCompositionsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RandomSkinGroup group && parameter is ObservableCollection<Composition> compositions)
            {
                var missingCompositions = new List<string>();
                
                foreach (var randomSkin in group.RandomSkins)
                {
                    if (!string.IsNullOrEmpty(randomSkin.Composition) && 
                        !compositions.Any(c => c.Id == randomSkin.Composition))
                    {
                        if (!missingCompositions.Contains(randomSkin.Composition))
                        {
                            missingCompositions.Add(randomSkin.Composition);
                        }
                    }
                }

                return missingCompositions.Count > 0 ? string.Join(", ", missingCompositions) : "None";
            }
            return "None";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}
