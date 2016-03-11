// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MobileOrbitSimulator.cs
// Simulates orbiting around a mobile parent of any children of the simulator.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Simulates orbiting around a mobile parent of any children of the simulator.
/// </summary>
public class MobileOrbitSimulator : OrbitSimulator {

    /// <summary>
    /// Updates the rotation of this orbit simulator around its current location in worldspace.
    /// </summary>
    /// <param name="deltaTimeSinceLastUpdate">The delta time (zero if paused) since last update.</param>
    protected override void UpdateOrbit(float deltaTimeSinceLastUpdate) {
        float angleStep = _orbitRateInDegreesPerHour * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTimeSinceLastUpdate;
        transform.RotateAround(transform.position, _axisOfOrbit, angleStep);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

