using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCnEncoder.Decoder;
using BCnEncoder.Shared.ImageFiles;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using BCnEncoder.ImageSharp;
using RWLib.SerzClone;
using System.Reflection.PortableExecutable;
using BCnEncoder.Shared;
using static RWLib.Graphics.TgPcDxFile.ImageDx;

namespace RWLib.Graphics
{
    public class TgpcdxDecoder
    {

        private byte[] ConvertHexStrToBytes(string hex)
        {
            string blobData = hex.Replace(" ", "").Replace("\n", "").Replace("\r", "");

            if (blobData.Length % 2 != 0)
                throw new ArgumentException("Hex string must have an even length.");

            byte[] bytes = new byte[blobData.Length / 2];

            for (int i = 0; i < blobData.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(blobData.Substring(i, 2), 16);
            }

            return bytes;
        }

        public Image<Rgba32> Decode(TgPcDxFile tgPcDxFile)
        {
            var mainMip = tgPcDxFile.Mip.First();
            var hexStr = mainMip.Blob;

            var byteArray = ConvertHexStrToBytes(hexStr);

            if (mainMip.BlobSize != byteArray.Length) 
                throw new InvalidDataException($"Invalid number of bytes, expected {mainMip.BlobSize} but received {byteArray.Length}");

            var ddsDecoder = new BcDecoder();

            var compressionFormat = TgPcDxFormatToCompressionFormat(mainMip.Format);

            var image = ddsDecoder.DecodeRawToImageRgba32(byteArray, tgPcDxFile.Width, tgPcDxFile.Height, compressionFormat);
            return image;
        }

        public CompressionFormat TgPcDxFormatToCompressionFormat(TgPcDxFile.ImageDx.DxFormat format)
        {
            switch (format)
            {
                case DxFormat.HC_IMAGE_FORMAT_COMPRESSED_EXPL_ALPHA:
                    return CompressionFormat.Bc2;

                case DxFormat.HC_IMAGE_FORMAT_COMPRESSED_INTERP_ALPHA:
                    return CompressionFormat.AtcInterpolatedAlpha;

                case DxFormat.HC_IMAGE_FORMAT_COLA8888:
                    return CompressionFormat.Rgba;

                case DxFormat.HC_IMAGE_FORMAT_COL888:
                    return CompressionFormat.Rgb;

                case DxFormat.HC_IMAGE_FORMAT_COMPRESSED:
                    return CompressionFormat.Bc1;

                default: 
                    return CompressionFormat.Unknown;

            }
        }
    }
}
