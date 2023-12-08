using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Railworker.Pages;
using RWLib;
using System.Runtime.ExceptionServices;
using Exceptions;

namespace Railworker.Core
{
    public static class RailworkerFiles
    {
        internal static App App { get => (App)Application.Current; }
        internal static Logger Logger { get => App.Logger; }

        public enum FileFormat { GeoPcDx, Bin, Xml, Dcsv, Csv, TgPcDx, XSec, ProxyBin, Txt, Lua, Luac, Pdf, Dds, Bat, Wav, Dav, Other }
        public enum FileContentType
        {
            TSBin, Binary, Xml, Csv, Lua, Pdf, Txt,
            Other
        }

        public static readonly Dictionary<string, FileFormat> FileExtensionToFileFormat = new Dictionary<string, FileFormat>
        {
            { "xml", FileFormat.Xml },
            { "GeoPcDx", FileFormat.GeoPcDx },
            { "bin", FileFormat.Bin },
            { "dcsv", FileFormat.Dcsv },
            { "csv", FileFormat.Csv },
            { "TgPcDx", FileFormat.TgPcDx },
            { "proxybin", FileFormat.ProxyBin },
            { "XSec", FileFormat.XSec },
            { "txt", FileFormat.Txt },
            { "lua", FileFormat.Lua },
            { "luac", FileFormat.Luac },
            { "pdf", FileFormat.Pdf },
            { "bat", FileFormat.Bat },
            { "cmd", FileFormat.Bat },
            { "wav", FileFormat.Wav },
            { "dav", FileFormat.Dav },
        };

        public static readonly Dictionary<FileFormat, FileContentType> FileFormatToFileContentTypeMapper = new Dictionary<FileFormat, FileContentType>
        {
            { FileFormat.Bin, FileContentType.TSBin },
            { FileFormat.GeoPcDx, FileContentType.TSBin },
            { FileFormat.TgPcDx, FileContentType.TSBin },
            { FileFormat.ProxyBin, FileContentType.TSBin },
            { FileFormat.XSec, FileContentType.TSBin },

            { FileFormat.Xml, FileContentType.Xml },

            { FileFormat.Dcsv, FileContentType.Csv },
            { FileFormat.Csv, FileContentType.Csv },

            { FileFormat.Txt, FileContentType.Txt },

            { FileFormat.Lua, FileContentType.Lua },

            { FileFormat.Pdf, FileContentType.Pdf },

            { FileFormat.Luac, FileContentType.Binary },
            { FileFormat.Dds, FileContentType.Binary },
            { FileFormat.Wav, FileContentType.Binary },
            { FileFormat.Dav, FileContentType.Binary },
        };

        public static readonly Dictionary<FileContentType, IHighlightingDefinition> FileContentTypeToAvalonSyntaxMapper = new Dictionary<FileContentType, IHighlightingDefinition>
            {
                { FileContentType.Xml, HighlightingManager.Instance.GetDefinition("XML") },
                { FileContentType.TSBin, HighlightingManager.Instance.GetDefinition("XML") },
                { FileContentType.Lua,  LoadAvalonSyntaxDefinition("TSLua.xshd") }
            };

        public static string[] AllowedTextEditorFileExtensions { get; } = GetFileExtensionsFromFileContentTypes(new[] {
            FileContentType.Txt,
            FileContentType.Xml,
            FileContentType.TSBin,
            FileContentType.Csv,
            FileContentType.Lua,
        });

        public static IHighlightingDefinition? GetAvalonSyntax(FileContentType fileContentType)
        {
            return FileContentTypeToAvalonSyntaxMapper
                .Where(x => x.Key == fileContentType)
                .Select(x => x.Value)
                .FirstOrDefault();
        }

        public static async Task<string> BinToXml(RWLibrary rWLib, string filename)
        {
            var xml = await rWLib.Serializer.DeserializeWithSerzExe(filename);
            using (var ms = new MemoryStream())
            {
                using (XmlTextWriter writer = new XmlTextWriter(ms, new UTF8Encoding(false)) { Formatting = Formatting.Indented })
                {
                    xml.WriteTo(writer);
                }

                return Encoding.UTF8.GetString(ms.ToArray());
            }

        }

        public static Dictionary<FileContentType, FileTransformer> FileContentTypeTransformers { get; } = new Dictionary<FileContentType, FileTransformer>
        {
            { FileContentType.TSBin, new FileTransformer(BinToXml) },
            { FileContentType.Other, new FileTransformer((_, x) => Task.FromResult(File.ReadAllText(x))) }
        };

        public static bool CanOpenInTextEditor(string extension)
        {
            return AllowedTextEditorFileExtensions.Where(e => extension.Equals(e, StringComparison.CurrentCultureIgnoreCase)).Count() > 0;
        }

        public struct OpenFileForTextEditorResult
        {
            public string Text { get; set; }
            public FileContentType FileContentType { get; set; }
            public FileTransformer Transformer { get; set; }
        }

        public static async Task<OpenFileForTextEditorResult> OpenFileForTextEditor(string filename, RWLibrary rWLibrary)
        {
            var ext = Path.GetExtension(filename).TrimStart('.');
            if (CanOpenInTextEditor(ext) == false)
            {
                throw new FileEditorCannotOpenThisFileExtension(filename);
            }
            var fileContentType = DetermineFileContentType(ext);

            var transformer = FileContentTypeTransformers
                .Where(x => x.Key == fileContentType)
                .Select(x => x.Value)
                .FirstOrDefault(FileContentTypeTransformers[FileContentType.Other]);

            var result = await transformer.Transform(filename, rWLibrary);

            return new OpenFileForTextEditorResult
            {
                Text = result,
                Transformer = transformer,
                FileContentType = fileContentType
            };
        }

        public static FileContentType DetermineFileContentType(string extension)
        {
            var fileFormat = DetermineFileFormat(extension);
            if (fileFormat == FileFormat.Other) return FileContentType.Other;
            var fileContentType = FileFormatToFileContentTypeMapper[fileFormat];

            return fileContentType;
        }

        public static FileFormat DetermineFileFormat(string extension)
        {
            var e = extension.ToLower();
            var fileFormat = FileExtensionToFileFormat
                .Where(x => x.Key.Equals(e, StringComparison.CurrentCultureIgnoreCase))
                .Select(x => x.Value).FirstOrDefault(FileFormat.Other);

            return fileFormat;
        }

        public static FileFormat[] GetFileFormatsFromContentTypes(FileContentType[] fileContentTypes)
        {
            var result = new List<FileFormat>();

            foreach (var fileFormatToContentType in FileFormatToFileContentTypeMapper)
            {
                if (fileContentTypes.Contains(fileFormatToContentType.Value))
                {
                    result.Add(fileFormatToContentType.Key);
                }
            }

            return result.ToArray();
        }

        public static string[] GetFileExtensionsFromFileFormats(FileFormat[] fileFormats)
        {
            var result = new List<string>();

            foreach (var fileExtensionToFormat in FileExtensionToFileFormat)
            {
                if (fileFormats.Contains(fileExtensionToFormat.Value))
                {
                    result.Add(fileExtensionToFormat.Key);
                }
            }

            return result.ToArray();
        }

        public static string[] GetFileExtensionsFromFileContentTypes(FileContentType[] fileContentTypes)
        {
            var fileFormats = GetFileFormatsFromContentTypes(fileContentTypes);
            return GetFileExtensionsFromFileFormats(fileFormats);
        }

        public static IHighlightingDefinition LoadAvalonSyntaxDefinition(string resourceName)
        {
            var type = typeof(RailworkerFiles);
            var fullName = "Railworker.Resources.AvalonSyntaxDefinitions." + resourceName;
            using (var stream = type.Assembly.GetManifestResourceStream(fullName))
            {
                if (stream == null) throw new FileNotFoundException("Could not find resource file: " + resourceName);
                using (XmlTextReader reader = new XmlTextReader(stream))
                {
                    return HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }
        }

        public static void MigrateOldSyntaxDefinitionFile(string resourceName)
        {
            var type = typeof(RailworkerFiles);
            var fullName = "Railworker.Resources.AvalonSyntaxDefinitions." + resourceName;
            using (var stream = type.Assembly.GetManifestResourceStream(fullName))
            {
                if (stream == null) throw new FileNotFoundException("Could not find resource file: " + resourceName);
                XshdSyntaxDefinition xshd;
                using (XmlTextReader reader = new XmlTextReader(stream))
                {
                    xshd = HighlightingLoader.LoadXshd(reader);
                }
                using (XmlTextWriter writer = new XmlTextWriter("output.xshd", System.Text.Encoding.UTF8))
                {
                    writer.Formatting = Formatting.Indented;
                    new SaveXshdVisitor(writer).WriteDefinition(xshd);
                }
            }
        }
    }
}
