using Railworker.Core;
using Railworker.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Railworker
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public class LanguageListItem
        {
            public string Name { get; set; } = "";
            public string Value { get; set; } = "";
        }

        internal App App { get => (App)Application.Current; }
        internal Logger Logger { get => App.Logger; }
        public ObservableCollection<LanguageListItem> LanguageList { get; } = new ObservableCollection<LanguageListItem>();


        public SettingsWindow()
        {
            InitializeComponent();

            DataContext = this;
            LanguageList.Add(new LanguageListItem
            {
                Name = Railworker.Language.Resources.use_system_language,
                Value = ""
            });
            //LanguageList.Add(new LanguageListItem
            //{
            //    Name = "Deutsch",
            //    Value = "de"
            //});
            LanguageList.Add(new LanguageListItem
            {
                Name = "English",
                Value = "en"
            });
            //LanguageList.Add(new LanguageListItem
            //{
            //    Name = "Nederlands",
            //    Value = "nl"
            //});
            //LanguageList.Add(new LanguageListItem
            //{
            //    Name = "Русский",
            //    Value = "ru"
            //});
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Logger.Debug($"Change language to {Settings.Default.Language}");
            App.SetLanguageDictionary();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
