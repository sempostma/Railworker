using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib
{
    public class RWBlueprintEditor2
    {
        private RWBlueprint blueprint;

        public RWBlueprintEditor2(RWBlueprint blueprint)
        {
            this.blueprint = blueprint;       
        }

        public void Parse()
        {
            var xml = blueprint.Xml;

            var root = xml;

            var decleration = new XDeclaration("1.0", "utf-8", null);
            var blueprintEditor2CompatibleXml = new XDocument(decleration);

            var result = ParseItem(root);

            Console.WriteLine(result.ToString());
        }

        private XElement ParseItem(XElement item)
        {
            var newRoot = new XElement(item.Name);

            var element = new XElement("Element");
            newRoot.Add(element);

            var value = new XElement("Value");
            element.Add(value);

            foreach (var child in item.Elements())
            {
                var type = child.Attribute(RWUtils.KujuNamspace + "type");
                switch(type?.Value)
                {
                    case "cDeltaString":
                        {
                            var attribute = new XElement("Attribute");
                            attribute.SetAttributeValue("name", child.Name);
                            value.Add(attribute);

                            var attributeChild = new XElement(type.Value);
                            attribute.Add(attributeChild);

                            var childElement = new XElement("Element");
                            attributeChild.Add(childElement);

                            var valueELement = new XElement("Value");
                            attributeChild.Add(valueELement);

                            valueELement.Value = child.Value;
                            break;
                        }
                    case null:
                        {
                            var attribute = new XElement("Not Implemented (null)");
                            attribute.SetAttributeValue("name", child.Name);
                            value.Add(attribute);
                            break;
                        }
                        
                    default:
                        {
                            var attribute = new XElement("Not Implemented");
                            attribute.SetAttributeValue("name", child.Name);
                            value.Add(attribute);
                            break;
                        }
                }

            }

            return newRoot;
        }
    }
}
