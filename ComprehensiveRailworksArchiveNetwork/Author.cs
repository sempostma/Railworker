using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ComprehensiveRailworksArchiveNetwork
{
    public class Author
    {
        public enum TrustLevelType { 
            Blocked, // can do nothing
            Unverified, // can be listed as co-author but cant upload anything until verified
            Verified, // can upload own packages 
            Trusted, // can upload exes and bat scripts without approval 
            Admin // can make admin descisions and can give change other users's trust levels
        }
        public required Guid Guid { get; set; }
        public required List<AuthorLinks> Links { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Description { get; set; }
        public required TrustLevelType TrustLevel { get; set; }
    }
}
