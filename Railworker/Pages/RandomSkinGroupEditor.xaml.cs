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
                    Rarity = 1,
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

        #endregion

        #region ViewModel

        public class RandomSkinGroupEditorViewModel : INotifyPropertyChanged
        {
            private string _id;
            private ObservableCollection<RandomSkin> _randomSkins;
            private RandomSkin _selectedRandomSkin;
            private RandomSkin.SkinTexture _selectedSkinTexture;

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

            public RandomSkinGroupEditorViewModel()
            {
                // Initialize with default values
                Id = "";
                RandomSkins = new ObservableCollection<RandomSkin>();
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
