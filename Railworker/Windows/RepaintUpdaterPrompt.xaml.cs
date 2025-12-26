using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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

namespace Railworker.Windows
{
    /// <summary>
    /// Interaction logic for RepaintUpdaterPrompt.xaml
    /// </summary>
    public partial class RepaintUpdaterPrompt : Window
    {
        class RepaintUpdaterPromptViewModel : ViewModel
        {
            private string _question = "";
            public string Question
            {
                get => _question;
                set => SetProperty(ref _question, value);
            }
        }

        public class RepaintUpdatePromptResult
        {
            public bool Keep { get; set; }
            public bool DoForAll { get; set; }
        }

        RepaintUpdaterPromptViewModel ViewModel { get; set; }
        Action<RepaintUpdatePromptResult>? callback;

        public RepaintUpdaterPrompt()
        {
            ViewModel = new RepaintUpdaterPromptViewModel();
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            CompletePrompt(new RepaintUpdatePromptResult
            {
                Keep = false,
                DoForAll = DoForAll.IsChecked ?? false
            });
        }

        private void Keep_Click(object sender, RoutedEventArgs e)
        {
            CompletePrompt(new RepaintUpdatePromptResult
            {
                Keep = true,
                DoForAll = DoForAll.IsChecked ?? false
            });
        }
        
        private void CompletePrompt(RepaintUpdatePromptResult result)
        {
            DoForAll.IsChecked = false;
            this.callback!(result);
        }

        public void Prompt(string question, Action<RepaintUpdatePromptResult> callback)
        {
            ViewModel.Question = question;
            this.callback = callback;
            Show();
            Activate();
            Topmost = true;
        }

        private class PreviousPromptWasNotFinishedException : Exception
        {
            public PreviousPromptWasNotFinishedException()
            {
            }

            public PreviousPromptWasNotFinishedException(string? message) : base(message)
            {
            }

            public PreviousPromptWasNotFinishedException(string? message, Exception? innerException) : base(message, innerException)
            {
            }
        }
    }
}
