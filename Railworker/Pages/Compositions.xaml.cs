using Microsoft.Win32;
using RWLib;
using RWLib.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private string _currentFilePath;

        public Compositions()
        {
            InitializeComponent();
            _rwLib = ((App)Application.Current).RWLib;
            _viewModel = new CompositionsViewModel();
            DataContext = _viewModel;
        }

        #region Event Handlers

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
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
                    _currentFilePath = openFileDialog.FileName;
                    // Save the directory for next time
                    SharedDirectories.LastJsonDirectory = System.IO.Path.GetDirectoryName(_currentFilePath);
                    
                    string jsonContent = File.ReadAllText(_currentFilePath);
                    var compositions = Composition.FromJson(jsonContent);
                    
                    if (compositions != null && compositions.Count > 0)
                    {
                        _viewModel.LoadCompositions(compositions);
                        StatusText.Text = $"Loaded {compositions.Count} compositions from: {_currentFilePath}";
                    }
                    else
                    {
                        MessageBox.Show("No compositions found in the file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    Title = "Save Compositions File"
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
                var compositions = _viewModel.Compositions.ToList();
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                string jsonContent = JsonSerializer.Serialize(compositions, options);
                File.WriteAllText(_currentFilePath, jsonContent);
                
                StatusText.Text = $"Saved {compositions.Count} compositions to: {_currentFilePath}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                }
            }
            else
            {
                MessageBox.Show("Please select a composition to remove.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CompositionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        #endregion

        #region ViewModel

        public class CompositionsViewModel : INotifyPropertyChanged
        {
            private ObservableCollection<Composition> _compositions;

            public ObservableCollection<Composition> Compositions
            {
                get => _compositions;
                set => SetProperty(ref _compositions, value);
            }

            public CompositionsViewModel()
            {
                Compositions = new ObservableCollection<Composition>();
            }

            public void LoadCompositions(List<Composition> compositions)
            {
                Compositions.Clear();
                foreach (var composition in compositions)
                {
                    Compositions.Add(composition);
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
}
