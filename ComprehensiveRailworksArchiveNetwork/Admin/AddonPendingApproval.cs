using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComprehensiveRailworksArchiveNetwork.Admin
{
    public class AddonPendingApproval
    {
        public Guid AddonGuid { get; set; }
        public AddonVersion AddonVersion { get; set; }
        public Guid AddonVariantGuid { get; set; }
        public string Reason { get; set; }
    }
}
