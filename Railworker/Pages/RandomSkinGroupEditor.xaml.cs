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
    /// Interaction logic for RandomSkinGroupEditor.xaml
    /// </summary>
    public partial class RandomSkinGroupEditor : Page
    {
        private RandomSkinGroupEditorViewModel _viewModel;
        private RWLibrary _rwLib;

        public RandomSkinGroupEditor()
        {
            InitializeComponent();
            _rwLib = ((App)Application.Current).RWLib;
            _viewModel = new RandomSkinGroupEditorViewModel();
            DataContext = _viewModel;
            this.StatusText.Text = "Create a new random skin group or open an existing one";
        }

        public RandomSkinGroupEditor(RandomSkinGroup randomSkinGroup)
        {
            InitializeComponent();
            _rwLib = ((App)Application.Current).RWLib;
            _viewModel = new RandomSkinGroupEditorViewModel();
            DataContext = _viewModel;
            
            if (randomSkinGroup != null)
            {
                _viewModel.LoadRandomSkinGroup(randomSkinGroup);
                this.StatusText.Text = $"Editing random skin group: {_viewModel.Id}";
            }
            else
            {
                this.StatusText.Text = "Create a new random skin group or open an existing one";
            }
        }

        public RandomSkinGroupEditor(RandomSkinGroup randomSkinGroup, List<Composition> compositions)
        {
            InitializeComponent();
            _rwLib = ((App)Application.Current).RWLib;
            _viewModel = new RandomSkinGroupEditorViewModel();
            DataContext = _viewModel;
            
            if (randomSkinGroup != null)
            {
                _viewModel.LoadRandomSkinGroup(randomSkinGroup);
                _viewModel.LoadCompositions(compositions);
                this.StatusText.Text = $"Editing random skin group: {_viewModel.Id}";
            }
            else
            {
                this.StatusText.Text = "Create a new random skin group or open an existing one";
            }
        }

        #region Event Handlers

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // This would typically save changes back to the parent collection
            // For now, we'll just show a message
            StatusText.Text = "Changes saved (note: this is a placeholder - actual saving happens in the parent page)";
        }

        private void AddRandomSkinButton_Click(object sender, RoutedEventArgs e)
        {
            var newRandomSkin = new RandomSkin
            {
                Id = $"new_skin_{DateTime.Now.Ticks}",
                Name = "New Random Skin",
                Composition = "",
                FullSkinsAmount = 36,
                Stacked = 1,
                Skins = new List<RandomSkin.SkinTexture>()
            };
            
            _viewModel.RandomSkins.Add(newRandomSkin);
            StatusText.Text = "Added new random skin";
        }

        private void RemoveRandomSkinButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedRandomSkin != null)
            {
                var result = MessageBox.Show($"Are you sure you want to remove the random skin '{_viewModel.SelectedRandomSkin.Name}'?", 
                    "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.RandomSkins.Remove(_viewModel.SelectedRandomSkin);
                    StatusText.Text = $"Removed random skin: {_viewModel.SelectedRandomSkin.Id}";
                }
            }
            else
            {
                MessageBox.Show("Please select a random skin to remove.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RandomSkinsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RandomSkinsListView.SelectedItem is RandomSkin randomSkin)
            {
                _viewModel.SelectedRandomSkin = randomSkin;
                RandomSkinDetailsGroup.Visibility = Visibility.Visible;
            }
            else
            {
                _viewModel.SelectedRandomSkin = null;
                RandomSkinDetailsGroup.Visibility = Visibility.Collapsed;
            }
        }

        private void AddSkinTextureButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedRandomSkin != null)
            {
                var newSkinTexture = new RandomSkin.SkinTexture
                {
                    Id = $"new_texture_{DateTime.Now.Ticks}",
                    Name = "New Skin Texture",
                    Group = "",
                    Texture = ""
                };
                
                _viewModel.SelectedRandomSkin.Skins.Add(newSkinTexture);
                StatusText.Text = "Added new skin texture";
                
                // Refresh the ListView
                SkinTexturesListView.Items.Refresh();
            }
            else
            {
                MessageBox.Show("Please select a random skin first.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveSkinTextureButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedSkinTexture != null && _viewModel.SelectedRandomSkin != null)
            {
                var result = MessageBox.Show($"Are you sure you want to remove the skin texture '{_viewModel.SelectedSkinTexture.Name}'?", 
                    "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.SelectedRandomSkin.Skins.Remove(_viewModel.SelectedSkinTexture);
                    StatusText.Text = $"Removed skin texture: {_viewModel.SelectedSkinTexture.Id}";
                    
                    // Refresh the ListView
                    SkinTexturesListView.Items.Refresh();
                }
            }
            else
            {
                MessageBox.Show("Please select a skin texture to remove.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SkinTexturesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SkinTexturesListView.SelectedItem is RandomSkin.SkinTexture skinTexture)
            {
                _viewModel.SelectedSkinTexture = skinTexture;
                SkinTextureDetailsGroup.Visibility = Visibility.Visible;
            }
            else
            {
                _viewModel.SelectedSkinTexture = null;
                SkinTextureDetailsGroup.Visibility = Visibility.Collapsed;
            }
        }

        private void PreviewRandomSkinButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedRandomSkin != null)
            {
                // Open preview window for the selected RandomSkin
                var previewWindow = new RandomSkinPreviewWindow(_viewModel.SelectedRandomSkin, _viewModel.AvailableCompositions, _rwLib);
                previewWindow.Show();
            }
            else
            {
                MessageBox.Show("Please select a RandomSkin to preview.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void GenerateThumbnailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedRandomSkin != null)
            {
                try
                {
                    StatusText.Text = "Generating thumbnails...";
                    
                    var composition = _viewModel.AvailableCompositions.FirstOrDefault(c => c.Id == _viewModel.SelectedRandomSkin.Composition);
                    if (composition == null)
                    {
                        MessageBox.Show("Composition not found for this RandomSkin.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Create thumbnails directory
                    var thumbnailsDir = System.IO.Path.Combine(Environment.CurrentDirectory, "thumbnails", _viewModel.Id);
                    Directory.CreateDirectory(thumbnailsDir);

                    int count = 0;
                    foreach (var skinTexture in _viewModel.SelectedRandomSkin.Skins)
                    {
                        if (!string.IsNullOrEmpty(skinTexture.Texture))
                        {
                            // Generate thumbnail using similar logic to RandomContainerGenerator
                            await GenerateThumbnail(skinTexture, composition, thumbnailsDir, count++);
                        }
                    }

                    StatusText.Text = $"Generated {count} thumbnails in {thumbnailsDir}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error generating thumbnails: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Error generating thumbnails";
                }
            }
            else
            {
                MessageBox.Show("Please select a RandomSkin to generate thumbnails for.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SelectCompositionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedRandomSkin != null && _viewModel.AvailableCompositions.Count > 0)
            {
                var compositionSelector = new CompositionSelectorWindow(_viewModel.AvailableCompositions);
                if (compositionSelector.ShowDialog() == true)
                {
                    _viewModel.SelectedRandomSkin.Composition = compositionSelector.SelectedComposition.Id;
                    StatusText.Text = $"Selected composition: {compositionSelector.SelectedComposition.Name}";
                }
            }
            else
            {
                MessageBox.Show("No compositions available or no RandomSkin selected.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditCompositionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedRandomSkin != null && !string.IsNullOrEmpty(_viewModel.SelectedRandomSkin.Composition))
            {
                var composition = _viewModel.AvailableCompositions.FirstOrDefault(c => c.Id == _viewModel.SelectedRandomSkin.Composition);
                if (composition != null)
                {
                    var mainWindow = (MainWindow)Application.Current.MainWindow;
                    mainWindow.OpenTab(new MainWindow.MainWindowViewModel.Tab
                    {
                        FrameContent = new CompositionEditor(composition),
                        Header = $"Edit: {composition.Name}"
                    });
                }
                else
                {
                    MessageBox.Show("Composition not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("No composition selected.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task GenerateThumbnail(RandomSkin.SkinTexture skinTexture, Composition composition, string outputDir, int index)
        {
            // This is a simplified version of the thumbnail generation from RandomContainerGenerator
            // You would need to implement the full logic based on your requirements
            try
            {
                var basePath = System.IO.Path.Combine(_rwLib.TSPath, "Assets\\Alex95\\ContainerPack01\\RailNetwork\\Interactive");
                basePath = string.IsNullOrEmpty(composition.BasePath) ? basePath : System.IO.Path.Combine(_rwLib.TSPath, "Assets", composition.BasePath);

                var inputFile = System.IO.Path.Combine(basePath, skinTexture.Texture);
                if (!File.Exists(inputFile))
                {
                    Console.WriteLine($"Texture file not found: {inputFile}");
                    return;
                }

                // For now, just copy the file as a placeholder
                // In a full implementation, you would process the image according to the composition projections
                var outputFile = System.IO.Path.Combine(outputDir, $"{index:D2}_{skinTexture.Id}.png");
                File.Copy(inputFile, outputFile, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating thumbnail for {skinTexture.Id}: {ex.Message}");
            }
        }

        #endregion

        #region ViewModel

        public class RandomSkinGroupEditorViewModel : INotifyPropertyChanged
        {
            private string _id;
            private ObservableCollection<RandomSkin> _randomSkins;
            private RandomSkin _selectedRandomSkin;
            private RandomSkin.SkinTexture _selectedSkinTexture;
            private List<Composition> _availableCompositions;

            public string Id
            {
                get => _id;
                set => SetProperty(ref _id, value);
            }

            public ObservableCollection<RandomSkin> RandomSkins
            {
                get => _randomSkins;
                set => SetProperty(ref _randomSkins, value);
            }

            public RandomSkin SelectedRandomSkin
            {
                get => _selectedRandomSkin;
                set => SetProperty(ref _selectedRandomSkin, value);
            }

            public RandomSkin.SkinTexture SelectedSkinTexture
            {
                get => _selectedSkinTexture;
                set => SetProperty(ref _selectedSkinTexture, value);
            }

            public List<Composition> AvailableCompositions
            {
                get => _availableCompositions ?? new List<Composition>();
                set => SetProperty(ref _availableCompositions, value);
            }

            public RandomSkinGroupEditorViewModel()
            {
                // Initialize with default values
                Id = "";
                RandomSkins = new ObservableCollection<RandomSkin>();
                AvailableCompositions = new List<Composition>();
            }

            public void LoadRandomSkinGroup(RandomSkinGroup randomSkinGroup)
            {
                Id = randomSkinGroup.Id;
                
                RandomSkins.Clear();
                foreach (var randomSkin in randomSkinGroup.RandomSkins)
                {
                    RandomSkins.Add(randomSkin);
                }
            }

            public void LoadCompositions(List<Composition> compositions)
            {
                AvailableCompositions = compositions ?? new List<Composition>();
            }

            public RandomSkinGroup GetRandomSkinGroup()
            {
                var randomSkinGroup = new RandomSkinGroup
                {
                    Id = Id,
                    RandomSkins = RandomSkins.ToList()
                };
                
                return randomSkinGroup;
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
