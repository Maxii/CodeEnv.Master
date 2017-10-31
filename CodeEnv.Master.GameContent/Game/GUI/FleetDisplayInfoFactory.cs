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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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
            //ItemInfoID.Name,
            ItemInfoID.UnitName,
            ItemInfoID.Category,
            ItemInfoID.Composition,
            ItemInfoID.Owner,
            ItemInfoID.Hero,
            ItemInfoID.SectorID,
            ItemInfoID.AlertStatus,

            //ItemInfoID.CurrentCmdEffectiveness,
            ItemInfoID.Formation,
            //ItemInfoID.UnitOffense,
            //ItemInfoID.UnitDefense,
            ItemInfoID.UnitHealth,
            //ItemInfoID.UnitWeaponsRange,
            ItemInfoID.UnitSensorRange,

            ItemInfoID.UnitOutputs,

            ItemInfoID.Target,
            ItemInfoID.TargetDistance,
            ItemInfoID.CurrentSpeedSetting,
            ItemInfoID.UnitFullSpeed,
            //ItemInfoID.UnitMaxTurnRate,

            ItemInfoID.Separator,

            ItemInfoID.IntelState,

            ItemInfoID.Separator,

            ItemInfoID.ActualSpeed,
            ItemInfoID.CameraDistance
        };

        protected override ItemInfoID[] OrderedInfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private IGameManager _gameMgr;

        private FleetDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() {
            _gameMgr = GameReferences.GameManager;
        }

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
                        colorizedText = _lineTemplate.Inject(GetTargetText(report.Target, report.Owner));
                        break;
                    case ItemInfoID.TargetDistance:
                        isSuccess = true;
                        string noTgtDistanceValueText;
                        float? tgtDistance = CalcTargetDistance(report.Target, report.Position, report.Owner, out noTgtDistanceValueText);
                        colorizedText = _lineTemplate.Inject(tgtDistance.HasValue ? GetFormat(infoID).Inject(tgtDistance.Value) : noTgtDistanceValueText);
                        break;
                    case ItemInfoID.CurrentSpeedSetting:
                        isSuccess = true;
                        float? speedSettingValue = CalcSpeedSettingValue(report.CurrentSpeedSetting, report.UnitFullSpeed, report.Owner);
                        string speedSettingText = GetSpeedSettingText(report.CurrentSpeedSetting, report.Owner);
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

        private string GetTargetText(INavigableDestination target, Player owner) {
            string tgtText = Unknown;
            if (target != null) {
                tgtText = target.DebugName;
            }
            else {
                if (owner != null && (owner.IsUser || owner.IsRelationshipWithUser(DiplomaticRelationship.Alliance))) {
                    tgtText = "None";
                }
            }
            return tgtText;
        }

        private float? CalcTargetDistance(INavigableDestination target, Vector3? fleetPosition, Player owner, out string noTgtDistanceValueText) {
            noTgtDistanceValueText = Unknown;
            if (owner != null && (owner.IsUser || owner.IsRelationshipWithUser(DiplomaticRelationship.Alliance))) {
                noTgtDistanceValueText = "N/A";
            }

            if (target != null && fleetPosition.HasValue) {
                return Vector3.Distance(target.Position, fleetPosition.Value);
            }
            return null;
        }

        private string GetSpeedSettingText(Speed speedSetting, Player owner) {
            string text = Unknown;
            if (speedSetting != Speed.None) {
                text = speedSetting.GetValueName();
            }
            else {
                if (owner != null && (owner.IsUser || owner.IsRelationshipWithUser(DiplomaticRelationship.Alliance))) {
                    text = Speed.Stop.GetValueName();
                }
            }
            return text;
        }

        private float? CalcSpeedSettingValue(Speed speedSetting, float? fullSpeedValue, Player owner) {
            float? speedValue = null;
            if (speedSetting != Speed.None) {
                if (fullSpeedValue.HasValue) {
                    speedValue = speedSetting.GetUnitsPerHour(fullSpeedValue.Value);
                }
            }
            else {
                if (owner != null && (owner.IsUser || owner.IsRelationshipWithUser(DiplomaticRelationship.Alliance))) {
                    speedValue = Constants.ZeroF;
                }
            }
            return speedValue;
        }

    }
}

