﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipDisplayManager.cs
// DisplayManager for Ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// DisplayManager for Ships.
    /// </summary>
    public class ShipDisplayManager : AElementDisplayManager {

        public ShipDisplayManager(IWidgetTrackable trackedShip, Layers meshLayer)
            : base(trackedShip, meshLayer) { }

    }

}

