// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCmdDisplayInfoFactory.cs
// Abstract generic base factory that makes instances of text containing info about UnitCommands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using Common.LocalResources;

    /// <summary>
    /// Abstract generic base factory that makes instances of text containing info about UnitCommands.
    /// </summary>
    public abstract class AUnitCmdDisplayInfoFactory<ReportType, FactoryType> : AMortalItemDisplayInfoFactory<ReportType, FactoryType>
        where ReportType : AUnitCmdReport
        where FactoryType : AUnitCmdDisplayInfoFactory<ReportType, FactoryType> {

        protected override bool TryMakeColorizedText(ItemInfoID infoID, ReportType report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case ItemInfoID.UnitName:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitName != null ? report.UnitName : Unknown);
                        break;
                    case ItemInfoID.CurrentCmdEffectiveness:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.CurrentCmdEffectiveness.HasValue ? GetFormat(infoID).Inject(report.CurrentCmdEffectiveness.Value) : Unknown);
                        break;
                    case ItemInfoID.Formation:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitFormation != Formation.None ? report.UnitFormation.GetValueName() : Unknown);
                        break;
                    case ItemInfoID.AlertStatus:
                        isSuccess = true;
                        colorizedText = GetColorizedAlertStatusText(report.AlertStatus);
                        break;
                    case ItemInfoID.UnitOffense:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitOffensiveStrength.HasValue ? report.UnitOffensiveStrength.Value.ToTextHud() : Unknown);
                        break;
                    case ItemInfoID.UnitDefense:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitDefensiveStrength.HasValue ? report.UnitDefensiveStrength.Value.ToTextHud() : Unknown);
                        break;
                    case ItemInfoID.UnitMaxHitPts:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitMaxHitPoints.HasValue ? GetFormat(infoID).Inject(report.UnitMaxHitPoints.Value) : Unknown);
                        //D.Log("{0}.TryMakeColorizedText({1}) called. Text output = {2}.", GetType().Name, infoID.GetValueName(), colorizedText.ToString());
                        break;
                    case ItemInfoID.UnitCurrentHitPts:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitCurrentHitPoints.HasValue ? GetFormat(infoID).Inject(report.UnitCurrentHitPoints.Value) : Unknown);
                        break;
                    case ItemInfoID.UnitHealth:
                        isSuccess = true;
                        colorizedText = GetColorizedHealthText(report.UnitHealth, report.UnitMaxHitPoints);
                        break;
                    case ItemInfoID.UnitWeaponsRange:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitWeaponsRange.HasValue ? report.UnitWeaponsRange.Value.ToString() : Unknown);
                        break;
                    case ItemInfoID.UnitSensorRange:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitSensorRange.HasValue ? report.UnitSensorRange.Value.ToString() : Unknown);
                        break;
                    case ItemInfoID.UnitScience:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitScience.HasValue ? GetFormat(infoID).Inject(report.UnitScience.Value) : Unknown);
                        break;
                    case ItemInfoID.UnitCulture:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitCulture.HasValue ? GetFormat(infoID).Inject(report.UnitCulture.Value) : Unknown);
                        break;
                    case ItemInfoID.UnitNetIncome:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitIncome.HasValue && report.UnitExpense.HasValue ? GetFormat(infoID).Inject(report.UnitIncome.Value - report.UnitExpense.Value) : Unknown);
                        break;
                    case ItemInfoID.Hero:
                        isSuccess = true;
                        colorizedText = GetColorizedHeroText(report.Hero);
                        break;
                }
            }
            return isSuccess;
        }

        private string GetColorizedAlertStatusText(AlertStatus alertStatus) {
            GameColor color = GameColor.White;
            string colorizedText = Constants.QuestionMark;
            if (alertStatus != AlertStatus.None) {
                switch (alertStatus) {
                    case AlertStatus.Normal:
                        color = GameColor.Green;
                        break;
                    case AlertStatus.Yellow:
                        color = GameColor.Yellow;
                        break;
                    case AlertStatus.Red:
                        color = GameColor.Red;
                        break;
                    case AlertStatus.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(alertStatus));
                }
                colorizedText = alertStatus.GetValueName();
            }
            colorizedText = colorizedText.SurroundWith(color);
            return _lineTemplate.Inject(colorizedText);
        }

        private string GetColorizedHeroText(Hero hero) {
            string colorizedHeroText = Unknown;
            if (hero != null) {
                colorizedHeroText = hero.Name;
            }
            string text = _lineTemplate.Inject(colorizedHeroText);
            return text;
        }

        protected string GetCurrentConstructionText(ConstructionInfo construction) {
            string constructionText = Unknown;
            if (construction != null) {
                if (construction == TempGameValues.NoConstruction) {
                    constructionText = "None";
                }
                else {
                    constructionText = construction.Name;
                }
            }
            string text = _lineTemplate.Inject(constructionText);
            return text;
        }



    }
}

