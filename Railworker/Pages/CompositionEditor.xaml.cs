using Microsoft.Win32;
using RWLib;
using RWLib.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using RailworkerMegaFreightPack1;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
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
using System.Diagnostics;

namespace Railworker.Pages
{
    /// <summary>
    /// Interaction logic for CompositionEditor.xaml
    /// </summary>
    public partial class CompositionEditor : Page
    {
        private CompositionEditorViewModel _viewModel;
        private RWLibrary _rwLib;
        private List<string> _loadedTextures = new List<string>();
        
        // Using shared directories to remember the last directory for different file types

        public CompositionEditor()
        {
            InitializeComponent();
            _rwLib = ((App)Application.Current).RWLib;
            _viewModel = new CompositionEditorViewModel();
            DataContext = _viewModel;
            this.StatusText.Text = "Create a new composition or open an existing one";
        }

        public CompositionEditor(Composition composition)
        {
            InitializeComponent();
            _rwLib = ((App)Application.Current).RWLib;
            _viewModel = new CompositionEditorViewModel();
            DataContext = _viewModel;
            
            if (composition != null)
            {
                _viewModel.LoadComposition(composition);
                this.StatusText.Text = $"Editing composition: {_viewModel.Id}";
            }
            else
            {
                this.StatusText.Text = "Create a new composition or open an existing one";
            }
        }

        #region Event Handlers

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Open Composition File"
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
                    // Save the directory for next time
                    SharedDirectories.LastJsonDirectory = System.IO.Path.GetDirectoryName(openFileDialog.FileName);
                    
                    string jsonContent = File.ReadAllText(openFileDialog.FileName);
                    var compositions = Composition.FromJson(jsonContent);
                    
                    if (compositions != null && compositions.Count > 0)
                    {
                        _viewModel.LoadComposition(compositions[0]);
                        this.StatusText.Text = $"Loaded composition: {_viewModel.Id}";
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
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Save Composition File"
            };
            
            // Set the initial directory if we have a saved one
            if (!string.IsNullOrEmpty(SharedDirectories.LastJsonDirectory) && Directory.Exists(SharedDirectories.LastJsonDirectory))
            {
                saveFileDialog.InitialDirectory = SharedDirectories.LastJsonDirectory;
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Save the directory for next time
                    SharedDirectories.LastJsonDirectory = System.IO.Path.GetDirectoryName(saveFileDialog.FileName);
                    
                    var composition = _viewModel.GetComposition();
                    var compositions = new List<Composition> { composition };
                    
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    
                    string jsonContent = JsonSerializer.Serialize(compositions, options);
                    File.WriteAllText(saveFileDialog.FileName, jsonContent);
                    
                    this.StatusText.Text = $"Saved composition to: {saveFileDialog.FileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            var addPixelMargins = AddMarginsCheckBox.IsChecked == true;

            Task.Run(async () =>
            {

                try
                {
                    // Check if we have any textures loaded
                    if (_loadedTextures == null || _loadedTextures.Count == 0)
                    {
                        MessageBox.Show("Please load textures first using the 'Add Textures' button.",
                            "No Textures Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Get the composition from the view model
                    var composition = _viewModel.GetComposition();

                    // Create a new image with the composed dimensions
                    var composedImage = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(
                        composition.ComposedImageWidth,
                        composition.ComposedImageHeight);

                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        this.StatusText.Text = $"Generating preview using all loaded textures...";
                    });

                    // Create a dummy RandomSkin with all loaded textures
                    var randomSkin = new RandomSkin
                    {
                        Id = "Preview",
                        Name = "Preview",
                        Composition = composition.Id,
                        Stacked = 1,
                        Skins = new List<RandomSkin.SkinTexture>()
                    };

                    // Add all loaded textures to the RandomSkin
                    for (int i = 0; i < _loadedTextures.Count; i++)
                    {
                        var textureFile = _loadedTextures[i];

                        // Create a SkinTexture for this texture
                        var skinTexture = new RandomSkin.SkinTexture
                        {
                            Texture = textureFile,
                            Name = System.IO.Path.GetFileNameWithoutExtension(textureFile),
                            Group = "Preview",
                            Id = $"Preview_{i}",
                            Rarity = 1
                        };

                        randomSkin.Skins.Add(skinTexture);
                    }

                    // Calculate grid layout for the textures
                    int texturesPerRow = (int)Math.Ceiling(Math.Sqrt(_loadedTextures.Count));

                    // Create tasks for each texture
                    var x = 0;
                    var y = 0;
                    var cargoNumber = 1;
                    var stackIndex = 0;

                    for (int i = 0; i < randomSkin.Skins.Count; i++)
                    {
                        var skin = randomSkin.Skins[i];
                        var stackOffset = composition.StylusYInterval * stackIndex;

                        // Create a ComposedTextureGenerator instance for this texture
                        var generator = new RandomContainerGenerator.ComposedTextureGenerator
                        {
                            RWLib = _rwLib,
                            Texture = skin,
                            Composition = composition,
                            ComposedImage = composedImage,
                            BaseX = x,
                            BaseY = y + stackOffset,
                            CargoNumber = cargoNumber++,
                            RandomSkin = randomSkin
                        };

                        // Build the composed image for this texture
                        await generator.Build(System.Threading.CancellationToken.None);

                        // Update position for next texture
                        if (++stackIndex >= randomSkin.Stacked)
                        {
                            x += composition.StylusXInterval;
                            if (x >= composition.StylusXInterval * composition.ComposedImageColumns)
                            {
                                x = 0;
                                y += composition.StylusYInterval * randomSkin.Stacked;
                            }
                            stackIndex = 0;
                        }
                    }

                    // Apply pixel margins if enabled
                    if (addPixelMargins)
                    {
                        RandomContainerGenerator.AddPixelmarginsWherePossible(composedImage);
                    }


                    // Convert the ImageSharp image to a BitmapImage for display
                    using (var memoryStream = new MemoryStream())
                    {
                        memoryStream.Position = 0;
                        composedImage.Save(memoryStream, new SixLabors.ImageSharp.Formats.Png.PngEncoder());

                        Application.Current.Dispatcher.Invoke(delegate
                        {
                            var bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.StreamSource = memoryStream;
                            bitmapImage.EndInit();
                            bitmapImage.Freeze();

                            // Display the preview
                            PreviewImage.Source = bitmapImage;
                            this.StatusText.Text = "Preview generated successfully.";
                        });
                    }
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        this.StatusText.Text = "Error generating preview.";
                    });
                    MessageBox.Show($"Error generating preview: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Export JSON File"
            };
            
            // Set the initial directory if we have a saved one
            if (!string.IsNullOrEmpty(SharedDirectories.LastJsonDirectory) && Directory.Exists(SharedDirectories.LastJsonDirectory))
            {
                saveFileDialog.InitialDirectory = SharedDirectories.LastJsonDirectory;
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Save the directory for next time
                    SharedDirectories.LastJsonDirectory = System.IO.Path.GetDirectoryName(saveFileDialog.FileName);
                    
                    var composition = _viewModel.GetComposition();
                    var compositions = new List<Composition> { composition };
                    
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    
                    string jsonContent = JsonSerializer.Serialize(compositions, options);
                    File.WriteAllText(saveFileDialog.FileName, jsonContent);
                    
                    this.StatusText.Text = $"Exported JSON to: {saveFileDialog.FileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddProjectionButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AddProjection();
        }

        private void RemoveProjectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedProjection != null)
            {
                _viewModel.RemoveProjection(_viewModel.SelectedProjection);
            }
            else
            {
                MessageBox.Show("Please select a projection to remove.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ProjectionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectionsListView.SelectedItem is Composition.Projection projection)
            {
                _viewModel.SelectedProjection = projection;
                ProjectionDetailsGroup.Visibility = Visibility.Visible;
            }
            else
            {
                _viewModel.SelectedProjection = null;
                ProjectionDetailsGroup.Visibility = Visibility.Collapsed;
            }
        }

        private void TexturesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TexturesListView.SelectedItem != null)
            {
                // When a texture is selected, update the preview
                PreviewButton_Click(sender, e);
                
                // Also update the mapping preview
                ShowMappingButton_Click(sender, e);
            }
        }

        private void ShowMappingButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected texture or use the first one if none is selected
            string textureFile = TexturesListView.SelectedItem != null
                    ? _loadedTextures[TexturesListView.SelectedIndex]
                    : _loadedTextures.First();
            Task.Run(async () =>
            {
                try
                {
                    // Check if we have any textures loaded
                    if (_loadedTextures == null || _loadedTextures.Count == 0)
                    {
                        MessageBox.Show("Please load textures first using the 'Add Textures' button.",
                            "No Textures Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Get the composition from the view model
                    var composition = _viewModel.GetComposition();

                    // Create a dummy RandomSkin.SkinTexture for the ComposedTextureGenerator
                    var skinTexture = new RandomSkin.SkinTexture
                    {
                        Texture = textureFile,
                        Name = System.IO.Path.GetFileNameWithoutExtension(textureFile),
                        Group = "Preview",
                        Id = "Preview",
                        Rarity = 1
                    };

                    // Create a dummy RandomSkin for the ComposedTextureGenerator
                    var randomSkin = new RandomSkin
                    {
                        Id = "Preview",
                        Name = "Preview",
                        Composition = composition.Id,
                        Stacked = 1
                    };

                    // Create a ComposedTextureGenerator instance
                    var generator = new RandomContainerGenerator.ComposedTextureGenerator
                    {
                        RWLib = _rwLib,
                        Texture = skinTexture,
                        Composition = composition,
                        ComposedImage = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(1, 1), // Dummy image, not used for mapping
                        BaseX = 0,
                        BaseY = 0,
                        CargoNumber = 1,
                        RandomSkin = randomSkin
                    };

                    // Use the DrawMapping method to generate the mapping preview
                    var mappingImage = await generator.DrawMapping(true, System.Threading.CancellationToken.None);

                    // Convert the ImageSharp image to a BitmapImage for display
                    using (var memoryStream = new MemoryStream())
                    {
                        mappingImage.Save(memoryStream, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                        memoryStream.Position = 0;

                        Application.Current.Dispatcher.Invoke(delegate
                        {
                            var bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.StreamSource = memoryStream;
                            bitmapImage.EndInit();
                            bitmapImage.Freeze();

                            // Display the mapping preview
                            MappingPreviewImage.Source = bitmapImage;

                            this.StatusText.Text = "Mapping preview generated successfully.";
                        });
                    }
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {

                        MessageBox.Show($"Error generating mapping preview: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.StatusText.Text = "Error generating mapping preview.";
                    });
                }
            });
        }
        
        private void ExportPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if we have a preview image
                if (PreviewImage.Source == null)
                {
                    MessageBox.Show("Please generate a preview first by clicking the 'Preview Composition' button.", 
                        "No Preview Available", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                // Create a save file dialog
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PNG Files (*.png)|*.png|All Files (*.*)|*.*",
                    Title = "Export Preview Image"
                };
                
                // Set the initial directory if we have a saved one
                if (!string.IsNullOrEmpty(SharedDirectories.LastTextureDirectory) && Directory.Exists(SharedDirectories.LastTextureDirectory))
                {
                    saveFileDialog.InitialDirectory = SharedDirectories.LastTextureDirectory;
                }
                
                if (saveFileDialog.ShowDialog() == true)
                {
                    // Save the directory for next time
                    SharedDirectories.LastTextureDirectory = System.IO.Path.GetDirectoryName(saveFileDialog.FileName);
                    
                    // Convert the BitmapImage to a PNG file
                    var bitmapImage = (BitmapImage)PreviewImage.Source;
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                    
                    using (var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    {
                        encoder.Save(fileStream);
                    }
                    
                    this.StatusText.Text = $"Preview exported to: {saveFileDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting preview: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.StatusText.Text = "Error exporting preview.";
            }
        }

        private void AddTexturesButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.tga;*.dds;*.tgpcdx)|*.png;*.jpg;*.jpeg;*.bmp;*.tga;*.dds;*.tgpcdx|All Files (*.*)|*.*",
                    Title = "Select Texture Files",
                    Multiselect = true
                };
            
                // Set the initial directory if we have a saved one
                if (!string.IsNullOrEmpty(SharedDirectories.LastTextureDirectory) && Directory.Exists(SharedDirectories.LastTextureDirectory))
                {
                    openFileDialog.InitialDirectory = SharedDirectories.LastTextureDirectory;
                }

                if (openFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        // Save the directory for next time
                        SharedDirectories.LastTextureDirectory = System.IO.Path.GetDirectoryName(openFileDialog.FileNames[0]);

                        Application.Current.Dispatcher.Invoke(delegate
                        {
                            this.StatusText.Text = "Loading textures...";
                        });
                    
                        _loadedTextures.Clear();
                        List<string> loadedFileNames = new List<string>();
                    
                        foreach (string filename in openFileDialog.FileNames)
                        {
                            try
                            {
                                // Use RWImageDecoder to load the texture to verify it can be loaded
                                var image = (SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>)await _rwLib.ImageDecoder.FromFilename(filename);
                            
                                // Add the full path to our list of loaded textures
                                _loadedTextures.Add(filename);
                            
                                // Add just the filename (not the full path) to the list for display
                                loadedFileNames.Add(System.IO.Path.GetFileName(filename));
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error loading texture {System.IO.Path.GetFileName(filename)}: {ex.Message}", 
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }


                        Application.Current.Dispatcher.Invoke(delegate
                        {

                            // Update the TexturesListView with the loaded file names
                            TexturesListView.ItemsSource = loadedFileNames;

                            if (_loadedTextures.Count > 0)
                            {
                                this.StatusText.Text = $"Loaded {_loadedTextures.Count} textures: {string.Join(", ", loadedFileNames)}";

                                // Select the first texture and generate a preview
                                TexturesListView.SelectedIndex = 0;
                            }
                            else
                            {
                                this.StatusText.Text = "No textures were loaded.";
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(delegate
                        {

                            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                            this.StatusText.Text = "Error loading textures.";
                        });
                    }
                }
            });
        }

        #endregion

        #region ViewModel

        public class CompositionEditorViewModel : INotifyPropertyChanged
        {
            private string _id;
            private string _name;
            private string _basePath;
            private int _fullSkinsAmount;
            private int _composedImageWidth;
            private int _composedImageHeight;
            private int _inputImageResizeWidth;
            private int _inputImageResizeHeight;
            private int _stylusXInterval;
            private int _stylusYInterval;
            private int _composedImageColumns;
            private int _composedImageRows;
            private float _outputScaleX;
            private float _outputScaleY;
            private ObservableCollection<Composition.Projection> _projections;
            private Composition.Projection _selectedProjection;
            private ObservableCollection<RandomSkin> _randomSkins;

            public string Id
            {
                get => _id;
                set => SetProperty(ref _id, value);
            }

            public string Name
            {
                get => _name;
                set => SetProperty(ref _name, value);
            }

            public string BasePath
            {
                get => _basePath;
                set => SetProperty(ref _basePath, value);
            }

            public int FullSkinsAmount
            {
                get => _fullSkinsAmount;
                set => SetProperty(ref _fullSkinsAmount, value);
            }

            public int ComposedImageWidth
            {
                get => _composedImageWidth;
                set => SetProperty(ref _composedImageWidth, value);
            }

            public int ComposedImageHeight
            {
                get => _composedImageHeight;
                set => SetProperty(ref _composedImageHeight, value);
            }

            public int InputImageResizeWidth
            {
                get => _inputImageResizeWidth;
                set => SetProperty(ref _inputImageResizeWidth, value);
            }

            public int InputImageResizeHeight
            {
                get => _inputImageResizeHeight;
                set => SetProperty(ref _inputImageResizeHeight, value);
            }

            public int StylusXInterval
            {
                get => _stylusXInterval;
                set => SetProperty(ref _stylusXInterval, value);
            }

            public int StylusYInterval
            {
                get => _stylusYInterval;
                set => SetProperty(ref _stylusYInterval, value);
            }

            public int ComposedImageColumns
            {
                get => _composedImageColumns;
                set => SetProperty(ref _composedImageColumns, value);
            }

            public int ComposedImageRows
            {
                get => _composedImageRows;
                set => SetProperty(ref _composedImageRows, value);
            }

            public float OutputScaleX
            {
                get => _outputScaleX;
                set => SetProperty(ref _outputScaleX, value);
            }

            public float OutputScaleY
            {
                get => _outputScaleY;
                set => SetProperty(ref _outputScaleY, value);
            }

            public ObservableCollection<Composition.Projection> Projections
            {
                get => _projections;
                set => SetProperty(ref _projections, value);
            }

            public Composition.Projection SelectedProjection
            {
                get => _selectedProjection;
                set => SetProperty(ref _selectedProjection, value);
            }

            public ObservableCollection<RandomSkin> RandomSkins
            {
                get => _randomSkins;
                set => SetProperty(ref _randomSkins, value);
            }

            public CompositionEditorViewModel()
            {
                // Initialize with default values
                Id = "";
                Name = "";
                BasePath = "";
                FullSkinsAmount = 36;
                ComposedImageWidth = 2048;
                ComposedImageHeight = 2048;
                InputImageResizeWidth = 512;
                InputImageResizeHeight = 512;
                StylusXInterval = 512;
                StylusYInterval = 227;
                ComposedImageColumns = 4;
                ComposedImageRows = 4;
                OutputScaleX = 1.0f;
                OutputScaleY = 1.0f;
                Projections = new ObservableCollection<Composition.Projection>();
                RandomSkins = new ObservableCollection<RandomSkin>();
            }

            public void LoadComposition(Composition composition)
            {
                Id = composition.Id;
                Name = composition.Name;
                BasePath = composition.BasePath;
                FullSkinsAmount = composition.FullSkinsAmount;
                ComposedImageWidth = composition.ComposedImageWidth;
                ComposedImageHeight = composition.ComposedImageHeight;
                InputImageResizeWidth = composition.InputImageResizeWidth;
                InputImageResizeHeight = composition.InputImageResizeHeight;
                StylusXInterval = composition.StylusXInterval;
                StylusYInterval = composition.StylusYInterval;
                ComposedImageColumns = composition.ComposedImageColumns;
                ComposedImageRows = composition.ComposedImageRows;
                OutputScaleX = composition.OutputScaleX;
                OutputScaleY = composition.OutputScaleY;
                
                Projections.Clear();
                foreach (var projection in composition.Projections)
                {
                    Projections.Add(projection);
                }
                
                // In a real implementation, we would also load the RandomSkins that use this composition
                // For now, we'll leave the RandomSkins collection empty
            }

            public Composition GetComposition()
            {
                var composition = new Composition
                {
                    Id = Id,
                    Name = Name,
                    BasePath = BasePath,
                    FullSkinsAmount = FullSkinsAmount,
                    ComposedImageWidth = ComposedImageWidth,
                    ComposedImageHeight = ComposedImageHeight,
                    InputImageResizeWidth = InputImageResizeWidth,
                    InputImageResizeHeight = InputImageResizeHeight,
                    StylusXInterval = StylusXInterval,
                    StylusYInterval = StylusYInterval,
                    ComposedImageColumns = ComposedImageColumns,
                    ComposedImageRows = ComposedImageRows,
                    OutputScaleX = OutputScaleX,
                    OutputScaleY = OutputScaleY,
                    Projections = Projections.ToList()
                };
                
                return composition;
            }

            public void AddProjection()
            {
                var projection = new Composition.Projection
                {
                    Name = $"Projection {Projections.Count + 1}",
                    SourceBbox = new Composition.Bbox
                    {
                        X = 0,
                        Y = 0,
                        Width = 100,
                        Height = 100,
                        Rotate = "None"
                    },
                    DestBbox = new Composition.Bbox
                    {
                        X = 0,
                        Y = 0,
                        Width = 100,
                        Height = 100,
                        Rotate = "None"
                    }
                };
                
                Projections.Add(projection);
                SelectedProjection = projection;
            }

            public void RemoveProjection(Composition.Projection projection)
            {
                Projections.Remove(projection);
                if (SelectedProjection == projection)
                {
                    SelectedProjection = null;
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
