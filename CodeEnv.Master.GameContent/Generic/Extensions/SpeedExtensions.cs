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
        /// Gets the speed in units per hour for this ship or fleet. Either data
        /// can be null (but not both) if you are certain which speed you are asking for.
        /// </summary>
        /// <param name="speed">The speed enum value.</param>
        /// <param name="fleetData">The fleet data.</param>
        /// <param name="shipData">The ship data.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static float GetUnitsPerHour(this Speed speed, FleetCmdData fleetData, ShipData shipData = null) {
            D.Assert(fleetData != null || shipData != null);

            // Note: see Flight.txt in GameDev Notes for analysis

            float fleetFullSpeed = Constants.ZeroF;
            if (fleetData != null) {
                fleetFullSpeed = fleetData.UnitFullSpeedValue;   // 11.24.15 InSystem, STL = 1.6, OpenSpace, FTL = 40
                //D.Log("{0}.FullSpeed = {1} units/hour.", fleetData.FullName, fleetFullSpeed);
            }

            float shipFullSpeed = Constants.ZeroF;
            if (shipData != null) {
                shipFullSpeed = shipData.FullSpeedValue; // 11.24.15 InSystem, STL = 1.6, OpenSpace, FTL = 40
                //D.Log("{0}.FullSpeed = {1} units/hour. FtlAvailable = {2}.", shipData.FullName, shipFullSpeed, shipData.IsFtlAvailableForUse);
            }

            float result;
            switch (speed) {
                case Speed.EmergencyStop:
                    return Constants.ZeroF;
                case Speed.Stop:
                    return Constants.ZeroF;
                case Speed.Docking:
                    result = 0.04F;
                    break;
                case Speed.StationaryOrbit:
                    result = 0.1F;
                    break;
                case Speed.MovingOrbit: // Typical Planet Orbital Speed ~ 0.1, Moons are no longer IShipOrbitable
                    result = 0.2F;
                    break;
                case Speed.Slow:
                    result = 0.3F;
                    break;
                case Speed.OneThird:
                    result = 0.25F * shipFullSpeed; // 11.24.15 InSystem, STL = 0.4, OpenSpace, FTL = 10
                    break;
                case Speed.TwoThirds:
                    result = 0.5F * shipFullSpeed;  // 11.24.15 InSystem, STL = 0.8, OpenSpace, FTL = 20
                    break;
                case Speed.Standard:
                    result = 0.75F * shipFullSpeed; // 11.24.15 InSystem, STL = 1.2, OpenSpace, FTL = 30
                    break;
                case Speed.Full:
                    result = 1.0F * shipFullSpeed;  // 11.24.15 InSystem, STL = 1.6, OpenSpace, FTL = 40
                    break;

                case Speed.FleetSlow:
                    result = 0.3F;
                    break;
                case Speed.FleetOneThird:
                    result = 0.25F * fleetFullSpeed;
                    break;
                case Speed.FleetTwoThirds:
                    result = 0.5F * fleetFullSpeed;
                    break;
                case Speed.FleetStandard:
                    result = 0.75F * fleetFullSpeed;
                    break;
                case Speed.FleetFull:
                    result = 1.0F * fleetFullSpeed;
                    break;

                case Speed.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(speed));
            }
            D.Assert(result != Constants.ZeroF, "Error: ShipData or FleetData is null.");
            return result;
        }

        /// <summary>
        /// Tries to increase the ship speed by one step above the source speed. Returns
        /// <c>true</c> if successful, <c>false</c> otherwise.
        /// </summary>
        /// <param name="sourceShipSpeed">The ship speed.</param>
        /// <param name="newShipSpeed">The new ship speed.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static bool TryIncreaseShipSpeed(this Speed sourceShipSpeed, out Speed newShipSpeed) {
            D.Assert(sourceShipSpeed != Speed.None && sourceShipSpeed != Speed.EmergencyStop && sourceShipSpeed != Speed.Stop && sourceShipSpeed != Speed.FleetSlow &&
                sourceShipSpeed != Speed.FleetOneThird && sourceShipSpeed != Speed.FleetTwoThirds && sourceShipSpeed != Speed.FleetStandard && sourceShipSpeed != Speed.FleetFull);
            newShipSpeed = Speed.None;

            switch (sourceShipSpeed) {
                case Speed.Docking:
                    newShipSpeed = Speed.StationaryOrbit;
                    return true;
                case Speed.StationaryOrbit:
                    newShipSpeed = Speed.MovingOrbit;
                    return true;
                case Speed.MovingOrbit:
                    newShipSpeed = Speed.Slow;
                    return true;
                case Speed.Slow:
                    newShipSpeed = Speed.OneThird;
                    return true;
                case Speed.OneThird:
                    newShipSpeed = Speed.TwoThirds;
                    return true;
                case Speed.TwoThirds:
                    newShipSpeed = Speed.Standard;
                    return true;
                case Speed.Standard:
                    newShipSpeed = Speed.Full;
                    return true;
                case Speed.Full:
                    return false;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(sourceShipSpeed));
            }


        }

        /// <summary>
        /// Trys to decrease the provided speed by the step indicated. Returns <c>true</c> if any decrease
        /// was made, even if the decrease was smaller than the step requested, otherwise <c>false</c>. 
        /// The resulting decreased Speed value is provided as <c>newSpeed</c>. If no decreased value is
        /// available, the method returns <c>false</c> and <c>newSpeed</c> will be set to Speed.None.
        /// </summary>
        /// <param name="speed">The speed.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        [Obsolete]
        public static bool TryDecrease(this Speed speed, SpeedStep step, out Speed newSpeed) {
            D.Assert(speed != Speed.None);
            newSpeed = Speed.None;
            if (step == SpeedStep.None) { return false; }
            switch (speed) {
                case Speed.EmergencyStop:
                    return false;
                case Speed.Stop:
                    newSpeed = Speed.EmergencyStop;
                    return true;
                case Speed.Docking:
                    switch (step) {
                        case SpeedStep.Minimum:
                            newSpeed = Speed.Stop;
                            break;
                        case SpeedStep.One:
                        case SpeedStep.Two:
                        case SpeedStep.Three:
                        case SpeedStep.Four:
                        case SpeedStep.Five:
                        case SpeedStep.Maximum:
                            newSpeed = Speed.EmergencyStop;
                            break;
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(step));
                    }
                    return true;
                case Speed.Slow:
                    switch (step) {
                        case SpeedStep.Minimum:
                            newSpeed = Speed.Docking;
                            break;
                        case SpeedStep.One:
                            newSpeed = Speed.Stop;
                            break;
                        case SpeedStep.Two:
                        case SpeedStep.Three:
                        case SpeedStep.Four:
                        case SpeedStep.Five:
                        case SpeedStep.Maximum:
                            newSpeed = Speed.EmergencyStop;
                            break;
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(step));
                    }
                    return true;
                case Speed.OneThird:
                    switch (step) {
                        case SpeedStep.Minimum:
                            newSpeed = Speed.Slow;
                            break;
                        case SpeedStep.One:
                            newSpeed = Speed.Docking;
                            break;
                        case SpeedStep.Two:
                            newSpeed = Speed.Stop;
                            break;
                        case SpeedStep.Three:
                        case SpeedStep.Four:
                        case SpeedStep.Five:
                        case SpeedStep.Maximum:
                            newSpeed = Speed.EmergencyStop;
                            break;
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(step));
                    }
                    return true;
                case Speed.TwoThirds:
                    switch (step) {
                        case SpeedStep.Minimum:
                            newSpeed = Speed.OneThird;
                            break;
                        case SpeedStep.One:
                            newSpeed = Speed.Slow;
                            break;
                        case SpeedStep.Two:
                            newSpeed = Speed.Docking;
                            break;
                        case SpeedStep.Three:
                            newSpeed = Speed.Stop;
                            break;
                        case SpeedStep.Four:
                        case SpeedStep.Five:
                        case SpeedStep.Maximum:
                            newSpeed = Speed.EmergencyStop;
                            break;
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(step));
                    }
                    return true;
                case Speed.Standard:
                    switch (step) {
                        case SpeedStep.Minimum:
                            newSpeed = Speed.TwoThirds;
                            break;
                        case SpeedStep.One:
                            newSpeed = Speed.OneThird;
                            break;
                        case SpeedStep.Two:
                            newSpeed = Speed.Slow;
                            break;
                        case SpeedStep.Three:
                            newSpeed = Speed.Docking;
                            break;
                        case SpeedStep.Four:
                            newSpeed = Speed.Stop;
                            break;
                        case SpeedStep.Five:
                        case SpeedStep.Maximum:
                            newSpeed = Speed.EmergencyStop;
                            break;
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(step));
                    }
                    return true;
                case Speed.Full:
                    switch (step) {
                        case SpeedStep.Minimum:
                            newSpeed = Speed.Standard;
                            break;
                        case SpeedStep.One:
                            newSpeed = Speed.TwoThirds;
                            break;
                        case SpeedStep.Two:
                            newSpeed = Speed.OneThird;
                            break;
                        case SpeedStep.Three:
                            newSpeed = Speed.Slow;
                            break;
                        case SpeedStep.Four:
                            newSpeed = Speed.Docking;
                            break;
                        case SpeedStep.Five:
                            newSpeed = Speed.Stop;
                            break;
                        case SpeedStep.Maximum:
                            newSpeed = Speed.EmergencyStop;
                            break;
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(step));
                    }
                    return true;
                case Speed.FleetSlow:
                    switch (step) {
                        case SpeedStep.Minimum:
                            newSpeed = Speed.Stop;
                            break;
                        case SpeedStep.One:
                        case SpeedStep.Two:
                        case SpeedStep.Three:
                        case SpeedStep.Four:
                        case SpeedStep.Five:
                        case SpeedStep.Maximum:
                            newSpeed = Speed.EmergencyStop;
                            break;
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(step));
                    }
                    return true;
                case Speed.FleetOneThird:
                    switch (step) {
                        case SpeedStep.Minimum:
                            newSpeed = Speed.FleetSlow;
                            break;
                        case SpeedStep.One:
                            newSpeed = Speed.Stop;
                            break;
                        case SpeedStep.Two:
                        case SpeedStep.Three:
                        case SpeedStep.Four:
                        case SpeedStep.Five:
                        case SpeedStep.Maximum:
                            newSpeed = Speed.EmergencyStop;
                            break;
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(step));
                    }
                    return true;
                case Speed.FleetTwoThirds:
                    switch (step) {
                        case SpeedStep.Minimum:
                            newSpeed = Speed.FleetOneThird;
                            break;
                        case SpeedStep.One:
                            newSpeed = Speed.FleetSlow;
                            break;
                        case SpeedStep.Two:
                            newSpeed = Speed.Stop;
                            break;
                        case SpeedStep.Three:
                        case SpeedStep.Four:
                        case SpeedStep.Five:
                        case SpeedStep.Maximum:
                            newSpeed = Speed.EmergencyStop;
                            break;
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(step));
                    }
                    return true;
                case Speed.FleetStandard:
                    switch (step) {
                        case SpeedStep.Minimum:
                            newSpeed = Speed.FleetTwoThirds;
                            break;
                        case SpeedStep.One:
                            newSpeed = Speed.FleetOneThird;
                            break;
                        case SpeedStep.Two:
                            newSpeed = Speed.FleetSlow;
                            break;
                        case SpeedStep.Three:
                            newSpeed = Speed.Stop;
                            break;
                        case SpeedStep.Four:
                        case SpeedStep.Five:
                        case SpeedStep.Maximum:
                            newSpeed = Speed.EmergencyStop;
                            break;
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(step));
                    }
                    return true;
                case Speed.FleetFull:
                    switch (step) {
                        case SpeedStep.Minimum:
                            newSpeed = Speed.FleetStandard;
                            break;
                        case SpeedStep.One:
                            newSpeed = Speed.FleetTwoThirds;
                            break;
                        case SpeedStep.Two:
                            newSpeed = Speed.FleetOneThird;
                            break;
                        case SpeedStep.Three:
                            newSpeed = Speed.FleetSlow;
                            break;
                        case SpeedStep.Four:
                            newSpeed = Speed.Stop;
                            break;
                        case SpeedStep.Five:
                        case SpeedStep.Maximum:
                            newSpeed = Speed.EmergencyStop;
                            break;
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(step));
                    }
                    return true;
                case Speed.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(speed));
            }
        }

    }
}

