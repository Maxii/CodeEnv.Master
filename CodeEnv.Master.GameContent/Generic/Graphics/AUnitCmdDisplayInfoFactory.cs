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
        where ReportType : ACmdReport
        where FactoryType : AUnitCmdDisplayInfoFactory<ReportType, FactoryType> {

        protected override bool TryMakeColorizedText(ContentID contentID, ReportType report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(contentID, report, out colorizedText);
            if (!isSuccess) {
                switch (contentID) {
                    case ContentID.ParentName:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.ParentName != null ? report.ParentName : _unknown);
                        break;
                    case ContentID.CurrentCmdEffectiveness:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.CurrentCmdEffectiveness.HasValue ? GetFormat(contentID).Inject(report.CurrentCmdEffectiveness.Value) : _unknown);
                        break;
                    case ContentID.Formation:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitFormation != Formation.None ? report.UnitFormation.GetName() : _unknown);
                        break;
                    case ContentID.UnitOffense:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitOffensiveStrength.HasValue ? report.UnitOffensiveStrength.Value.ToString() : _unknown);
                        break;
                    case ContentID.UnitDefense:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitDefensiveStrength.HasValue ? report.UnitDefensiveStrength.Value.ToString() : _unknown);
                        break;
                    case ContentID.UnitMaxHitPts:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitMaxHitPoints.HasValue ? GetFormat(contentID).Inject(report.UnitMaxHitPoints.Value) : _unknown);
                        break;
                    case ContentID.UnitCurrentHitPts:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitCurrentHitPoints.HasValue ? GetFormat(contentID).Inject(report.UnitCurrentHitPoints.Value) : _unknown);
                        break;
                    case ContentID.UnitHealth:
                        isSuccess = true;
                        colorizedText = GetColorizedHealthText(report.UnitHealth, report.MaxHitPoints);
                        break;
                    case ContentID.UnitMaxWeaponsRange:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitMaxWeaponsRange.HasValue ? GetFormat(contentID).Inject(report.UnitMaxWeaponsRange.Value) : _unknown);
                        break;
                    case ContentID.UnitMaxSensorRange:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitMaxSensorRange.HasValue ? GetFormat(contentID).Inject(report.UnitMaxSensorRange.Value) : _unknown);
                        break;
                    case ContentID.UnitScience:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitScience.HasValue ? GetFormat(contentID).Inject(report.UnitScience.Value) : _unknown);
                        break;
                    case ContentID.UnitCulture:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitCulture.HasValue ? GetFormat(contentID).Inject(report.UnitCulture.Value) : _unknown);
                        break;
                    case ContentID.UnitNetIncome:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitIncome.HasValue && report.UnitExpense.HasValue ? GetFormat(contentID).Inject(report.UnitIncome.Value - report.UnitExpense.Value) : _unknown);
                        break;
                }
            }
            return isSuccess;
        }
    }
}

