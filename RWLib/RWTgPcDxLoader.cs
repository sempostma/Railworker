using RWLib.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
