using Railworker.Core;
using RWLib;
using RWLib.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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

namespace Railworker.Pages
{
    /// <summary>
    /// Interaction logic for ConsistCreator.xaml
    /// </summary>
    public partial class ConsistCreator : Page
    {
        public class ConsistCreatorViewModel : ViewModel
        {
            public string Provider { get; set; } = "RailworkerPreloads";
            public string Product { get; set; } = "";
            public string Name { get; set; } = "";
            public bool Reversed { get; set; } = false;
            public required Consist Consist { get; set; }
        }

        internal App App { get => (App)Application.Current; }
        internal Logger Logger { get => App.Logger; }

        ConsistCreatorViewModel ViewModel { get; set; }

        public ConsistCreator(Consist consist)
        {
            ViewModel = new ConsistCreatorViewModel
            {
                Consist = consist,
                Product = "<type your category here>",
                Name = "[RP] " + consist.Name
            };

            DataContext = ViewModel;

            InitializeComponent();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var consistBlueprint = await App.RWLib!.BlueprintLoader.CreateConsistBlueprint(
                    ViewModel.Provider,
                    ViewModel.Product,
                    System.IO.Path.ChangeExtension(System.IO.Path.Combine("Preload", ViewModel.Name), ".bin"),
                    ViewModel.Consist.RWConsist,
                    ViewModel.Reversed
                );

                var result = await App.RWLib.Serializer.SerializeWithSerzExe(consistBlueprint.Xml.Document!);

                var combinedPath = consistBlueprint.BlueprintId.CombinedPath;
                var filepath = System.IO.Path.Combine(App.RWLib.TSPath, "Assets", combinedPath);

                if (System.IO.File.Exists(filepath))
                {
                    var ex = new FileAlreadyExistsException(combinedPath);
                    Logger.Error(ex);
                    MessageBox.Show(Railworker.Language.Resources.file_already_exists + ": " + ex.ToString(), Railworker.Language.Resources.msg_error, MessageBoxButton.OK, MessageBoxImage.Error);
                    throw ex;
                }

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filepath)!);
                System.IO.File.Move(result, filepath);

                MessageBox.Show(Railworker.Language.Resources.success, Railworker.Language.Resources.msg_message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                MessageBox.Show(ex.ToString(), Railworker.Language.Resources.msg_error, MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }
}
