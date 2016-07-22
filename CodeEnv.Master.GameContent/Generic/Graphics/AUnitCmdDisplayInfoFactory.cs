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

        protected override bool TryMakeColorizedText(AccessControlInfoID infoID, ReportType report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case AccessControlInfoID.ParentName:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.ParentName != null ? report.ParentName : _unknown);
                        break;
                    case AccessControlInfoID.CurrentCmdEffectiveness:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.CurrentCmdEffectiveness.HasValue ? GetFormat(infoID).Inject(report.CurrentCmdEffectiveness.Value) : _unknown);
                        break;
                    case AccessControlInfoID.Formation:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitFormation != Formation.None ? report.UnitFormation.GetValueName() : _unknown);
                        break;
                    case AccessControlInfoID.UnitOffense:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitOffensiveStrength.HasValue ? report.UnitOffensiveStrength.Value.ToTextHud() : _unknown);
                        break;
                    case AccessControlInfoID.UnitDefense:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitDefensiveStrength.HasValue ? report.UnitDefensiveStrength.Value.ToTextHud() : _unknown);
                        break;
                    case AccessControlInfoID.UnitMaxHitPts:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitMaxHitPoints.HasValue ? GetFormat(infoID).Inject(report.UnitMaxHitPoints.Value) : _unknown);
                        break;
                    case AccessControlInfoID.UnitCurrentHitPts:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitCurrentHitPoints.HasValue ? GetFormat(infoID).Inject(report.UnitCurrentHitPoints.Value) : _unknown);
                        break;
                    case AccessControlInfoID.UnitHealth:
                        isSuccess = true;
                        colorizedText = GetColorizedHealthText(report.UnitHealth, report.MaxHitPoints);
                        break;
                    case AccessControlInfoID.UnitWeaponsRange:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitWeaponsRange.HasValue ? report.UnitWeaponsRange.Value.ToString() : _unknown);
                        break;
                    case AccessControlInfoID.UnitSensorRange:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitSensorRange.HasValue ? report.UnitSensorRange.Value.ToString() : _unknown);
                        break;
                    case AccessControlInfoID.UnitScience:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitScience.HasValue ? GetFormat(infoID).Inject(report.UnitScience.Value) : _unknown);
                        break;
                    case AccessControlInfoID.UnitCulture:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitCulture.HasValue ? GetFormat(infoID).Inject(report.UnitCulture.Value) : _unknown);
                        break;
                    case AccessControlInfoID.UnitNetIncome:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitIncome.HasValue && report.UnitExpense.HasValue ? GetFormat(infoID).Inject(report.UnitIncome.Value - report.UnitExpense.Value) : _unknown);
                        break;
                }
            }
            return isSuccess;
        }
    }
}

