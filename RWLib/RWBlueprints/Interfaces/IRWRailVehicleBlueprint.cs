﻿using RWLib.RWBlueprints.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib.RWBlueprints.Interfaces
{
    public interface IRWRailVehicleBlueprint
    {
        public XElement Xml { get; }
        public IRailVehicleComponent RailVehicleComponent { get; }
        public String Name { get; }
        public RWDisplayName DisplayName { get; }
        public RWBlueprintID BlueprintId { get; }
    }
}
