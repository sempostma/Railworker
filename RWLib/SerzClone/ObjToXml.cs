using RWLib.SerzCloneOld;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static RWLib.SerzClone.Node;

namespace RWLib.SerzClone
{
    public class ObjToXml
    {
        private class AncestorItem
        {
            internal uint counter;
            internal uint children;
            internal required XContainer element;
        }

        private Stack<AncestorItem> ancenstors = new Stack<AncestorItem>();
        private XDocument xDocument;

        public ObjToXml() {
            var decleration = new XDeclaration("1.0", "utf-8", null);
            xDocument = new XDocument(decleration);
            ancenstors.Push(new AncestorItem
            {
                children = 1,
                counter = 0,
                element = xDocument
            });
        }

        public XDocument Finish()
        {
            return xDocument;
        }

        public void Push(NodeUnion nodeUnion)
        {
            var parent = ancenstors.Peek();

            switch (nodeUnion.type)
            {
                case NodeType.FF41:
                    {
                        var node = (FF41Node)nodeUnion.value;
                        break;
                    }
                case NodeType.FF4E:
                    {
                        var node = (FF4ENode)nodeUnion.value;
                        break;
                    }
                case NodeType.FF50:
                    {
                        // node with children
                        var node = (FF50Node)nodeUnion.value;
                        var element = new XElement(node.name);
                        if (ancenstors.Count == 1)
                        {
                            element.Add(new XAttribute(XNamespace.Xmlns + "d", RWUtils.KujuNamspace));
                        }
                        parent.element.Add(element);
                        parent.counter++;
                        ancenstors.Push(new AncestorItem
                        {
                            element = element,
                            counter = 0,
                            children = node.children
                        });
                        break;
                    }
                case NodeType.FF52:
                    {
                        var node = (FF52Node)nodeUnion.value;
                        break;
                    }
                case NodeType.FF56:
                    {
                        // node with no children
                        var node = (FF56Node)nodeUnion.value;
                        var element = new XElement(node.name);
                        element.Add(new XAttribute(RWUtils.KujuNamspace + "type", node.dType.ToAttributeString()));
                        element.Value = node.value.value.ToString()!;
                        parent.element.Add(element);
                        parent.counter++;
                        break;
                    }
                case NodeType.FF70:
                    {
                        ancenstors.Pop();

                        // close node
                        //var node = (FF70Node)nodeUnion.value;
                        //var element = new XElement(node.name);
                        //parent.element.Add(element);
                        //parent.counter++;
                        break;
                    }
            }
        }
    }
}
