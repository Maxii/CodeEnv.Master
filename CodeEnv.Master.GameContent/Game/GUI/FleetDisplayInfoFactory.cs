// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetDisplayInfoFactory.cs
// Factory that makes instances of text containing info about Fleets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Factory that makes instances of text containing info about Fleets.
    /// </summary>
    public class FleetDisplayInfoFactory : AUnitCmdDisplayInfoFactory<FleetCmdReport, FleetDisplayInfoFactory> {

        private static AccessControlInfoID[] _infoIDsToDisplay = new AccessControlInfoID[] {
            AccessControlInfoID.Name,
            AccessControlInfoID.ParentName,
            AccessControlInfoID.Category,
            AccessControlInfoID.Composition,
            AccessControlInfoID.Owner,

            AccessControlInfoID.CurrentCmdEffectiveness,
            AccessControlInfoID.Formation,
            AccessControlInfoID.UnitOffense,
            AccessControlInfoID.UnitDefense,
            AccessControlInfoID.UnitHealth,
            AccessControlInfoID.UnitWeaponsRange,
            AccessControlInfoID.UnitSensorRange,
            AccessControlInfoID.UnitScience,
            AccessControlInfoID.UnitCulture,
            AccessControlInfoID.UnitNetIncome,

            AccessControlInfoID.Target,
            AccessControlInfoID.TargetDistance,
            AccessControlInfoID.CurrentSpeed,
            AccessControlInfoID.UnitFullSpeed,
            AccessControlInfoID.UnitMaxTurnRate,

            AccessControlInfoID.CameraDistance,
            AccessControlInfoID.IntelState
        };

        protected override AccessControlInfoID[] InfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private FleetDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(AccessControlInfoID infoID, FleetCmdReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case AccessControlInfoID.Category:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Category != FleetCategory.None ? report.Category.GetValueName() : _unknown);
                        break;
                    case AccessControlInfoID.Composition:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitComposition != null ? report.UnitComposition.ToString() : _unknown);
                        break;
                    case AccessControlInfoID.Target:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Target != null ? report.Target.DisplayName : _unknown);
                        break;
                    case AccessControlInfoID.TargetDistance:
                        isSuccess = true;
                        float? targetDistance = CalcTargetDistance(report.Target, report.Position);
                        colorizedText = _phrase.Inject(targetDistance.HasValue ? GetFormat(infoID).Inject(targetDistance.Value) : _unknown);
                        break;
                    case AccessControlInfoID.CurrentSpeed:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.CurrentSpeed.HasValue ? GetFormat(infoID).Inject(report.CurrentSpeed.Value) : _unknown);
                        break;
                    case AccessControlInfoID.UnitFullSpeed:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitFullSpeed.HasValue ? GetFormat(infoID).Inject(report.UnitFullSpeed.Value) : _unknown);
                        break;
                    case AccessControlInfoID.UnitMaxTurnRate:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitMaxTurnRate.HasValue ? GetFormat(infoID).Inject(report.UnitMaxTurnRate.Value) : _unknown);
                        break;
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(infoID));
                }
            }
            return isSuccess;
        }

        private float? CalcTargetDistance(INavigable target, Vector3? fleetPosition) {
            if (target != null && fleetPosition.HasValue) {
                return Vector3.Distance(target.Position, fleetPosition.Value);
            }
            return null;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

