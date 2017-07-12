// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitElementDisplayInfoFactory.cs
// Abstract generic base factory that makes instances of text containing info about ElementItems.
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
    /// Abstract generic base factory that makes instances of text containing info about ElementItems.
    /// </summary>
    public abstract class AUnitElementDisplayInfoFactory<ReportType, FactoryType> : AMortalItemDisplayInfoFactory<ReportType, FactoryType>
        where ReportType : AUnitElementReport
        where FactoryType : AUnitElementDisplayInfoFactory<ReportType, FactoryType> {

        protected override bool TryMakeColorizedText(ItemInfoID infoID, ReportType report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case ItemInfoID.UnitName:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitName != null ? report.UnitName : Unknown);
                        break;
                    case ItemInfoID.Offense:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.OffensiveStrength.HasValue ? report.OffensiveStrength.Value.ToTextHud() : Unknown);
                        break;
                    case ItemInfoID.WeaponsRange:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.WeaponsRange.HasValue ? report.WeaponsRange.Value.ToString() : Unknown);
                        break;
                    case ItemInfoID.AlertStatus:
                        isSuccess = true;
                        colorizedText = GetColorizedAlertStatusText(report.AlertStatus);
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

    }
}

