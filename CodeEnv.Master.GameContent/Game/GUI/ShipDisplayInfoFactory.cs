// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipDisplayInfoFactory.cs
// Factory that makes instances of text containing info about Ships.
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
    /// Factory that makes instances of text containing info about Ships.
    /// </summary>
    public class ShipDisplayInfoFactory : AElementItemDisplayInfoFactory<ShipReport, ShipDisplayInfoFactory> {

        private static AccessControlInfoID[] _infoIDsToDisplay = new AccessControlInfoID[] {
            AccessControlInfoID.Name,
            AccessControlInfoID.ParentName,
            AccessControlInfoID.Owner,
            AccessControlInfoID.Category,
            AccessControlInfoID.Health,
            AccessControlInfoID.Defense,
            AccessControlInfoID.Offense,
            AccessControlInfoID.WeaponsRange,
            //AccessControlInfoID.SensorRange,  // makes no sense
            AccessControlInfoID.Science,
            AccessControlInfoID.Culture,
            AccessControlInfoID.NetIncome,
            AccessControlInfoID.Mass,

            AccessControlInfoID.Target,
            AccessControlInfoID.TargetDistance,
            AccessControlInfoID.CombatStance,
            AccessControlInfoID.CurrentSpeed,
            AccessControlInfoID.FullSpeed,
            AccessControlInfoID.MaxTurnRate,

            AccessControlInfoID.CameraDistance,
            AccessControlInfoID.IntelState
        };

        protected override AccessControlInfoID[] InfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private ShipDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(AccessControlInfoID infoID, ShipReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case AccessControlInfoID.Category:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Category != ShipHullCategory.None ? report.Category.GetValueName() : _unknown);
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
                    case AccessControlInfoID.FullSpeed:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.FullSpeed.HasValue ? GetFormat(infoID).Inject(report.FullSpeed.Value) : _unknown);
                        break;
                    case AccessControlInfoID.MaxTurnRate:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.MaxTurnRate.HasValue ? GetFormat(infoID).Inject(report.MaxTurnRate.Value) : _unknown);
                        break;
                    case AccessControlInfoID.CombatStance:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.CombatStance != ShipCombatStance.None ? report.CombatStance.GetValueName() : _unknown);
                        break;
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(infoID));
                }
            }
            return isSuccess;
        }

        private float? CalcTargetDistance(INavigable target, Vector3? shipPosition) {
            if (target != null) {
                IMortalItem mortalTgt = target as IMortalItem;
                if (mortalTgt != null && !mortalTgt.IsOperational) {
                    // target has died so don't try to access its position
                    return null;
                }
                if (shipPosition.HasValue) {
                    return Vector3.Distance(target.Position, shipPosition.Value);
                }
            }
            return null;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

