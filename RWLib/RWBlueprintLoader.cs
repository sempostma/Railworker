using RWLib.RWBlueprints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib
{
    public class RWBlueprintLoader : RWLibraryDependent
    {
        private RWSerializer serializer;

        internal RWBlueprintLoader(RWLibrary rWLibrary, RWSerializer serializer) : base(rWLibrary)
        {
            this.serializer = serializer;
        }

        public RWBlueprint FromXDocument(XDocument xDocument)
        {
            XElement blueprint = xDocument.Root!.Element("Blueprint")!.Elements().First()!;

            switch (blueprint.Name.ToString())
            {
                case "cWagonBlueprint":
                    return new RWWagonBlueprint(blueprint);

                case "cEngineBlueprint":
                    return new RWEngineBlueprint(blueprint);

                case "cEngineSimBlueprint":
                    var engineSimulation = blueprint
                        .Element("EngineSimComponent")!
                        .Element("cEngineSimComponentBlueprint")!
                        .Element("SubSystem")!
                        .Elements().First();

                    switch(engineSimulation.Name.ToString()) {
                        case "EngineSimulation-cDieselElectricSubSystemBlueprint":
                            return new RWDieselElectricEngineSimulationBlueprint(blueprint);

                        case "EngineSimulation-cElectricSubSystemBlueprint":
                            return new RWElectricEngineSimulationBlueprint(blueprint);

                        default:
                            throw new NotImplementedException("Engine simulation type is not implemented");
                    }

                default:
                    return new RWUnknownBlueprint(blueprint);
            }
        }

        public async Task<RWBlueprint> FromFilename(string filename)
        {
            XDocument document = await serializer.Deserialize(filename);

            return FromXDocument(document);
        }

        public async Task<RWBlueprint> FromBlueprintID(RWBlueprintID blueprintID)
        {
            string blueprintPath = blueprintID.GetRelativeFilePathFromAssetsFolder();
            blueprintPath = Path.ChangeExtension(blueprintPath, "bin");
            string filename = Path.Combine(rWLib.options.TSPath, "Assets", blueprintPath);

            return await FromFilename(filename);
        }
    }
}
