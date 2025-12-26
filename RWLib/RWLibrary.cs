using RWLib.Exceptions;
using RWLib.Graphics;
using RWLib.Packaging;
using RWLib.RWBlueprints.Components;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace RWLib
{
    public class RWLibrary
    {
        internal RWLibOptions options;

        public string TSPath => options.TSPath;
        public string SerzExePath => options.SerzExePath;

        public RWSerializer Serializer { get; }
        public RWBlueprintLoader BlueprintLoader { get; }
        public RWRouteLoader RouteLoader { get; }
        public RWTracksBinParser TracksBinParser { get; }
        public RWRailDriver RailDriver { get; }
        public RWVariantGenerator VariantGenerator { get; }
        public RWTgPcDxLoader TgPcDxLoader { get; }
        public RWImageDecoder ImageDecoder { get; }

        public RWLibrary(RWLibOptions? options = null)
        {
            this.options = options ?? new RWLibOptions();

            Serializer = CreateSerializer(this.options.UseCustomSerz);
            BlueprintLoader = CreateBlueprintLoader(Serializer);
            RouteLoader = CreateRouteLoader(Serializer);
            TracksBinParser = CreateTracksBinParser(RouteLoader);
            RailDriver = CreatRailDriver();
            VariantGenerator = CreateVariantGenerator(Serializer);
            TgPcDxLoader = CreateTgPcDxLoader(Serializer);
            ImageDecoder = CreateImageDecoder();
        }

        private RWSerializer CreateSerializer(bool useCustomSerz = false)
        {
            if (useCustomSerz) return new RWSerializer(this);
            else return new RWSerializer(this);
        }

        private RWBlueprintLoader CreateBlueprintLoader(RWSerializer serializer)
        {
            var blueprintLoader = new RWBlueprintLoader(this, serializer);
            return blueprintLoader;
        }

        private RWRouteLoader CreateRouteLoader(RWSerializer serializer)
        {
            return new RWRouteLoader(this, serializer);
        }

        private RWTgPcDxLoader CreateTgPcDxLoader(RWSerializer serializer)
        {
            return new RWTgPcDxLoader(this, serializer);
        }

        private RWTracksBinParser CreateTracksBinParser(RWRouteLoader rwRouteLoader)
        {
            return new RWTracksBinParser(this, rwRouteLoader);
        }

        private RWVariantGenerator CreateVariantGenerator(RWSerializer rwSerializer)
        {
            return new RWVariantGenerator(this, rwSerializer);
        }

        private RWRailDriver CreatRailDriver()
        {
            return new RWRailDriver(this);
        }

        private RWImageDecoder CreateImageDecoder()
        {
            return new RWImageDecoder(this);
        }

        public void WriteZipFile(RWPackageInfo info, string destinationFile)
        {
            using (var destination = File.OpenWrite(destinationFile))
            {
                WriteZipFile(info, destination);
            }
        }

        public void WriteRWPFile(RWPackageInfo info, string destinationFile, WriteRWPOptions? options = null)
        {
            using (var destination = File.OpenWrite(destinationFile))
            {
                WriteRWPFile(info, destination, options);
            } 
        }

        public async Task WritePackageInfoFile(RWPackageInfoWithMD5 info, string destinationFile)
        {
            using (var destination = File.OpenWrite(destinationFile))
            {
                await WritePackageInfoFile(info, destination);
            }
        }

        public async Task WritePackageInfoFile(RWPackageInfoWithMD5 info, Stream destination)
        {
            using (StreamWriter writer = new StreamWriter(destination))
            {
                await writer.WriteLineAsync(info.FileNames.Length.ToString());
                await writer.WriteLineAsync(info.Author);
                if (info.License == RWPackageInfo.LicenseType.Unlicensed)
                {
                    throw new InvalidDataException("Unable to write license type because the license can not be \"Unlicensed\". It should either be Protected or Unprotected.");
                }
                await writer.WriteLineAsync(info.License == RWPackageInfo.LicenseType.Protected ? "eProtected" : "eUnprotected");
                for (int i = 0; i < info.FileNames.Length; i++)
                {
                    await writer.WriteLineAsync(info.FileNames[i]);
                }
                await writer.WriteLineAsync(info.Md5.ToString());
            }
        }

        public async Task<RWPackageInfo> ReadPackageInfoFile(string filename)
        {
            using (StreamReader reader = File.OpenText(filename))
            {
                var line1 = await reader.ReadLineAsync();
                if (String.IsNullOrEmpty(line1)) throw new InvalidDataException("Invalid PI file: " + filename + ". Cant read number of entries.");
                var filesTotal = int.Parse(line1);
                var author = await reader.ReadLineAsync();
                if (String.IsNullOrEmpty(author)) throw new InvalidDataException("Invalid PI file: " + filename + ". Cant read author.");
                var licenseStr = await reader.ReadLineAsync();
                if (String.IsNullOrEmpty(author)) throw new InvalidDataException("Invalid PI file: " + filename + ". Cant read license.");

                var list = new string[filesTotal];

                RWPackageInfo.LicenseType license;
                if (licenseStr == "eProtected")
                {
                    license = RWPackageInfo.LicenseType.Protected;
                } else if (licenseStr == "eUnprotected")
                {
                    license = RWPackageInfo.LicenseType.Unprotected;
                } else
                {
                    throw new InvalidDataException("Invalid PI file: " + filename + ". Invalid license type: " + licenseStr);
                }

                for (int i = 0; i < filesTotal; i++)
                {
                    var line = await reader.ReadLineAsync();
                    if (String.IsNullOrEmpty(line)) throw new InvalidDataException("Invalid PI file: " + filename + ". Cant read asset line number: " + i);
                    list[i] = line;
                }

                var hexCode = reader.ReadLine();
                if (String.IsNullOrEmpty(hexCode)) throw new InvalidDataException("Invalid PI file: " + filename + ". Cant read guid.");

                if (String.IsNullOrWhiteSpace(await reader.ReadToEndAsync()) == false) {
                    throw new InvalidDataException("Expected EOL. Extra text was unexpected: " + filename);
                }

                return new RWPackageInfoWithMD5
                {
                    Author = author,
                    Md5 = hexCode,
                    FileNames = list,
                    License = license,
                    Name = filename.Replace('_', ' ').Replace(".rwp", "")
                };
            }
        }

        public void WriteZipFile(RWPackageInfo info, Stream destination)
        {
            using (var archive = new ZipArchive(destination, ZipArchiveMode.Create))
            {
                foreach (var file in info.FileNames)
                {
                    archive.CreateEntryFromFile(Path.Combine(TSPath, file), file);
                }
            }
        }

        public void WriteRWPFile(RWPackageInfo info, Stream destination, WriteRWPOptions? options = null)
        {
            using (var binaryWriter = new BinaryWriter(destination))
            {
                binaryWriter.Write(info.Author);

                if (info.License == RWPackageInfo.LicenseType.Unprotected)
                {
                    binaryWriter.Write((byte)1);
                }
                else if (info.License == RWPackageInfo.LicenseType.Protected)
                {
                    binaryWriter.Write((byte)2);
                }
                else
                {
                    binaryWriter.Write((byte)0);
                }

                using (var archive = new ZipArchive(destination, ZipArchiveMode.Create))
                {
                    if (options?.PackageAsAPAsset == true)
                    {
                        var filenamesByProduct = info.FileNames.GroupBy(x => String.Join(Path.DirectorySeparatorChar, x.Split(Path.DirectorySeparatorChar).Skip(1).Take(2)));
                        foreach (var group in filenamesByProduct)
                        {
                            var productName = String.Join("", group.Key.Split(Path.DirectorySeparatorChar).Skip(1));
                            var apFile = archive.CreateEntry(group.Key + Path.DirectorySeparatorChar + productName + "Assets.ap");
                            using (var apEntry = apFile.Open())
                            {
                                using (var ap = new ZipArchive(apEntry, ZipArchiveMode.Create))
                                {
                                    foreach (var file in group)
                                    {
                                        var entryName = String.Join(Path.DirectorySeparatorChar, file.Split(Path.DirectorySeparatorChar).Skip(3));
                                        ap.CreateEntryFromFile(Path.Combine(TSPath, file), entryName);
                                    }
                                }
                            }
                        }
                    } else
                    {
                        foreach (var file in info.FileNames)
                        {
                            archive.CreateEntryFromFile(Path.Combine(TSPath, file), file);
                        }
                    }
                }
            }
        }

        public RWPackage ReadRWPFile(string fileName)
        {
            using (var stream = File.OpenRead(fileName)) { 
                return ReadRWPFile(fileName, stream);
            }
        }

        public RWPackage ReadRWPFile(string fileName, Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                var authorStrLen = reader.ReadByte();
                var authorBytes = reader.ReadBytes(authorStrLen);
                var authorStr = Encoding.ASCII.GetString(authorBytes);
                var licenseTypeInt = reader.ReadByte();

                RWPackageInfo.LicenseType license = RWPackageInfo.LicenseType.Unlicensed;
                if (licenseTypeInt == 0)
                {
                    license = RWPackageInfo.LicenseType.Unlicensed;
                }
                else if (licenseTypeInt == 1)
                {
                    license = RWPackageInfo.LicenseType.Unprotected;
                }
                else if (licenseTypeInt == 2) 
                { 
                    license = RWPackageInfo.LicenseType.Protected;
                } else
                {
                    throw new InvalidPackageLicenseException("Invalid license type: " + licenseTypeInt);
                }

                var file = new ZipArchive(stream, ZipArchiveMode.Read);
                var fileNames = file.Entries.Select(x => x.FullName).ToArray();
                var md5 = "dummy md5";

                var packageInfo = new RWPackageInfoWithMD5
                {
                    Name = fileName.Replace('_', ' ').Replace(".rwp", ""),
                    License = license,
                    Author = authorStr,
                    FileNames = fileNames,
                    Md5 = md5
                };

                return new RWPackage
                {
                    PackageInfo = packageInfo,
                    Archive = file
                };
            }
        }

        public XDocument CreateDCSV(string csvFileName)
        {
            // Create the root element with namespace
            XElement root = new XElement("cCSVArray",
                new XAttribute(XNamespace.Xmlns + "d", RWUtils.KujuNamspace),
                new XAttribute(RWUtils.KujuNamspace + "version", "1.0"),
                new XAttribute(RWUtils.KujuNamspace + "id", "1"));

            // Create the CSVItem container
            XElement csvItemContainer = new XElement("CSVItem");

            string[] lines = File.ReadAllLines(csvFileName);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] values = line.Split(',');

                XElement csvItem = new XElement("cCSVItem",
                    new XAttribute(RWUtils.KujuNamspace + "id", (i + 2).ToString()), // Start IDs from 2
                    new XElement("X",
                        new XAttribute(RWUtils.KujuNamspace + "type", "sFloat32"),
                        values[0].Trim()),
                    new XElement("Y",
                        new XAttribute(RWUtils.KujuNamspace + "type", "sFloat32"),
                        values[1].Trim()),
                    new XElement("Name",
                        new XAttribute(RWUtils.KujuNamspace + "type", "cDeltaString"),
                        values[2].Trim())
                );

                csvItemContainer.Add(csvItem);
            }

            root.Add(csvItemContainer);

            XDocument document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                root
            );

            return document;
        }
    }
}