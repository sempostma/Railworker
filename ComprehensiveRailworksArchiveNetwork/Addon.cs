using System.Runtime.Serialization;

namespace ComprehensiveRailworksArchiveNetwork
{
    public enum AddonType { ElectricLocomotive = 1, SteamLocomotive = 2, DieselLocomotive = 3, FreightWagon = 4, Coach = 5, SpecialVehicle = 6, Repaint = 7, Route = 8, Scenario = 9, Scenery = 10, Other = 11 }

    public enum AddonEra { I = 1, II = 2, III = 3, IV = 4 }

    public class Addon
    {
        public required Guid Guid { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required Author Author { get; set; }
        public required AddonType Type { get; set; }
        public required AddonEra Era { get; set; }
        public required List<Collaborator> Credits { get; set; }
        public required List<AddonVariant> Variants { get; set; }
        /// <summary>
        /// Indicates if the Addon is optional, this is meant for sound updates or script updates for existing addons.
        /// </summary>
        public required bool IsOptional { get; set; }
    }
}