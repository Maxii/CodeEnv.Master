// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MovingOrbitSimulator.cs
// Class that simulates the movement of an object orbiting around a location that can move.
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
/// Class that simulates the movement of an object orbiting around a location that can move.
/// Assumes this script is attached to an otherwise empty gameobject [the orbiterGO] whose parent is the object
/// being orbited. The position of this orbiterGO should be coincident with that of the object being orbited. The
/// object that is orbiting is either parented to this orbiterGO or attached with a fixed joint, thus simulating 
/// orbital movement by rotating the orbiterGO.
/// </summary>
public class MovingOrbitSimulator : OrbitSimulator {

    /// <summary>
    /// Updates the rotation of this object around its current location in worldspace
    /// (it is coincident with the position of the object being orbited)
    /// to simulate the orbit of anything attached around the object orbited.
    /// </summary>
    /// <param name="deltaTime">The delta time.</param>
    protected override void UpdateOrbit(float deltaTime) {
        float desiredStepAngle = _orbitSpeedInDegreesPerSecond * deltaTime;
        //D.Log("{0}.{1}.desiredStepAngle = {2}.", _transform.name, GetType().Name, desiredStepAngle);
        _transform.RotateAround(_transform.position, axisOfOrbit, desiredStepAngle);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

