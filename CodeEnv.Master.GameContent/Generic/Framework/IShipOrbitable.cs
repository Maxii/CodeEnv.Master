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

    /// <summary>
    /// Interface for objects that can be orbited by ships.
    /// </summary>
    public interface IShipOrbitable {

        /// <summary>
        /// Readonly. The maximum distance from the object's position (center) that ships can orbit. 
        /// </summary>
        float MaximumShipOrbitDistance { get; }

        void AssumeOrbit(IShipModel ship);

        void LeaveOrbit(IShipModel orbitingShip);

        string FullName { get; }

    }
}

