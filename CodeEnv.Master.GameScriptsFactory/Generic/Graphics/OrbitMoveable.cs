// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OrbitMoveable.cs
// Class that simulates the movement of an object orbiting around a location that is moveable.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Class that simulates the movement of an object orbiting around a location that
/// is moveable. Both orbital movement and self rotation of the orbiting object are implemented.
/// Assumes this script is attached to a parent of [rotatingObject] whose position is coincident
/// with that of the moveable object it is orbiting. This script simulates
/// orbital movement of [rotatingObject] by rotating this parent object.
/// </summary>
public class OrbitMoveable : Orbit {

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

