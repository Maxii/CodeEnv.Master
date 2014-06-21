﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MovingOrbiter.cs
// Class that simulates the movement of an object orbiting around a location that is moveable.
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
/// Class that simulates the movement of an object orbiting around a location that is moveable. 
/// Assumes this script is attached to a parent of the orbiting object whose position is coincident
/// with that of the moveable object that is being orbited. This script simulates
/// orbital movement of the orbiting object by rotating this parent object.
/// </summary>
public class MovingOrbiter : Orbiter {

    /// <summary>
    /// Updates the rotation of this object around its current location in worldspace
    /// (it is coincident with the position of the object being orbited)
    /// to simulate the orbit of this object's child around the object orbited.
    /// </summary>
    /// <param name="deltaTime">The delta time.</param>
    protected override void UpdateOrbit(float deltaTime) {
        _transform.RotateAround(_transform.position, _transform.up, _orbitSpeed * deltaTime);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

