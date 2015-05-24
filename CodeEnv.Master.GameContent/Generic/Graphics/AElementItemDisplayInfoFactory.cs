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
        where ReportType : AElementItemReport
        where FactoryType : AElementItemDisplayInfoFactory<ReportType, FactoryType> {

        protected override bool TryMakeColorizedText(ContentID contentID, ReportType report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(contentID, report, out colorizedText);
            if (!isSuccess) {
                switch (contentID) {
                    case ContentID.ParentName:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.ParentName != null ? report.ParentName : _unknown);
                        break;
                    case ContentID.Offense:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.OffensiveStrength.HasValue ? report.OffensiveStrength.Value.ToString() : _unknown);
                        break;
                    case ContentID.MaxWeaponsRange:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.MaxWeaponsRange.HasValue ? GetFormat(contentID).Inject(report.MaxWeaponsRange.Value) : _unknown);
                        break;
                    case ContentID.MaxSensorRange:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.MaxSensorRange.HasValue ? GetFormat(contentID).Inject(report.MaxSensorRange.Value) : _unknown);
                        break;
                    case ContentID.Science:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Science.HasValue ? GetFormat(contentID).Inject(report.Science.Value) : _unknown);
                        break;
                    case ContentID.Culture:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Culture.HasValue ? GetFormat(contentID).Inject(report.Culture.Value) : _unknown);
                        break;
                    case ContentID.NetIncome:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Income.HasValue && report.Expense.HasValue ? GetFormat(contentID).Inject(report.Income.Value - report.Expense.Value) : _unknown);
                        break;
                }
            }
            return isSuccess;
        }

    }
}

