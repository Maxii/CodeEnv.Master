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
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Class that simulates the movement of an object orbiting around a location that
/// is moveable. Both orbital movement and self rotation of the orbiting object are implemented.
/// Assumes this script is attached to a parent of [rotatingObject] whose position is coincident
/// with that of the moveable object it is orbiting. This script simulates
/// orbital movement of [rotatingObject] by rotating this parent object.
/// </summary>
public class OrbitMoveable : Orbit {

    protected override void OnUpdate() {
        float adjustedDeltaTime = GameTime.DeltaTimeOrPausedWithGameSpeed * (int)UpdateRate;
        // rotates this parent object (coincident with the position of the moving object to orbit as it is parented to it) around its 
        // current location in worldspace to simulate orbital movement of the child mesh
        _transform.RotateAround(_transform.position, _transform.up, _orbitSpeed * adjustedDeltaTime);

        if (rotatingObject != null) {
            // rotates the child object around its own LOCAL Y axis
            rotatingObject.Rotate(axisOfRotation * _rotationSpeed * adjustedDeltaTime, relativeTo: Space.Self);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

