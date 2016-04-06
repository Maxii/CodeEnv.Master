// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MobileShipCloseOrbitSimulator.cs
// Simulates orbiting around a mobile parent of any ships attached by a fixed joint.
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
/// Simulates orbiting around a mobile parent of any ships attached by a fixed joint.
/// This is also an INavigableTarget which allows it to be used as a destination by a Ship's AutoPilot.
/// </summary>
public class MobileShipCloseOrbitSimulator : ShipCloseOrbitSimulator, IMobileShipCloseOrbitSimulator {

    /// <summary>
    /// The worldspace direction of travel of the OrbitedObject around which this simulator is rotating.
    /// </summary>
    [System.Obsolete]
    public Vector3 DirectionOfTravel {
        get {
            D.Assert(OrbitData.ToOrbit);   // Ships always actively orbit. _previousPosition not valid without it
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
        _previousPosition = transform.position;
    }

    /// <summary>
    /// Updates the rotation of this simulator around its current location in worldspace.
    /// </summary>
    /// <param name="deltaTimeSinceLastUpdate">The delta time (zero if paused) since last update.</param>
    protected override void UpdateOrbit(float deltaTimeSinceLastUpdate) {
        float angleStep = _orbitRateInDegreesPerHour * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTimeSinceLastUpdate;
        transform.RotateAround(transform.position, _axisOfOrbit, angleStep);
        _previousPosition = transform.position;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

