// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipOrbitable.cs
// Interface for objects that can be orbited by ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for objects that can be orbited by ships.
    /// </summary>
    public interface IShipOrbitable {

        ShipOrbitSlot ShipOrbitSlot { get; }

        /// <summary>
        /// The radius of the KeepoutZone surrounding this IShipOrbitable object.
        /// The ShipOrbitSlot.InnerRadius has the same value.
        /// </summary>
        float KeepoutRadius { get; }

        string FullName { get; }

        Vector3 Position { get; }

        Transform Transform { get; }

        bool IsMobile { get; }

    }
}

