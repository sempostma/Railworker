using System.Xml.Linq;

namespace RWLib
{
    public class RWScenario
    {
        public XDocument scenarioProperties;
        public string guid;
        public string routeGuid;

        public RWScenario(XDocument scenarioProperties, string guid, string routeGuid)
        {
            this.scenarioProperties = scenarioProperties;
            this.guid = guid;
            this.routeGuid = routeGuid;
        }
    }
}