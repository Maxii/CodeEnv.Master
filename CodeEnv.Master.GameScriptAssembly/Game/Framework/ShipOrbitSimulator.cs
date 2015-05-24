// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipOrbitSimulator.cs
// Class that simulates the movement of an object orbiting around an IShipOrbitable object that does not move.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Class that simulates the movement of ships orbiting around an IShipOrbitable object that does not move.
/// Assumes this script is attached to an otherwise empty gameobject [the orbiterGO] whose parent is the IShipOrbitable object
/// being orbited. The position of this orbiterGO should be coincident with that of the IShipOrbitable object being orbited. The
/// ships that are orbiting are either parented to this orbiterGO or attached with a fixed joint, thus simulating orbital movement by 
/// rotating the orbiterGO.
/// </summary>
public class ShipOrbitSimulator : OrbitSimulator, IShipOrbitSimulator {

    public Rigidbody Rigidbody { get; private set; }

    protected override void Awake() {
        base.Awake();
        Rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        Rigidbody.isKinematic = true;
        Rigidbody.useGravity = false;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

