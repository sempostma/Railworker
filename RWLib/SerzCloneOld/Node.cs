using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWLib.SerzCloneOld
{
    public class Node
    {
        public enum DataType
        {
            _bool,
            _sUInt8,
            _sInt16,
            _sInt32,
            _sUInt32,
            _sUInt64,
            _sFloat32,
            _cDeltaString
        }

        public enum NodeType
        {
            FF41,
            FF4E,
            FF50,
            FF52,
            FF56,
            FF70
        }

        public struct CDeltaString
        {
            public string value;
        }

        public struct DataUnion
        {
            public DataType type;
            public ValueType value;
        }

        public struct NodeUnion
        {
            public NodeType type;
            public ValueType value;
        }

        public struct FF41Node
        {
            public string name;
            public byte numElements;
            public DataType dType;
            public DataUnion[] values;
        }

        public struct FF4ENode
        {
            // Placeholder for ff4eNode
        }

        public struct FF50Node
        {
            public string name;
            public uint id;
            public uint children;
        }

        public struct FF52Node
        {
            public string name;
            public uint value;
        }

        public struct FF56Node
        {
            public string name;
            public DataType dType;
            public DataUnion value;
        }

        public struct FF70Node
        {
            public string name;
        }

        public static object ParseWithDataType(DataType dataType, string value)
        {
            switch (dataType)
            {
                case DataType._bool:
                    return bool.Parse(value);
                case DataType._sUInt8:
                    return byte.Parse(value);
                case DataType._sInt16:
                    return short.Parse(value);
                case DataType._sInt32:
                    return int.Parse(value);
                case DataType._sUInt32:
                    return uint.Parse(value);
                case DataType._sUInt64:
                    return ulong.Parse(value);
                case DataType._sFloat32:
                    return float.Parse(value);
                case DataType._cDeltaString:
                    return value;
                default:
                    throw new ArgumentException("Unknown dataType: " + dataType.ToString());
            }
        }
    }
}
