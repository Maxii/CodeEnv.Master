// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IOrbitable.cs
// Interface for objects that can be orbited.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for objects that can be orbited.
    /// </summary>
    public interface IOrbitable {

        /// <summary>
        /// Readonly. The distance from the object's position (center) that ships
        /// and fleets should orbit. 
        /// </summary>
        float OrbitDistance { get; }

        void AssumeOrbit(IShipModel ship);

        void LeaveOrbit(IShipModel orbitingShip);

        string FullName { get; }

    }
}

