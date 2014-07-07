﻿// --------------------------------------------------------------------------------------------------------------------
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

        string FullName { get; }

        Vector3 Position { get; }

        Transform Transform { get; }

        bool IsMobile { get; }

    }
}

