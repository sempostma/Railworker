using Exceptions;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using Railworker.Core;
using RWLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
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
using System.Xml;
using System.Xml.Linq;
using static Railworker.Core.RailworkerFiles;

namespace Railworker.Pages
{
    /// <summary>
    /// Interaction logic for FileEditor.xaml
    /// </summary>
    public partial class FileEditor : Page
    {
        public class FileEditorViewModel : ViewModel
        {
            private string _fileContents = "";
            public string FileContents
            {
                get => _fileContents; 
                set
                {
                    SetProperty(ref _fileContents, value);
                }
            }

            private IHighlightingDefinition? _syntaxHighlighting = null;
            public IHighlightingDefinition? SyntaxHighlighting { get => _syntaxHighlighting; set => SetProperty(ref _syntaxHighlighting, value); }
            public FileFormat FileFormat { get; set; }
            public FileContentType FileContentType { get; set; }
            public required string Filename { get; set; }
        }

        internal App App { get => (App)Application.Current; }
        internal Logger Logger { get => App.Logger; }

        public static string[] AllowedFileExtensions { get; } = RailworkerFiles.AllowedTextEditorFileExtensions;

        public static bool CanOpen(string extension)
        {
            return RailworkerFiles.CanOpenInTextEditor(extension);
        }

        public static Task<OpenFileForTextEditorResult> OpenFile(string filename, RWLibrary rWLibrary)
        {
            return RailworkerFiles.OpenFileForTextEditor(filename, rWLibrary);
        }

        public FileEditorViewModel ViewModel;

        public FileEditor(string fileContents, string filename)
        {
            var ext = System.IO.Path.GetExtension(filename).TrimStart('.');
            var fileContentType = DetermineFileContentType(ext);
            var fileFormat = DetermineFileFormat(ext);
            var highlighting = GetAvalonSyntax(fileContentType);

            ViewModel = new FileEditorViewModel
            {
                FileContents = fileContents,
                Filename = filename,
                FileContentType = fileContentType,
                FileFormat = fileFormat,
                SyntaxHighlighting = highlighting
            };
            DataContext = ViewModel;
            InitializeComponent();
            TextEditor.Text = fileContents;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            TextEditor.TextChanged += TextEditor_TextChanged;
        }

        private void TextEditor_TextChanged(object? sender, EventArgs e)
        {
            if (TextEditor.Text != ViewModel.FileContents)
            {
                ViewModel.FileContents = TextEditor.Text;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FileContents")
            {
                string text = (sender as string)!;
                if (TextEditor.Text != text)
                {
                    TextEditor.Text = text;
                }
            }
        }

        public Task SaveFile()
        {
            return Task.Run(async () =>
            {
                var xml = XDocument.Parse(ViewModel.FileContents);
                var temporaryFile = await App.RWLib!.Serializer.SerializeWithSerzExe(xml);

                string args = string.Format("/e, /select, \"{0}\"", temporaryFile);

                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = "explorer";
                info.Arguments = args;
                Process.Start(info);
            });
        }
    }
}
