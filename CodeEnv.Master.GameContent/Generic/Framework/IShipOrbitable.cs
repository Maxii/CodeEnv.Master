// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipOrbitable.cs
// Interface for Items that can be orbited by ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Interface for Items that can be orbited by ships.
    /// </summary>
    public interface IShipOrbitable : INavigableTarget {

        ShipOrbitSlot ShipOrbitSlot { get; }

        /// <summary>
        /// A collection of assembly stations that are local to the item.
        /// </summary>
        IList<StationaryLocation> LocalAssemblyStations { get; }

        Player Owner { get; }

        Transform transform { get; }

        bool IsOrbitingAllowedBy(Player player);

    }
}

