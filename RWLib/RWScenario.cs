using RWLib.RWBlueprints.Components;
using System.Xml.Linq;

namespace RWLib
{
    public class RWScenario : RWXml
    {
        public XDocument scenarioProperties;
        public string guid;
        public string routeGuid;

        public RWScenario(XDocument scenarioProperties, string guid, string routeGuid, RWLibrary lib) : base(scenarioProperties.Root!, lib)
        {
            this.scenarioProperties = scenarioProperties;
            this.guid = guid;
            this.routeGuid = routeGuid;
        }

        public RWDisplayName? DisplayName
        {
            get
            {
                var element = Xml.Descendants("DisplayName").FirstOrDefault();
                if (element == null) return null;
                else return new RWDisplayName(element!);
            }
        }
    }
}