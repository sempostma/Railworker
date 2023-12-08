using Railworker.Pages;
using RWLib.RWBlueprints.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Railworker.UserControls
{
    /// <summary>
    /// Interaction logic for DisplayNameUserControl.xaml
    /// </summary>
    public partial class DisplayNameUserControl : UserControl
    {
        private readonly string[] languages = ["English", "French", "Italian", "German", "Spanish", "Dutch", "Polish", "Russian", "Other", "Key"];

        public string English { get => DisplayName?.GetDisplayName("English") ?? ""; set => DisplayName?.SetDisplayName("English", value); }
        public string French { get => DisplayName?.GetDisplayName("French") ?? ""; set => DisplayName?.SetDisplayName("French", value); }
        public string Italian { get => DisplayName?.GetDisplayName("Italian") ?? ""; set => DisplayName?.SetDisplayName("Italian", value); }
        public string German { get => DisplayName?.GetDisplayName("German") ?? ""; set => DisplayName?.SetDisplayName("German", value); }
        public string Spanish { get => DisplayName?.GetDisplayName("Spanish") ?? ""; set => DisplayName?.SetDisplayName("Spanish", value); }
        public string Dutch { get => DisplayName?.GetDisplayName("Dutch") ?? ""; set => DisplayName?.SetDisplayName("Dutch", value); }
        public string Polish { get => DisplayName?.GetDisplayName("Polish") ?? ""; set => DisplayName?.SetDisplayName("Polish", value); }
        public string Russian { get => DisplayName?.GetDisplayName("Russian") ?? ""; set => DisplayName?.SetDisplayName("Russian", value); }
        public string Other { get => DisplayName?.GetDisplayName("Other") ?? ""; set => DisplayName?.SetDisplayName("Other", value); }
        public string Key { get => DisplayName?.GetDisplayName("Key") ?? ""; set => DisplayName?.SetDisplayName("Key", value); }

        public RWDisplayName? DisplayName
        {
            get { return (RWDisplayName?)GetValue(DisplayNameProperty); }
            set { SetValue(DisplayNameProperty, value); }
        }

        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register(
                "DisplayName",
                typeof(RWDisplayName),
                typeof(DisplayNameUserControl)
        );

        public DisplayNameUserControl()
        {
            InitializeComponent();
        }
    }
}
