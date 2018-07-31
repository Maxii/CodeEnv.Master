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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Factory that makes instances of text containing info about Ships.
    /// </summary>
    public class ShipDisplayInfoFactory : AUnitElementDisplayInfoFactory<ShipReport, ShipDisplayInfoFactory> {

        private static ItemInfoID[] _infoIDsToDisplay = new ItemInfoID[] {
            ItemInfoID.Name,
            ItemInfoID.UnitName,
            ItemInfoID.Owner,
            ItemInfoID.Category,
            ItemInfoID.Health,
            //ItemInfoID.Defense,
            //ItemInfoID.Offense,
            //ItemInfoID.WeaponsRange,
            ItemInfoID.AlertStatus,
            //ItemInfoID.OrderDirective,

            ItemInfoID.Outputs,
            //ItemInfoID.Mass,
            ItemInfoID.ConstructionCost,

            ItemInfoID.Target,
            ItemInfoID.TargetDistance,
            ItemInfoID.CombatStance,
            ItemInfoID.CurrentSpeedSetting,
            ItemInfoID.FullSpeed,
            //ItemInfoID.MaxTurnRate,

            ItemInfoID.Separator,

            ItemInfoID.IntelState,

            ItemInfoID.Separator,

            ItemInfoID.ActualSpeed,
            ItemInfoID.CameraDistance
        };

        protected override ItemInfoID[] OrderedInfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private ShipDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(ItemInfoID infoID, ShipReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case ItemInfoID.SectorID:
                        isSuccess = true;
                        string sectorIdMsg = report.SectorID != default(IntVector3) ? report.SectorID.ToString() : "None";
                        colorizedText = _lineTemplate.Inject(sectorIdMsg);
                        break;
                    case ItemInfoID.Category:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Category != ShipHullCategory.None ? report.Category.GetValueName() : Unknown);
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
                        float? speedSettingValue = CalcSpeedSettingValue(report.CurrentSpeedSetting, report.FullSpeed, report.Owner);
                        string speedSettingText = GetSpeedSettingText(report.CurrentSpeedSetting, report.Owner);
                        colorizedText = _lineTemplate.Inject(speedSettingText, speedSettingValue.HasValue ? GetFormat(infoID).Inject(speedSettingValue.Value) : Unknown);
                        break;
                    case ItemInfoID.FullSpeed:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.FullSpeed.HasValue ? GetFormat(infoID).Inject(report.FullSpeed.Value) : Unknown);
                        break;
                    case ItemInfoID.TurnRate:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.TurnRate.HasValue ? GetFormat(infoID).Inject(report.TurnRate.Value) : Unknown);
                        break;
                    case ItemInfoID.CombatStance:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.CombatStance != ShipCombatStance.None ? report.CombatStance.GetValueName() : Unknown);
                        break;
                    case ItemInfoID.ActualSpeed:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(GetFormat(infoID).Inject(report.__ActualSpeedValue.Value));
                        break;
                    case ItemInfoID.OrderDirective:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.__OrderDirective.GetValueName());
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

