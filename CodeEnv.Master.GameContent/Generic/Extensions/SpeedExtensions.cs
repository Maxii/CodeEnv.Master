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
    /// TODO Externalize values in XML.
    /// </summary>
    public static class SpeedExtensions {

        /// <summary>
        /// Gets the speed in units per hour for this ship or fleet. Either data
        /// can be null (but not both) if you are certain which speed you are asking for.
        /// </summary>
        /// <param name="speed">The speed enum value.</param>
        /// <param name="fleetData">The fleet data.</param>
        /// <param name="shipData">The ship data.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static float GetValue(this Speed speed, FleetCmdData fleetData, ShipData shipData = null) {
            D.Assert(fleetData != null || shipData != null);

            float fleetFullSpeed = Constants.ZeroF;
            if (fleetData != null) {
                fleetFullSpeed = fleetData.UnitFullSpeed;
                //D.Log("{0}.FullSpeed = {1} units/hour.", fleetData.FullName, fleetFullSpeed);
            }

            float shipFullSpeed = Constants.ZeroF;
            if (shipData != null) {
                shipFullSpeed = shipData.FullSpeed;
                //D.Log("{0}.FullSpeed = {1} units/hour. FtlAvailable = {2}.", shipData.FullName, shipFullSpeed, shipData.IsFtlAvailableForUse);
            }

            float result;

            switch (speed) {
                case Speed.EmergencyStop:
                    return Constants.ZeroF;
                case Speed.Stop:
                    return Constants.ZeroF;
                case Speed.Thrusters:
                    result = 0.02F * shipData.FullStlSpeed;
                    break;
                case Speed.Slow:
                    result = 0.10F * shipData.FullStlSpeed;
                    break;
                case Speed.OneThird:
                    result = 0.25F * shipFullSpeed;
                    break;
                case Speed.TwoThirds:
                    result = 0.5F * shipFullSpeed;
                    break;
                case Speed.Standard:
                    result = 0.75F * shipFullSpeed;
                    break;
                case Speed.Full:
                    result = 1.0F * shipFullSpeed;
                    break;
                //case Speed.Flank:
                //    stlSpeed = 1.10F * shipFullSpeed;
                //    break;

                case Speed.FleetSlow:
                    result = 0.10F * fleetData.UnitFullStlSpeed;
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
        /// Trys to decrease the provided speed by the step indicated. Returns <c>true</c> if any decrease
        /// was made, even if the decrease was smaller than the step requested, otherwise <c>false</c>. 
        /// The resulting decreased Speed value is provided as <c>newSpeed</c>. If no decreased value is
        /// available, the method returns <c>false</c> and <c>newSpeed</c> will be set to Speed.None.
        /// </summary>
        /// <param name="speed">The speed.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
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
                case Speed.Thrusters:
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
                            newSpeed = Speed.Thrusters;
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
                            newSpeed = Speed.Thrusters;
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
                            newSpeed = Speed.Thrusters;
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
                            newSpeed = Speed.Thrusters;
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
                            newSpeed = Speed.Thrusters;
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

