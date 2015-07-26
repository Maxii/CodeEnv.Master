// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementDisplayInfoFactory.cs
// Factory that makes instances of text containing info about Settlements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Factory that makes instances of text containing info about Settlements.
    /// </summary>
    public class SettlementDisplayInfoFactory : AUnitCmdDisplayInfoFactory<SettlementReport, SettlementDisplayInfoFactory> {

        private static ContentID[] _contentIDsToDisplay = new ContentID[] {                         
            ContentID.Name,
            ContentID.ParentName,
            ContentID.Category,
            ContentID.Composition,
            ContentID.Owner,

            ContentID.CurrentCmdEffectiveness,
            ContentID.Formation,
            ContentID.UnitOffense,
            ContentID.UnitDefense,
            ContentID.UnitHealth,
            ContentID.UnitWeaponsRange,
            ContentID.UnitSensorRange,
            ContentID.UnitScience,
            ContentID.UnitCulture,
            ContentID.UnitNetIncome,

            ContentID.Capacity,
            ContentID.Resources,
            ContentID.Population,
            ContentID.Approval,

            ContentID.CameraDistance,
            ContentID.IntelState
        };

        protected override ContentID[] ContentIDsToDisplay { get { return _contentIDsToDisplay; } }

        private SettlementDisplayInfoFactory() {
            Initialize();
        }

        protected override void Initialize() { }

        protected override bool TryMakeColorizedText(ContentID contentID, SettlementReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(contentID, report, out colorizedText);
            if (!isSuccess) {
                switch (contentID) {
                    case ContentID.Category:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Category != SettlementCategory.None ? report.Category.GetValueName() : _unknown);
                        break;
                    case ContentID.Composition:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitComposition != null ? report.UnitComposition.ToString() : _unknown);
                        break;
                    case ContentID.Capacity:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Capacity.HasValue ? GetFormat(contentID).Inject(report.Capacity.Value) : _unknown);
                        break;
                    case ContentID.Resources:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Resources.HasValue ? report.Resources.Value.ToString() : _unknown);
                        break;
                    case ContentID.Population:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Population.HasValue ? GetFormat(contentID).Inject(report.Population.Value) : _unknown);
                        break;
                    case ContentID.Approval:
                        isSuccess = true;
                        colorizedText = GetColorizedApprovalText(report.Approval);
                        break;
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(contentID));
                }
            }
            return isSuccess;
        }

        private string GetColorizedApprovalText(float? approval) {
            GameColor approvalColor = GameColor.White;
            string colorizedApprovalText = Constants.QuestionMark;
            if (approval.HasValue) {
                approvalColor = (approval.Value > GeneralSettings.Instance.ContentApprovalThreshold) ? GameColor.Green :
                    (approval.Value > GeneralSettings.Instance.UnhappyApprovalThreshold) ? GameColor.White :
                    (approval.Value > GeneralSettings.Instance.RevoltApprovalThreshold) ? GameColor.Yellow :
                                                                                          GameColor.Red;
                colorizedApprovalText = Constants.FormatPercent_0Dp.Inject(approval.Value);
            }
            return _phrase.Inject(colorizedApprovalText);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

