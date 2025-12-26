using RWLib.Abstract;
using BCnEncoder.Decoder;
using SixLabors.ImageSharp.PixelFormats;
using static System.Net.Mime.MediaTypeNames;
using BCnEncoder.ImageSharp;

namespace RWLib
{
    public class RWImageDecoder : RWExe
    {
        public RWImageDecoder(RWLibrary rWLib) : base(rWLib)
        {
        }

        public async Task<SixLabors.ImageSharp.Image> FromFilename(string filename)
        {
            if (filename.ToLower().EndsWith(".tgpcdx"))
            {
                return await rWLib.TgPcDxLoader.LoadTgPcDx(filename);
            } else if (filename.ToLower().EndsWith(".dds")) {
                var ddsDecoder = new BcDecoder();
                using (var stream = File.OpenRead(filename))
                {
                    return await ddsDecoder.DecodeToImageRgba32Async(stream);
                }
            }
            else if (filename.ToLower().EndsWith(".ace")) {
                await RunProcess(rWLib.options.ConvertToTGPath, filename);
                var newFilename = Path.ChangeExtension(filename, ".TgPcDx");
                return await FromFilename(newFilename);
            } else
            {
                return await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(filename);
            }
        }

        public async Task ConvertToTgPcDx(string filename, string destinationFilename)
        {
            await RunProcess(rWLib.options.RWAceToolPath, filename);
            var aceFilename = Path.ChangeExtension(filename, ".ace");
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFilename));
            File.Create(destinationFilename).Close();
            await RunProcess(rWLib.options.ConvertToTGPath,
                new string[] {
                    "-i \"" + aceFilename + "\"",
                    "-o \"" + destinationFilename + "\"",
                    "-target pc",
                    //"-nowindow",
                    //"-costXML \"\\" + destinationFilename + ".cost\"",
                    //"-r \"\\" + Path.Combine(rWLib.TSPath, "Source") + "\"",
                    //"-interpalpha",
                    "-forcecompress"
                }
            );
        }
    }
}

