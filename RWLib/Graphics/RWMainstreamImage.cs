using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RWLib.Interfaces;
using SixLabors.ImageSharp;

namespace RWLib.Graphics
{
    public class RWMainstreamImage : IRWImage
    {
        public Image Image { get; set; }
    }
}
