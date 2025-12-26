using RWLib.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Railworker.Pages
{
    /// <summary>
    /// Interaction logic for CompositionSelectorWindow.xaml
    /// </summary>
    public partial class CompositionSelectorWindow : Window
    {
        public Composition SelectedComposition { get; private set; }

        public CompositionSelectorWindow(List<Composition> compositions)
        {
            InitializeComponent();
            CompositionsListView.ItemsSource = compositions;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (CompositionsListView.SelectedItem is Composition composition)
            {
                SelectedComposition = composition;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a composition.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
