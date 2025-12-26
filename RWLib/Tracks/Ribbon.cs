using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWLib.Tracks
{
    public class Ribbon
    {
        public string ID { get; set; }
        public double Length { get; set; }
        public bool IsSuperElevated { get; set; }
        public bool LockCounterWhenModified { get; set; }
        public List<HeightPoint> HeightPoints { get; set; } = new List<HeightPoint>();
    }
}
