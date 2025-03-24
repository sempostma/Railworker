using RWLib.SerzClone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib.Graphics
{
    public class TgPcDxFile : RWXml
    {
        public class ImageDx : RWXml
        {
            public enum DxFormat
            {
                HC_IMAGE_FORMAT_COMPRESSED_EXPL_ALPHA,
                HC_IMAGE_FORMAT_COMPRESSED
            }

            public DxFormat Format => (DxFormat)Enum.Parse(typeof(DxFormat), Xml.Descendants("Format").First().Value);
            public bool IsSwizzled => Xml.Descendants("IsSwizzled").First().Value == "1";
            public int Width => int.Parse(Xml.Descendants("Width").First().Value);
            public int Height => int.Parse(Xml.Descendants("Height").First().Value);
            public string Blob => Xml.Descendants(RWUtils.KujuNamspace + "blob").First().Value;
            public long BlobSize => (long)Xml.Descendants(RWUtils.KujuNamspace + "blob").First().Attribute(RWUtils.KujuNamspace + "size")!;

            public ImageDx(XElement xml, RWLibrary lib) : base(xml, lib)
            {
            }
        }

        public int Width => int.Parse(Xml.Descendants("Width").First().Value);
        public int Height => int.Parse(Xml.Descendants("Height").First().Value);
        public string Name => Xml.Descendants("Name").First().Value;
        public IEnumerable<ImageDx> Mip => Xml.Descendants("Mip").First().Elements().Select(x => new ImageDx(x, lib));

        public TgPcDxFile(XDocument document, RWLibrary lib) : base(document.Root!, lib)
        {

        }
    }
}
