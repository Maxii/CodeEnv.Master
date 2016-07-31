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

        private static ItemInfoID[] _infoIDsToDisplay = new ItemInfoID[] {
            ItemInfoID.Name,
            ItemInfoID.ParentName,
            ItemInfoID.Category,
            ItemInfoID.Composition,
            ItemInfoID.Owner,
            ItemInfoID.SectorIndex,

            ItemInfoID.CurrentCmdEffectiveness,
            ItemInfoID.Formation,
            ItemInfoID.UnitOffense,
            ItemInfoID.UnitDefense,
            ItemInfoID.UnitHealth,
            ItemInfoID.UnitWeaponsRange,
            ItemInfoID.UnitSensorRange,
            ItemInfoID.UnitScience,
            ItemInfoID.UnitCulture,
            ItemInfoID.UnitNetIncome,

            ItemInfoID.Target,
            ItemInfoID.TargetDistance,
            ItemInfoID.CurrentSpeedSetting,
            ItemInfoID.UnitFullSpeed,
            ItemInfoID.UnitMaxTurnRate,

            ItemInfoID.Separator,

            ItemInfoID.IntelState,

            ItemInfoID.Separator,

            ItemInfoID.ActualSpeed,
            ItemInfoID.CameraDistance
        };

        protected override ItemInfoID[] OrderedInfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private FleetDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(ItemInfoID infoID, FleetCmdReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case ItemInfoID.Category:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Category != FleetCategory.None ? report.Category.GetValueName() : Unknown);
                        break;
                    case ItemInfoID.Composition:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitComposition != null ? report.UnitComposition.ToString() : Unknown);
                        break;
                    case ItemInfoID.Target:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Target != null ? report.Target.DisplayName : Unknown);
                        break;
                    case ItemInfoID.TargetDistance:
                        isSuccess = true;
                        float? targetDistance = CalcTargetDistance(report.Target, report.Position);
                        colorizedText = _lineTemplate.Inject(targetDistance.HasValue ? GetFormat(infoID).Inject(targetDistance.Value) : Unknown);
                        break;
                    case ItemInfoID.CurrentSpeedSetting:
                        isSuccess = true;
                        float? speedSettingValue = CalcSpeedSettingValue(report.CurrentSpeedSetting, report.UnitFullSpeed);
                        string speedSettingText = report.CurrentSpeedSetting != Speed.None ? report.CurrentSpeedSetting.GetValueName() : Unknown;
                        colorizedText = _lineTemplate.Inject(speedSettingText, speedSettingValue.HasValue ? GetFormat(infoID).Inject(speedSettingValue.Value) : Unknown);
                        break;
                    case ItemInfoID.UnitFullSpeed:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitFullSpeed.HasValue ? GetFormat(infoID).Inject(report.UnitFullSpeed.Value) : Unknown);
                        break;
                    case ItemInfoID.UnitMaxTurnRate:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitMaxTurnRate.HasValue ? GetFormat(infoID).Inject(report.UnitMaxTurnRate.Value) : Unknown);
                        break;

                    case ItemInfoID.ActualSpeed:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(GetFormat(infoID).Inject(report.__ActualSpeedValue.Value));
                        break;
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(infoID));
                }
            }
            return isSuccess;
        }

        private float? CalcSpeedSettingValue(Speed speedSetting, float? fullSpeedValue) {
            if (speedSetting != Speed.None) {
                if (fullSpeedValue.HasValue) {
                    return speedSetting.GetUnitsPerHour(fullSpeedValue.Value);
                }
            }
            return null;
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

