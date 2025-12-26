using Railworker.Core;
using Railworker.Properties;
using RWLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Railworker
{
    public class Scenario : ViewModel
    {
        public required RWScenario RWScenario { get; set; }

        public string Guid { get; set; } = "";
        public string Name { get; set; } = "";
        public string RouteGuid { get; set; } = "";

        public static async IAsyncEnumerable<Scenario> FromRWScenarios(IAsyncEnumerable<RWScenario> rwScenarios)
        {
            await foreach (var rwScenario in rwScenarios)
            {
                yield return new Scenario()
                {
                    Name = rwScenario.DisplayName == null ? Language.Resources.unknown_name : Utilities.DetermineDisplayName(rwScenario.DisplayName),
                    Guid = rwScenario.guid,
                    RWScenario = rwScenario,
                    RouteGuid = rwScenario.routeGuid
                };
            }
        }
    }
}