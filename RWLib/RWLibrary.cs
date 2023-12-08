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

        public RWLibrary(RWLibOptions? options = null)
        {
            this.options = options ?? new RWLibOptions();

            Serializer = CreateSerializer(this.options.UseCustomSerz);
            BlueprintLoader = CreateBlueprintLoader(Serializer);
            RouteLoader = CreateRouteLoader(Serializer);
            TracksBinParser = CreateTracksBinParser(RouteLoader);
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
    }
}