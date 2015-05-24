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
    public class FleetDisplayInfoFactory : AUnitCmdDisplayInfoFactory<FleetReport, FleetDisplayInfoFactory> {

        private static ContentID[] _contentIDsToDisplay = new ContentID[] {                         
            ContentID.Name,
            ContentID.ParentName,
            ContentID.Category,
            ContentID.Composition,
            ContentID.Owner,

            ContentID.CurrentCmdEffectiveness,
            ContentID.Formation,
            ContentID.UnitOffense,
            ContentID.UnitDefense,
            ContentID.UnitHealth,
            ContentID.UnitMaxWeaponsRange,
            ContentID.UnitMaxSensorRange,
            ContentID.UnitScience,
            ContentID.UnitCulture,
            ContentID.UnitNetIncome,

            ContentID.Target,
            ContentID.TargetDistance,
            ContentID.CurrentSpeed,
            ContentID.UnitFullSpeed,
            ContentID.UnitMaxTurnRate,

            ContentID.CameraDistance,
            ContentID.IntelState
        };

        protected override ContentID[] ContentIDsToDisplay { get { return _contentIDsToDisplay; } }

        private FleetDisplayInfoFactory() {
            Initialize();
        }

        protected override void Initialize() { }

        protected override bool TryMakeColorizedText(ContentID contentID, FleetReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(contentID, report, out colorizedText);
            if (!isSuccess) {
                switch (contentID) {
                    case ContentID.Category:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Category != FleetCategory.None ? report.Category.GetName() : _unknown);
                        break;
                    case ContentID.Composition:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitComposition != null ? report.UnitComposition.ToString() : _unknown);
                        break;
                    case ContentID.Target:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Target != null ? report.Target.DisplayName : _unknown);
                        break;
                    case ContentID.TargetDistance:
                        isSuccess = true;
                        float? targetDistance = CalcTargetDistance(report.Target, report.Position);
                        colorizedText = _phrase.Inject(targetDistance.HasValue ? GetFormat(contentID).Inject(targetDistance.Value) : _unknown);
                        break;
                    case ContentID.CurrentSpeed:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.CurrentSpeed.HasValue ? GetFormat(contentID).Inject(report.CurrentSpeed.Value) : _unknown);
                        break;
                    case ContentID.UnitFullSpeed:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitFullSpeed.HasValue ? GetFormat(contentID).Inject(report.UnitFullSpeed.Value) : _unknown);
                        break;
                    case ContentID.UnitMaxTurnRate:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitMaxTurnRate.HasValue ? GetFormat(contentID).Inject(report.UnitMaxTurnRate.Value) : _unknown);
                        break;
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(contentID));
                }
            }
            return isSuccess;
        }

        private float? CalcTargetDistance(INavigableTarget target, Vector3? fleetPosition) {
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

