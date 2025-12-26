using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Railworker
{
    public class Blueprint
    {
        public enum BlueprintExistance
        {
            Found,
            Missing,
            PartiallyReplaced,
            FullyReplaced,
            Replaced
        }
    }
}
