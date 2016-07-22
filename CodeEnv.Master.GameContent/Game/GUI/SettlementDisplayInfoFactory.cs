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
    public class SettlementDisplayInfoFactory : AUnitCmdDisplayInfoFactory<SettlementCmdReport, SettlementDisplayInfoFactory> {

        private static AccessControlInfoID[] _infoIDsToDisplay = new AccessControlInfoID[] {
            AccessControlInfoID.Name,
            AccessControlInfoID.ParentName,
            AccessControlInfoID.Category,
            AccessControlInfoID.Composition,
            AccessControlInfoID.Owner,

            AccessControlInfoID.CurrentCmdEffectiveness,
            AccessControlInfoID.Formation,
            AccessControlInfoID.UnitOffense,
            AccessControlInfoID.UnitDefense,
            AccessControlInfoID.UnitHealth,
            AccessControlInfoID.UnitWeaponsRange,
            AccessControlInfoID.UnitSensorRange,
            AccessControlInfoID.UnitScience,
            AccessControlInfoID.UnitCulture,
            AccessControlInfoID.UnitNetIncome,

            AccessControlInfoID.Capacity,
            AccessControlInfoID.Resources,
            AccessControlInfoID.Population,
            AccessControlInfoID.Approval,

            AccessControlInfoID.CameraDistance,
            AccessControlInfoID.IntelState
        };

        protected override AccessControlInfoID[] InfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private SettlementDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(AccessControlInfoID infoID, SettlementCmdReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case AccessControlInfoID.Category:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Category != SettlementCategory.None ? report.Category.GetValueName() : _unknown);
                        break;
                    case AccessControlInfoID.Composition:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitComposition != null ? report.UnitComposition.ToString() : _unknown);
                        break;
                    case AccessControlInfoID.Capacity:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Capacity.HasValue ? GetFormat(infoID).Inject(report.Capacity.Value) : _unknown);
                        break;
                    case AccessControlInfoID.Resources:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Resources.HasValue ? report.Resources.Value.ToString() : _unknown);
                        break;
                    case AccessControlInfoID.Population:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Population.HasValue ? GetFormat(infoID).Inject(report.Population.Value) : _unknown);
                        break;
                    case AccessControlInfoID.Approval:
                        isSuccess = true;
                        colorizedText = GetColorizedApprovalText(report.Approval);
                        break;
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(infoID));
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

