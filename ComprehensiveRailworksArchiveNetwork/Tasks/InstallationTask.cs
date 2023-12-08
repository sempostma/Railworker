using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ComprehensiveRailworksArchiveNetwork.Tasks
{
    [XmlInclude(typeof(CopyDirectory))]
    [XmlInclude(typeof(CopyFile))]
    [XmlInclude(typeof(ExecuteBat))]
    [XmlInclude(typeof(MoveDirectory))]
    [XmlInclude(typeof(MoveFile))]
    public abstract class InstallationTask
    {

    }
}