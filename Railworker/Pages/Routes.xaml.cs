using RWLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Railworker
{
    /// <summary>
    /// Interaction logic for Routes.xaml
    /// </summary>
    public partial class RoutesWindow : Page
    {
        public RoutesWindow()
        {
        }
        public ObservableCollection<Route> Routes { get; } = new ObservableCollection<Route>();
    }
}
