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
            ////ItemInfoID.SensorRange,  // makes no sense
            //ItemInfoID.Science,
            //ItemInfoID.Culture,
            //ItemInfoID.NetIncome,
            //ItemInfoID.Production,
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
                    case ItemInfoID.Category:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Category != ShipHullCategory.None ? report.Category.GetValueName() : Unknown);
                        break;
                    case ItemInfoID.Target:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Target != null ? report.Target.DebugName : Unknown);
                        break;
                    case ItemInfoID.TargetDistance:
                        isSuccess = true;
                        float? targetDistance = CalcTargetDistance(report.Target, report.Position);
                        colorizedText = _lineTemplate.Inject(targetDistance.HasValue ? GetFormat(infoID).Inject(targetDistance.Value) : Unknown);
                        break;
                    case ItemInfoID.CurrentSpeedSetting:
                        isSuccess = true;
                        float? speedSettingValue = CalcSpeedSettingValue(report.CurrentSpeedSetting, report.FullSpeed);
                        string speedSettingText = report.CurrentSpeedSetting != Speed.None ? report.CurrentSpeedSetting.GetValueName() : Unknown;
                        colorizedText = _lineTemplate.Inject(speedSettingText, speedSettingValue.HasValue ? GetFormat(infoID).Inject(speedSettingValue.Value) : Unknown);
                        break;
                    case ItemInfoID.FullSpeed:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.FullSpeed.HasValue ? GetFormat(infoID).Inject(report.FullSpeed.Value) : Unknown);
                        break;
                    case ItemInfoID.MaxTurnRate:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.MaxTurnRate.HasValue ? GetFormat(infoID).Inject(report.MaxTurnRate.Value) : Unknown);
                        break;
                    case ItemInfoID.CombatStance:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.CombatStance != ShipCombatStance.None ? report.CombatStance.GetValueName() : Unknown);
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

        private float? CalcTargetDistance(INavigableDestination target, Vector3? shipPosition) {
            if (target != null) {
                IMortalItem_Ltd mortalTgt = target as IMortalItem_Ltd;
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


    }
}

