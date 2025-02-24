using RWLib.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RWLib
{
    public class RWTgPcDxLoader : RWLibraryDependent
    {
        private RWSerializer serializer;
        private TgpcdxDecoder decoder = new TgpcdxDecoder();

        internal RWTgPcDxLoader(RWLibrary rWLibrary, RWSerializer serializer) : base(rWLibrary)
        {
            this.serializer = serializer;
        }

        public async Task<Image<Rgba32>> LoadTgPcDx(string filename)
        {
            var doc = await serializer.DeserializeWithSerzExe(filename);
            var tgpcdx = new TgPcDxFile(doc, rWLib);

            var image = decoder.Decode(tgpcdx);

            return image;
        }
    }
}
