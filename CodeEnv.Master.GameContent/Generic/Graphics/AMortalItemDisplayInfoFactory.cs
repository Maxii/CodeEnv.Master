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

        protected override bool TryMakeColorizedText(ContentID contentID, ReportType report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(contentID, report, out colorizedText);
            if (!isSuccess) {
                switch (contentID) {
                    case ContentID.SectorIndex:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.SectorIndex.ToString());
                        break;
                    case ContentID.MaxHitPoints:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.MaxHitPoints.HasValue ? GetFormat(contentID).Inject(report.MaxHitPoints.Value) : _unknown);
                        break;
                    case ContentID.CurrentHitPoints:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.CurrentHitPoints.HasValue ? GetFormat(contentID).Inject(report.CurrentHitPoints.Value) : _unknown);
                        break;
                    case ContentID.Health:
                        isSuccess = true;
                        colorizedText = GetColorizedHealthText(report.Health, report.MaxHitPoints);
                        break;
                    case ContentID.Defense:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.DefensiveStrength.HasValue ? report.DefensiveStrength.Value.ToTextHud() : _unknown);
                        break;
                    case ContentID.Mass:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Mass.HasValue ? GetFormat(contentID).Inject(report.Mass.Value) : _unknown);
                        break;
                }
            }
            return isSuccess;
        }

        protected string GetColorizedHealthText(float? health, float? maxHp) {
            GameColor healthColor = GameColor.White;
            string colorizedHealthText = Constants.QuestionMark;
            if (health.HasValue) {
                healthColor = (health.Value > GeneralSettings.Instance.InjuredHealthThreshold) ? GameColor.Green :
                            (health.Value > GeneralSettings.Instance.CriticalHealthThreshold) ? GameColor.Yellow :
                                                                                                    GameColor.Red;
                colorizedHealthText = Constants.FormatPercent_0Dp.Inject(health.Value).SurroundWith(healthColor);
            }

            GameColor maxHpColor = GameColor.White;
            string colorizedMaxHpText = Constants.QuestionMark;
            if (maxHp.HasValue) {
                maxHpColor = GameColor.Green;
                colorizedMaxHpText = Constants.FormatFloat_1DpMax.Inject(maxHp.Value).SurroundWith(maxHpColor);
            }
            return _phrase.Inject(colorizedHealthText, colorizedMaxHpText);
        }

    }
}

