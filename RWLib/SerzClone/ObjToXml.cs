using Microsoft.VisualBasic.FileIO;
using RWLib.SerzCloneOld;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static RWLib.SerzClone.BinToObj;
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

        //public const string doubleFixedPoint = "0.###################################################################################################################################################################################################################################################################################################################################################";
        public const string doubleFixedPoint = "0.######";

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

        public string ValueToString(DataType dataType, ValueType value)
        {
            switch(dataType)
            {
                case DataType._sFloat32:
                    {
                        return ((float)value).ToString("0.0000000", CultureInfo.InvariantCulture);
                    }
                default:
                    {
                        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
                    }
            }
        }

        public void Push(NodeUnion nodeUnion)
        {
            var parent = ancenstors.Peek();

            switch (nodeUnion.type)
            {
                case NodeType.FF41:
                    {
                        // node with a fixed length series of values of a specific type
                        var node = (FF41Node)nodeUnion.value;
                        var element = new XElement(node.name);
                        element.Add(new XAttribute(RWUtils.KujuNamspace + "numElements", node.numElements));
                        element.Add(new XAttribute(RWUtils.KujuNamspace + "elementType", node.dType.ToAttributeString()));
                        if (node.dType == DataType._sFloat32)
                        {
                            element.Add(new XAttribute(RWUtils.KujuNamspace + "precision", "string"));
                        }
                        var sb = new StringBuilder();
                        for (int i = 0; i < node.numElements; i++)
                        {
                            // 8 bytes
                            var isLast = i + 1 == node.numElements;

                            switch(node.dType)
                            {
                                case DataType._sFloat32:
                                    {
                                        var value = ((float)node.values[i].value).ToString("N7", CultureInfo.InvariantCulture);
                                        sb.Append(value);
                                        break;
                                    }
                                default:
                                    {
                                        var value = Convert.ToString(node.values[i].value, CultureInfo.InvariantCulture);
                                        sb.Append(value);
                                        break;
                                    }
                            }

                            if (!isLast)
                            {
                                sb.Append(' ');
                            }
                        }

                        element.Value = sb.ToString();

                        parent.element.Add(element);
                        parent.counter++;

                        break;
                    }
                case NodeType.FF42:
                    {
                        // blob node
                        var node = (FF42Node)nodeUnion.value;
                        var element = new XElement(RWUtils.KujuNamspace + "blob");

                        element.Add(new XAttribute(RWUtils.KujuNamspace + "size", node.size));

                        var result = Convert.ToHexString(node.data);
                        var sb = new StringBuilder();

                        for(int i = 0; i < result.Length; i += 16)
                        {
                            // 8 bytes
                            var isLast = i + 16 >= result.Length;
                            if (isLast)
                            {
                                var count = Math.Min(16, result.Length - i);
                                sb.Append(result, i, count);
                            }
                            else
                            {
                                sb.Append(result, i, 16);
                                if ((i + 16) % 64 == 0) sb.Append("\n");
                                else sb.Append(' ');
                            }
                        }

                        element.Value = sb.ToString();

                        parent.element.Add(element);
                        parent.counter++;

                        break;
                    }
                case NodeType.FF4E:
                    {
                        var node = (FF4ENode)nodeUnion.value;
                        // do nothing, placeholder node
                        var element = new XElement(RWUtils.KujuNamspace + "nil");
                        parent.element.Add(element);
                        parent.counter++;
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
                            element.Add(new XAttribute(RWUtils.KujuNamspace + "version", "1.0"));
                        }
                        if (node.id != 0)
                        {
                            element.Add(new XAttribute(RWUtils.KujuNamspace + "id", node.id));
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
                        // element with uint32 value
                        var node = (FF52Node)nodeUnion.value;
                        var element = new XElement(node.name);
                        element.Add(new XAttribute(RWUtils.KujuNamspace + "type", "ref"));
                        element.Value = node.value.ToString(CultureInfo.InvariantCulture);
                        parent.element.Add(element);
                        parent.counter++;
                        break;
                    }
                case NodeType.FF56:
                    {
                        // node with no children
                        var node = (FF56Node)nodeUnion.value;
                        var element = new XElement(node.name);
                        element.Add(new XAttribute(RWUtils.KujuNamspace + "type", node.dType.ToAttributeString()));
                        if (node.dType == DataType._sFloat32)
                        {
                            var doubleVal = Convert.ToDouble(node.value.value);
                            var altEncoding = Convert.ToHexString(BitConverter.GetBytes(doubleVal)); // use a souble here as the value is encoded as a double in this case
                            element.Add(new XAttribute(RWUtils.KujuNamspace + "alt_encoding", altEncoding));
                            element.Add(new XAttribute(RWUtils.KujuNamspace + "precision", "string"));

                            var str = doubleVal.ToString("g6", CultureInfo.InvariantCulture);
                            if (str.Contains('e'))
                            {
                                var firstPart = str.Split('e')[0];
                                var plusOrMinus = str.Split('e')[1][0];
                                var lastPart = str.Split(plusOrMinus)[1];
                                lastPart = lastPart.PadLeft(3, '0');
                                str = firstPart + 'e' + plusOrMinus + lastPart;
                            }

                            element.Value = str;
                        }
                        else if (node.dType == DataType._bool)
                        {
                            element.Value = ((bool)node.value.value) == true ? "1" : "0";
                        }
                        else
                        {
                            element.Value = Convert.ToString(node.value.value, CultureInfo.InvariantCulture) ?? String.Empty;
                        }
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
                default:
                    throw new InvalidNodeTypeException("Invalid node type during Xml serialization: " + nodeUnion.type.ToString());

            }
        }
    }
}
