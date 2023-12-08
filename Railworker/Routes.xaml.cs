using RWLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace Railworker
{
    /// <summary>
    /// Interaction logic for Routes.xaml
    /// </summary>
    public partial class RoutesWindow : Window
    {
        public RoutesWindow()
        {
            InitializeComponent();
        }
        public ObservableCollection<Route> Routes { get; } = new ObservableCollection<Route>();
    }
}
