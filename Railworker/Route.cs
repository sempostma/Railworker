using Railworker.Properties;
using RWLib;
using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Linq;
using Railworker.Core;

namespace Railworker
{
    public class Route : ViewModel
    {
        public bool IsFavorite { get; set; }
        public string Name { get; set; } = "";
        public string Guid { get; set; } = "";
        public RWRoute? RWRoute { get; private set; }

        public static async IAsyncEnumerable<Route> FromRWRoutes(IAsyncEnumerable<RWRoute> rwRoutes)
        {
            var favoriteGuids = new string[Settings.Default.FavoriteRoutes.Count];
            Settings.Default.FavoriteRoutes.CopyTo(favoriteGuids, 0);

            await foreach (var rwRoute in rwRoutes)
            {
                yield return new Route()
                {
                    Name = rwRoute.DisplayName == null ? Language.Resources.unknown_name : Utilities.DetermineDisplayName(rwRoute.DisplayName),
                    Guid = rwRoute.guid,
                    IsFavorite = favoriteGuids.Contains(rwRoute.guid),
                    RWRoute = rwRoute
                };
            }
        }
    }
}