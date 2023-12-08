using RWLib.SerzClone;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static RWLib.SerzClone.Node;

namespace RWLib.SerzCloneOld
{
    public class BinToObj
    {
        

        private enum StringContext { 
            Name,
            DType,
            Value,
        };
        public class InvalidNodeTypeException : Exception
        {
            public InvalidNodeTypeException(string? message) : base(message)
            {
            }
        }
        public class TooManyChildrenException : Exception { }

        private const string prolog = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";
        private const string binPrelude = "SERZ";

        private BinaryStreamReader StreamReader { get; set; }
        private int line = 0;
        private List<string> stringMap = new List<string>();
        private NodeUnion[] savedTokenList = new NodeUnion[255];
        private int savedTokenListIdx = 0;
        private List<NodeUnion> result = new List<NodeUnion>();


        public BinToObj(Stream stream)
        {
            StreamReader = new BinaryStreamReader(stream);
        }

        public async System.Threading.Tasks.Task<List<NodeUnion>> Run()
        {
            await StreamReader.Start();
            await CheckPrelude();
            var _ = await StreamReader.ReadUint32();

            do
            {
                if (StreamReader.Current == 0xff)
                {
                    await StreamReader.IncrementCurrentIndex();

                    switch(StreamReader.Current)
                    {
                        case 0x41:
                            {
                                await StreamReader.IncrementCurrentIndex();
                                var token = await ProcessFF41();
                                var node = new NodeUnion { type = NodeType.FF41, value = token };
                                result.Add(node);
                                savedTokenList[savedTokenListIdx] = node;
                                break;
                            }
                        case 0x43:
                            {
                                await StreamReader.MoveCurrentIndex(6);
                                if (result.Count > 0)
                                {
                                    if (result[^1].type != NodeType.FF50)
                                    {
                                        throw new InvalidDataException("While parsing a 0x43 node we expected to find a previous 0x55 node to add the 0x43 node to.");
                                    }
                                    var t = result[^1];
                                    var r = (FF50Node)t.value;
                                    r.children = 1;
                                    t.value = r;
                                    result[^1] = t;
                                }
                                break;
                            }
                        case 0x4e: 
                            {
                                await StreamReader.IncrementCurrentIndex();
                                var n = new FF4ENode();
                                var node = new NodeUnion { type = NodeType.FF4E, value = n };
                                result.Add(node);
                                savedTokenList[savedTokenListIdx] = node;
                                break;
                            }
                        case 0x50:
                            {
                                await StreamReader.IncrementCurrentIndex();
                                var token = await ProcessFF50();
                                var node = new NodeUnion { type = NodeType.FF50, value = token };
                                result.Add(node);
                                savedTokenList[savedTokenListIdx] = node;
                                break;
                            }
                        case 0x52:
                            {
                                await StreamReader.IncrementCurrentIndex();
                                var token = await ProcessFF52();
                                var node = new NodeUnion { type = NodeType.FF52, value = token };
                                result.Add(node);
                                savedTokenList[savedTokenListIdx] = node;
                                break;
                            }
                        case 0x56:
                            {
                                await StreamReader.IncrementCurrentIndex();
                                var token = await ProcessFF56();
                                var node = new NodeUnion { type = NodeType.FF56, value = token };
                                result.Add(node);
                                savedTokenList[savedTokenListIdx] = node;
                                break;
                            }
                        case 0x70:
                            {
                                await StreamReader.IncrementCurrentIndex();
                                var token = await ProcessFF70();
                                var node = new NodeUnion { type = NodeType.FF70, value = token };
                                result.Add(node);
                                savedTokenList[savedTokenListIdx] = node;
                                break;
                            }
                        default:
                            throw new InvalidNodeTypeException("Invalid node type: " + StreamReader.Current.ToString("X"));
                    }
                    savedTokenListIdx = (savedTokenListIdx + 1) % 255;
                } else
                {
                    result.Add(await ProcessSavedLine());
                }
                line++;

            } while (StreamReader.IsFinished == false);

            return result;
        }

        private async System.Threading.Tasks.Task CheckPrelude()
        {
            for (int i = 0; i < 4; i++)
            {
                if (StreamReader.Current != binPrelude[StreamReader.CurrentIndex]) throw new RWLib.Exceptions.IncorrectPreludeException("Bad prelude character at the start of .bin file.");
                await StreamReader.IncrementCurrentIndex();
            }
        }

        private async Task<string> Identifier(StringContext stringContext)
        {
            List<byte> retArray = new List<byte>();

            var first = StreamReader.Current;
            await StreamReader.IncrementCurrentIndex();
            var second = StreamReader.Current;
            await StreamReader.IncrementCurrentIndex();

            if (first == 0xFF && second == 0xFF) // New string
            {
                var strLen = await StreamReader.ReadUint32();
                byte[] str = await StreamReader.ReadBytes((int)strLen);

                if (stringContext == StringContext.Name) // Replace '-' with '::' in names only
                {
                    for (int i = 0; i < str.Length; i++)
                    {
                        if (str[i] == ':' && str[i + 1] == ':')
                        {
                            retArray.AddRange(Encoding.ASCII.GetBytes("-"));
                            i += 1;
                        }
                        else
                        {
                            retArray.Add(str[i]);
                        }
                    }
                }
                else
                {
                    retArray.AddRange(str);
                }

                var retStr = Encoding.ASCII.GetString(retArray.ToArray());

                stringMap.Add(retStr);

                return (stringContext == StringContext.Name && retArray.Count == 0) ? "e" : retStr;
            }

            var littleEndian = new byte[2] { first, second };
            UInt16 strIdx = BitConverter.ToUInt16(littleEndian);
            string savedName = stringMap[strIdx];

            return (stringContext == StringContext.Name && savedName.Length == 0) ? "e" : savedName;
        }

        private async Task<CDeltaString> ProcessCDeltaString()
        {
            var result = new List<byte>();
            var str = await Identifier(StringContext.Value);
            return new CDeltaString
            {
                value = str
            };
        }

        private async Task<DataUnion> ProcessData(DataType dataType)
        {
            var value = await ProcessDataValue(dataType);
            return new DataUnion { value = value, type = dataType };
        }

        private async Task<ValueType> ProcessDataValue(DataType dataType)
        {
            switch (dataType)
            {
                case DataType._bool:
                    return await StreamReader.ReadBool();
                case DataType._sUInt8:
                    return await StreamReader.ReadUint8();
                case DataType._sInt16:
                    return await StreamReader.ReadInt16();
                case DataType._sInt32:
                    return await StreamReader.ReadInt32();
                case DataType._sUInt32:
                    return await StreamReader.ReadUint32();
                case DataType._sUInt64:
                    return await StreamReader.ReadUint64();
                case DataType._sFloat32:
                    return await StreamReader.ReadFloat();
                case DataType._cDeltaString:
                    return await ProcessCDeltaString();
                default:
                    throw new ArgumentException("Unknown dataType: " + dataType.ToString());
            }
        }

        private async Task<NodeUnion> ProcessSavedLine()
        {
            if (StreamReader.Current > savedTokenList.Length)
            {
                throw new InvalidDataException("Invalid .bin file. While validating the saved token list counter, we encountered an incorrect value.");
            }

            var savedLine = savedTokenList[StreamReader.Current];
            await StreamReader.IncrementCurrentIndex();

            switch(savedLine.type)
            {
                case NodeType.FF56:
                    {
                        var n = (FF56Node)savedLine.value;
                        var data = await ProcessData(n.dType);
                        var node = new FF56Node
                        {
                            name = n.name,
                            dType = n.dType,
                            value = data
                        };
                        return new NodeUnion
                        {
                            type = NodeType.FF56,
                            value = node
                        };
                    }
                case NodeType.FF41:
                    { 
                        var n = (FF41Node)savedLine.value;
                        var dataUnionList = new List<DataUnion>();
                        var numElements = StreamReader.Current;

                        var values = new List<DataUnion>();
                        int i = 0;
                        while(i < n.numElements)
                        {
                            values.Add(await ProcessData(n.dType));
                            i++;
                        }

                        var node = new FF41Node
                        {
                            name = n.name,
                            dType = n.dType,
                            values = values.ToArray(),
                            numElements = numElements
                        };

                        return new NodeUnion
                        {
                            type = NodeType.FF41,
                            value = node
                        };
                    }

                case NodeType.FF50:
                    {
                        var n = (FF50Node)savedLine.value;
                        var id = await StreamReader.ReadUint32();
                        var children = await StreamReader.ReadUint32();

                        if (children > 100)
                        {
                            throw new TooManyChildrenException();
                        }

                        var node = new FF50Node
                        {
                            name = n.name,
                            id = id,
                            children = children
                        };

                        return new NodeUnion
                        {
                            type = NodeType.FF50,
                            value = node
                        };
                    }
                case NodeType.FF52:
                    {
                        var value = await StreamReader.ReadUint32();
                        var n = (FF52Node)savedLine.value;

                        var node = new FF52Node
                        {
                            name = n.name,
                            value = value
                        };

                        return new NodeUnion { type = NodeType.FF52, value = node };
                    }
                case NodeType.FF70:
                    {
                        var n = (FF70Node)savedLine.value;

                        var node = new FF70Node
                        {
                            name = n.name
                        };

                        return new NodeUnion
                        {
                            type = NodeType.FF70,
                            value = node
                        };
                    }
                case NodeType.FF4E:
                    {
                        var node = new FF4ENode { };

                        return new NodeUnion
                        {
                            type = NodeType.FF4E,
                            value = node
                        };
                    }
                default:
                    throw new InvalidDataException("Invalid NodeType: " + savedLine.type.ToString());
            }
        }

        private async Task<FF41Node> ProcessFF41()
        {
            var nodeName = await Identifier(StringContext.Name);
            var dataType = await Identifier(StringContext.DType);
            var elemType = Enum.Parse<DataType>('_' + dataType);
            var numElements = StreamReader.Current;
            await StreamReader.IncrementCurrentIndex();

            var elements = new DataUnion[numElements];
            for (int i = 0; i < numElements; i++)
            {
                elements[i] = await ProcessData(elemType);
            }

            return new FF41Node
            {
                dType = elemType,
                name = nodeName,
                numElements = numElements,
                values = elements
            };
        }

        private async Task<FF50Node> ProcessFF50()
        {
            var nodeName = await Identifier(StringContext.Name);
            var id = await StreamReader.ReadUint32();
            var children = await StreamReader.ReadUint32();

            return new FF50Node
            {
                name = nodeName,
                id = id,
                children = children
            };
        }

        private async Task<FF52Node> ProcessFF52()
        {
            var nodeName = await Identifier(StringContext.Name);
            var value = await StreamReader.ReadUint32();

            return new FF52Node
            {
                name = nodeName,
                value = value
            };
        }

        private async Task<FF56Node> ProcessFF56()
        {
            var nodeName = await Identifier(StringContext.Name);
            var dataTypeStr = await Identifier(StringContext.DType);
            var dataType = Enum.Parse<DataType>('_' + dataTypeStr);
            var data = await ProcessData(dataType);

            return new FF56Node
            {
                name = nodeName,
                dType = dataType,
                value = data
            };
        }

        private async Task<FF70Node> ProcessFF70()
        {
            var nodeName = await Identifier(StringContext.Name);

            return new FF70Node
            {
                name = nodeName
            };
        }
    } 
}
