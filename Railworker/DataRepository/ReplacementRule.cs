using System.Configuration;

namespace Railworker.DataRepository
{
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class ReplacementRule : ViewModel
    {
        [SettingsSerializeAs(SettingsSerializeAs.Xml)]
        public class ReplacementFilters
        {
            public bool AI { get; set; } = true;
            public bool Player { get; set; } = true;
            // loose consists will still count as AI or Player if they are contained in the instruction set of the scenario for a player or AI
            public bool Static { get; set; } = true;

            // if the consist has an engine often indicates (when its a passenger train) if the train has passengers.
            // so you may want a replacement vehicle for when the consist has no engine, so you can apply your passengerless wagon variant.
            public bool OnlyEnginelessConsists { get; set; } = false;
        }

        public string OldName { get; set; } = "";
        public string OldXmlPath { get; set; } = "";
        public string NewName { get; set; } = "";
        public string NewXmlPath { get; set; } = "";
        public int Priority { get; set; } = 0;
        public bool Reversed { get; set; } = false;
        public ReplacementFilters Filters { get; set; } = new ReplacementFilters();
    }
}
