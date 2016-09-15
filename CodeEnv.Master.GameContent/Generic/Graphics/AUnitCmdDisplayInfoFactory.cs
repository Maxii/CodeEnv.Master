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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

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
                    case ItemInfoID.ParentName:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.ParentName != null ? report.ParentName : Unknown);
                        break;
                    case ItemInfoID.CurrentCmdEffectiveness:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.CurrentCmdEffectiveness.HasValue ? GetFormat(infoID).Inject(report.CurrentCmdEffectiveness.Value) : Unknown);
                        break;
                    case ItemInfoID.Formation:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitFormation != Formation.None ? report.UnitFormation.GetValueName() : Unknown);
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
                }
            }
            return isSuccess;
        }
    }
}

