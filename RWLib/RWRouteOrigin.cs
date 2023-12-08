using System.Xml.Linq;

namespace RWLib
{
    public class RWRouteOrigin : RWXml
    {
        public RWRouteOrigin(XElement xElement, RWLibrary lib) : base(xElement, lib)
        {
        }

        public double Lat
        {
            get => ((double?)xml
                ?.Element("Origin")
                ?.Element("sGeoPosition")
                ?.Element("Lat")) ?? 0.0;
         }

        public double Long
        {
            get => ((double?)xml
                ?.Element("Origin")
                ?.Element("sGeoPosition")
                ?.Element("Long")) ?? 0.0;
        }

        public double Easting
        {
            get => ((double?)xml
                ?.Element("MapOffset")
                ?.Element("sMapCoords")
                ?.Element("Easting")) ?? 0.0;
        }

        public double Northing
        {
            get => ((double?)xml
                ?.Element("MapOffset")
                ?.Element("sMapCoords")
                ?.Element("Northing")) ?? 0.0;
        }

        public string ZoneLetter
        {
            get => ((string?)xml
                ?.Element("ZoneLetter")) ?? "";
        }

        public int ZoneNumber
        {
            get => ((int?)xml
                ?.Element("ZoneNumber")) ?? 0;
        }
    }
}