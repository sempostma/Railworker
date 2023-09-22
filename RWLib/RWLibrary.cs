namespace RWLib
{
    public class RWLibrary
    {
        internal RWLibOptions options;

        public RWLibrary(RWLibOptions options)
        {
            this.options = options;
        }

        public RWSerializer CreateSerializer()
        {
            var serializer = new RWSerializer(this);
            return serializer;
        }

        public RWBlueprintLoader CreateBlueprintLoader(RWSerializer serializer)
        {
            var blueprintLoader = new RWBlueprintLoader(this, serializer);
            return blueprintLoader;
        }

        public RWRouteLoader CreateRouteLoader(RWSerializer serializer)
        {
            return new RWRouteLoader(this, serializer);
        }
    }
}