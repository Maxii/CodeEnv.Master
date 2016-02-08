// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MovingShipOrbitSimulator.cs
// Class that simulates the movement of ships orbiting around an IShipOrbitable object that itself moves.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Class that simulates the movement of ships orbiting around an IShipOrbitable object that itself moves.
/// Assumes this script is attached to an otherwise empty gameobject [the orbiterGO] whose parent is the IShipOrbitable object
/// being orbited. The position of this orbiterGO should be coincident with that of the IShipOrbitable object being orbited. The
/// ships that are orbiting are either parented to this orbiterGO or attached with a fixed joint, thus simulating orbital movement by 
/// rotating the orbiterGO.
/// </summary>
public class MovingShipOrbitSimulator : MovingOrbitSimulator, IMovingShipOrbitSimulator {

    public Rigidbody Rigidbody { get; private set; }

    /// <summary>
    /// The direction in worldspace this MovingShipOrbitSimulator is currently traveling.
    /// </summary>
    public Vector3 DirectionOfTravel {
        get {
            D.Assert(IsActivelyOrbiting);   // Ships always actively orbit. _previousPosition not valid without it
            return (transform.position - _previousPosition).normalized;
        }
    }

    /// <summary>
    /// The position of this orbitSimulator (which is the same as the position of its moving OrbitedObject) the last time
    /// the orbit was updated. Only valid if this simulator is actively rotating around its moving OrbitedObject as otherwise
    /// Update() is not enabled and _previousPosition is therefore not updated.
    /// </summary>
    private Vector3 _previousPosition;

    protected override void Awake() {
        base.Awake();
        Rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        Rigidbody.isKinematic = true;
        Rigidbody.useGravity = false;
        _previousPosition = transform.position;
    }

    protected override void UpdateOther() {
        base.UpdateOther();
        _previousPosition = transform.position;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

