// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SpeedExtensions.cs
// Extension methods for Speed values.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Extension methods for Speed values.
    /// </summary>
    public static class SpeedExtensions {    //TODO Externalize values in XML.

        /// <summary>
        /// Gets the speed in units per hour for this ship or fleet. Either or both datas
        /// can be null if you are certain which speed and move mode you are asking for. If
        /// moveMode = None, then you must be asking for a constant speed value.
        /// </summary>
        /// <param name="speed">The speed enum value.</param>
        /// <param name="moveMode">The move mode.</param>
        /// <param name="shipData">The ship data.</param>
        /// <param name="fleetData">The fleet data.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static float GetUnitsPerHour(this Speed speed, ShipMoveMode moveMode, ShipData shipData, FleetCmdData fleetData) {
            if (moveMode == ShipMoveMode.None) {
                D.Assert(speed == Speed.EmergencyStop || speed == Speed.Stop || speed == Speed.Docking || speed == Speed.StationaryOrbit ||
                    speed == Speed.MovingOrbit || speed == Speed.Slow);
            }

            // Note: see Flight.txt in GameDev Notes for analysis of speed values

            float fullSpeedFactor = Constants.ZeroF;
            switch (speed) {
                case Speed.EmergencyStop:
                    return Constants.ZeroF;
                case Speed.Stop:
                    return Constants.ZeroF;
                case Speed.Docking:
                    return 0.04F;
                case Speed.StationaryOrbit:
                    return 0.1F;
                case Speed.MovingOrbit: // Typical Planet Orbital Speed ~ 0.1, Moons are no longer IShipOrbitable
                    return 0.2F;
                case Speed.Slow:
                    return 0.3F;
                case Speed.OneThird:
                    // 11.24.15 InSystem, STL = 0.4, OpenSpace, FTL = 10
                    fullSpeedFactor = 0.25F;
                    break;
                case Speed.TwoThirds:
                    // 11.24.15 InSystem, STL = 0.8, OpenSpace, FTL = 20
                    fullSpeedFactor = 0.50F;
                    break;
                case Speed.Standard:
                    // 11.24.15 InSystem, STL = 1.2, OpenSpace, FTL = 30
                    fullSpeedFactor = 0.75F;
                    break;
                case Speed.Full:
                    // 11.24.15 InSystem, STL = 1.6, OpenSpace, FTL = 40
                    fullSpeedFactor = 1.0F;
                    break;
                case Speed.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(speed));
            }

            float fullSpeed = Constants.ZeroF;
            if (moveMode == ShipMoveMode.ShipSpecific) {
                fullSpeed = shipData.FullSpeedValue; // 11.24.15 InSystem, STL = 1.6, OpenSpace, FTL = 40
                //D.Log("{0}.FullSpeed = {1} units/hour. IsFtlOperational = {2}.", shipData.FullName, fullSpeed, shipData.IsFtlOperational);
            }
            else {
                fullSpeed = fleetData.UnitFullSpeedValue;   // 11.24.15 InSystem, STL = 1.6, OpenSpace, FTL = 40
                //D.Log("{0}.FullSpeed = {1} units/hour.", fleetData.FullName, fullSpeed);
            }
            return fullSpeedFactor * fullSpeed;
        }

        /// <summary>
        /// Tries to decrease the speed by one step below the source speed. Returns
        /// <c>true</c> if successful, <c>false</c> otherwise. Throws an exception if sourceSpeed
        /// represents a speed of value zero - aka no velocity.
        /// </summary>
        /// <param name="sourceSpeed">The source speed.</param>
        /// <param name="newSpeed">The new speed.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static bool TryDecreaseSpeed(this Speed sourceSpeed, out Speed newSpeed) {
            D.Assert(sourceSpeed != Speed.None && sourceSpeed != Speed.EmergencyStop && sourceSpeed != Speed.Stop);
            newSpeed = Speed.None;

            switch (sourceSpeed) {
                case Speed.Docking:
                    return false;
                case Speed.StationaryOrbit:
                    newSpeed = Speed.Docking;
                    return true;
                case Speed.MovingOrbit:
                    newSpeed = Speed.StationaryOrbit;
                    return true;
                case Speed.Slow:
                    newSpeed = Speed.MovingOrbit;
                    return true;
                case Speed.OneThird:
                    newSpeed = Speed.Slow;
                    return true;
                case Speed.TwoThirds:
                    newSpeed = Speed.OneThird;
                    return true;
                case Speed.Standard:
                    newSpeed = Speed.TwoThirds;
                    return true;
                case Speed.Full:
                    newSpeed = Speed.Standard;
                    return true;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(sourceSpeed));
            }
        }

        /// <summary>
        /// Tries to increase the speed by one step above the source speed. Returns
        /// <c>true</c> if successful, <c>false</c> otherwise. Throws an exception if sourceSpeed
        /// represents a speed of value zero - aka no velocity.
        /// </summary>
        /// <param name="sourceSpeed">The source speed.</param>
        /// <param name="newSpeed">The new speed.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static bool TryIncreaseSpeed(this Speed sourceSpeed, out Speed newSpeed) {
            D.Assert(sourceSpeed != Speed.None && sourceSpeed != Speed.EmergencyStop && sourceSpeed != Speed.Stop);
            newSpeed = Speed.None;

            switch (sourceSpeed) {
                case Speed.Docking:
                    newSpeed = Speed.StationaryOrbit;
                    return true;
                case Speed.StationaryOrbit:
                    newSpeed = Speed.MovingOrbit;
                    return true;
                case Speed.MovingOrbit:
                    newSpeed = Speed.Slow;
                    return true;
                case Speed.Slow:
                    newSpeed = Speed.OneThird;
                    return true;
                case Speed.OneThird:
                    newSpeed = Speed.TwoThirds;
                    return true;
                case Speed.TwoThirds:
                    newSpeed = Speed.Standard;
                    return true;
                case Speed.Standard:
                    newSpeed = Speed.Full;
                    return true;
                case Speed.Full:
                    return false;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(sourceSpeed));
            }
        }
    }
}

