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

        protected override bool TryMakeColorizedText(AccessControlInfoID infoID, ReportType report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case AccessControlInfoID.ParentName:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.ParentName != null ? report.ParentName : _unknown);
                        break;
                    case AccessControlInfoID.Offense:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.OffensiveStrength.HasValue ? report.OffensiveStrength.Value.ToTextHud() : _unknown);
                        break;
                    case AccessControlInfoID.WeaponsRange:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.WeaponsRange.HasValue ? report.WeaponsRange.Value.ToString() : _unknown);
                        break;
                    case AccessControlInfoID.Science:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Science.HasValue ? GetFormat(infoID).Inject(report.Science.Value) : _unknown);
                        break;
                    case AccessControlInfoID.Culture:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Culture.HasValue ? GetFormat(infoID).Inject(report.Culture.Value) : _unknown);
                        break;
                    case AccessControlInfoID.NetIncome:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Income.HasValue && report.Expense.HasValue ? GetFormat(infoID).Inject(report.Income.Value - report.Expense.Value) : _unknown);
                        break;
                }
            }
            return isSuccess;
        }

    }
}

