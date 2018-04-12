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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Simulates orbiting around a mobile parent of any children of the simulator.
/// </summary>
public class MobileOrbitSimulator : OrbitSimulator {

    /// <summary>
    /// The relative orbit speed of the object around the location. A value of 1 means
    /// an orbit will take one OrbitPeriod.
    /// <remarks>TEMP Currently used only for moons as planets orbit immobile stars. If used by something else besides
    /// Moons, I'll need to create a MoonOrbitSimulator derived class to override this value.</remarks>
    /// </summary>
    protected override float RelativeOrbitRate { get { return TempGameValues.RelativeOrbitRateOfMoons; } }

    /// <summary>
    /// Updates the rotation of this orbit simulator around its current location in worldspace.
    /// </summary>
    /// <param name="deltaTimeSinceLastUpdate">The delta time since last update.</param>
    protected override void UpdateOrbit(float deltaTimeSinceLastUpdate) {
        float angleStep = _orbitRateInDegreesPerHour * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTimeSinceLastUpdate;
        transform.RotateAround(transform.position, _axisOfOrbit, angleStep);
    }

    public override string ToString() {
        return DebugName;
    }

}

