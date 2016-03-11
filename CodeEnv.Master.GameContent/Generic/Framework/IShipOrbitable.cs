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

    using UnityEngine;

    /// <summary>
    /// Interface for Items that can be orbited by ships.
    /// </summary>
    public interface IShipOrbitable : INavigableTarget {

        ShipOrbitSlot ShipOrbitSlot { get; }

        Player Owner { get; }

        Transform transform { get; }

        bool IsOrbitAllowedBy(Player player);

    }
}

