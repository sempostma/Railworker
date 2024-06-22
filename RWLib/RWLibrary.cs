using RWLib.Exceptions;
using RWLib.Packaging;
using System.IO.Compression;
using System.Text;

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
        public RWVariantGenerator RWVariantGenerator { get; }

        public RWLibrary(RWLibOptions? options = null)
        {
            this.options = options ?? new RWLibOptions();

            Serializer = CreateSerializer(this.options.UseCustomSerz);
            BlueprintLoader = CreateBlueprintLoader(Serializer);
            RouteLoader = CreateRouteLoader(Serializer);
            TracksBinParser = CreateTracksBinParser(RouteLoader);
            RailDriver = CreatRailDriver();
            RWVariantGenerator = CreateVariantGenerator(Serializer);
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

        public void WriteZipFile(RWPackageInfo info, string destinationFile)
        {
            using (var destination = File.OpenWrite(destinationFile))
            {
                WriteZipFile(info, destination);
            }
        }

        public void WriteRWPFile(RWPackageInfo info, string destinationFile)
        {
            using (var destination = File.OpenWrite(destinationFile))
            {
                WriteRWPFile(info, destination);
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

        public void WriteRWPFile(RWPackageInfo info, Stream destination)
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
                    foreach (var file in info.FileNames)
                    {
                        archive.CreateEntryFromFile(Path.Combine(TSPath, file), file);
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
    }
}