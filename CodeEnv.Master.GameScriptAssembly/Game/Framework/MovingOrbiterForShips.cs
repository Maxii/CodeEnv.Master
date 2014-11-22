// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MovingOrbiterForShips.cs
// Class that simulates the movement of ships orbiting around an IShipOrbitable object that is mobile.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Class that simulates the movement of ships orbiting around an IShipOrbitable object that is mobile.
/// Assumes this script is attached to an otherwise empty gameobject [the orbiterGO] whose parent is the IShipOrbitable object
/// being orbited. The position of this orbiterGO should be coincident with that of the IShipOrbitable object being orbited. The
/// ships that are orbiting are parented to this orbiterGO, thus simulating orbital movement by 
/// changing the rotation of the orbiterGO.
/// </summary>
public class MovingOrbiterForShips : MovingOrbiter, IOrbiterForShips {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

