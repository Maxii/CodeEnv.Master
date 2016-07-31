// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AElementItemDisplayInfoFactory.cs
// Abstract generic base factory that makes instances of text containing info about ElementItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract generic base factory that makes instances of text containing info about ElementItems.
    /// </summary>
    public abstract class AElementItemDisplayInfoFactory<ReportType, FactoryType> : AMortalItemDisplayInfoFactory<ReportType, FactoryType>
        where ReportType : AUnitElementReport
        where FactoryType : AElementItemDisplayInfoFactory<ReportType, FactoryType> {

        protected override bool TryMakeColorizedText(ItemInfoID infoID, ReportType report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case ItemInfoID.ParentName:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.ParentName != null ? report.ParentName : Unknown);
                        break;
                    case ItemInfoID.Offense:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.OffensiveStrength.HasValue ? report.OffensiveStrength.Value.ToTextHud() : Unknown);
                        break;
                    case ItemInfoID.WeaponsRange:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.WeaponsRange.HasValue ? report.WeaponsRange.Value.ToString() : Unknown);
                        break;
                    case ItemInfoID.Science:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Science.HasValue ? GetFormat(infoID).Inject(report.Science.Value) : Unknown);
                        break;
                    case ItemInfoID.Culture:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Culture.HasValue ? GetFormat(infoID).Inject(report.Culture.Value) : Unknown);
                        break;
                    case ItemInfoID.NetIncome:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Income.HasValue && report.Expense.HasValue ? GetFormat(infoID).Inject(report.Income.Value - report.Expense.Value) : Unknown);
                        break;
                    case ItemInfoID.Mass:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Mass.HasValue ? GetFormat(infoID).Inject(report.Mass.Value) : Unknown);
                        break;
                }
            }
            return isSuccess;
        }

    }
}

