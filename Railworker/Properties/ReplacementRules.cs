using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Railworker.DataRepository;

namespace Railworker.Properties
{
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public partial class ReplacementRules
    {
        public ObservableCollection<ReplacementRule> List { get; } = new ObservableCollection<ReplacementRule>();
    }
}
