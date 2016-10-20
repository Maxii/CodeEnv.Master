// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItemDisplayInfoFactory.cs
// Abstract generic base factory that makes instances of text containing info about MortalItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract generic base factory that makes instances of text containing info about MortalItems.
    /// </summary>
    public abstract class AMortalItemDisplayInfoFactory<ReportType, FactoryType> : AIntelItemDisplayInfoFactory<ReportType, FactoryType>
        where ReportType : AMortalItemReport
        where FactoryType : AMortalItemDisplayInfoFactory<ReportType, FactoryType> {

        protected override bool TryMakeColorizedText(ItemInfoID infoID, ReportType report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case ItemInfoID.SectorID:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.SectorID.ToString());
                        break;
                    case ItemInfoID.MaxHitPoints:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.MaxHitPoints.HasValue ? GetFormat(infoID).Inject(report.MaxHitPoints.Value) : Unknown);
                        break;
                    case ItemInfoID.CurrentHitPoints:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.CurrentHitPoints.HasValue ? GetFormat(infoID).Inject(report.CurrentHitPoints.Value) : Unknown);
                        break;
                    case ItemInfoID.Health:
                        isSuccess = true;
                        colorizedText = GetColorizedHealthText(report.Health, report.MaxHitPoints);
                        break;
                    case ItemInfoID.Defense:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.DefensiveStrength.HasValue ? report.DefensiveStrength.Value.ToTextHud() : Unknown);
                        break;
                }
            }
            return isSuccess;
        }

        protected string GetColorizedHealthText(float? health, float? maxHp) {
            GameColor healthColor = GameColor.White;
            string colorizedHealthText = Constants.QuestionMark;
            if (health.HasValue) {
                healthColor = (health.Value > GeneralSettings.Instance.HealthThreshold_Damaged) ? GameColor.Green :
                            (health.Value > GeneralSettings.Instance.HealthThreshold_BadlyDamaged) ? GameColor.Yellow :
                            (health.Value > GeneralSettings.Instance.HealthThreshold_CriticallyDamaged) ? GameColor.Orange : GameColor.Red; ;
                colorizedHealthText = Constants.FormatPercent_0Dp.Inject(health.Value).SurroundWith(healthColor);
            }

            GameColor maxHpColor = GameColor.White;
            string colorizedMaxHpText = Constants.QuestionMark;
            if (maxHp.HasValue) {
                maxHpColor = GameColor.Green;
                colorizedMaxHpText = Constants.FormatFloat_1DpMax.Inject(maxHp.Value).SurroundWith(maxHpColor);
            }
            return _lineTemplate.Inject(colorizedHealthText, colorizedMaxHpText);
        }

    }
}

