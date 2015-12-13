// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementLabelTextFactory.cs
// LabelText factory for Settlements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// LabelText factory for Settlements.
    /// </summary>
    [System.Obsolete]
    public class SettlementLabelTextFactory : AIntelItemLabelTextFactory<SettlementReport, SettlementCmdData> {

        private static IDictionary<DisplayTargetID, IList<LabelContentID>> _includedContentLookup = new Dictionary<DisplayTargetID, IList<LabelContentID>>() {
            {DisplayTargetID.CursorHud, new List<LabelContentID>() {
                LabelContentID.Name,
                LabelContentID.ParentName,
                LabelContentID.Owner,
                LabelContentID.Category,

                LabelContentID.Composition,
                LabelContentID.Formation,
                LabelContentID.CurrentCmdEffectiveness,
                LabelContentID.UnitMaxWeaponsRange,
                LabelContentID.UnitMaxSensorRange,
                LabelContentID.UnitOffense,
                LabelContentID.UnitDefense,
                LabelContentID.UnitHealth,

                LabelContentID.Population,
                LabelContentID.Capacity,
                LabelContentID.Resources,
                LabelContentID.UnitScience,
                LabelContentID.UnitCulture,
                LabelContentID.UnitNetIncome,
                LabelContentID.Approval,

                LabelContentID.CameraDistance,
                LabelContentID.IntelState
            }},
            //{DisplayTargetID.BasesScreen, new List<LabelContentID>() {
            //    LabelContentID.Name,
            //    LabelContentID.ParentName,
            //    LabelContentID.Owner,
            //    LabelContentID.Category,

            //    LabelContentID.Composition,
            //    LabelContentID.Formation,
            //    LabelContentID.CurrentCmdEffectiveness,
            //    LabelContentID.UnitMaxWeaponsRange,
            //    LabelContentID.UnitMaxSensorRange,
            //    LabelContentID.UnitOffense,
            //    LabelContentID.UnitDefense,
            //    //LabelContentID.UnitMaxHitPts,
            //    //LabelContentID.UnitCurrentHitPts,
            //    LabelContentID.UnitHealth,

            //    LabelContentID.Population,
            //    //LabelContentID.CapacityUsed,
            //    //LabelContentID.ResourcesUsed,
            //    //LabelContentID.SpecialsUsed,
            //}}
        };

        //#pragma warning disable 0649
        private static IDictionary<DisplayTargetID, IDictionary<LabelContentID, string>> _phraseOverrideLookup = new Dictionary<DisplayTargetID, IDictionary<LabelContentID, string>>() {
            //{DisplayTargetID.BasesScreen, new Dictionary<LabelContentID, string>() {
            //    {LabelContentID.ParentName, "{0}"},
            //    {LabelContentID.Composition, "{0}"}
            //}}
        };
        //#pragma warning restore 0649

        public SettlementLabelTextFactory() : base() { }

        public override bool TryMakeInstance(DisplayTargetID displayTgtID, LabelContentID contentID, SettlementReport report, SettlementCmdData data, out IColoredTextList content) {
            content = _includeUnknownLookup[displayTgtID] ? _unknownContent : _emptyContent;
            switch (contentID) {
                case LabelContentID.Name:
                    content = !report.Name.IsNullOrEmpty() ? new ColoredTextList_String(report.Name) : content;
                    break;
                case LabelContentID.ParentName:
                    content = !report.ParentName.IsNullOrEmpty() ? new ColoredTextList_String(report.ParentName) : content;
                    break;
                case LabelContentID.Owner:
                    content = report.Owner != null ? new ColoredTextList_Owner(report.Owner) : content;
                    break;
                case LabelContentID.Category:
                    content = report.Category != SettlementCategory.None ? new ColoredTextList_String(report.Category.GetValueName()) : content;
                    break;
                case LabelContentID.Composition:
                    content = report.UnitComposition != null ? new ColoredTextList_String(report.UnitComposition.ToString()) : content;
                    break;
                case LabelContentID.Formation:
                    content = report.UnitFormation != Formation.None ? new ColoredTextList_String(report.UnitFormation.GetValueName()) : content;
                    break;
                case LabelContentID.CurrentCmdEffectiveness:
                    content = report.CurrentCmdEffectiveness.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.CurrentCmdEffectiveness.Value) : content;
                    break;
                case LabelContentID.UnitMaxWeaponsRange:
                    content = report.UnitMaxWeaponsRange.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.UnitMaxWeaponsRange.Value) : content;
                    break;
                case LabelContentID.UnitMaxSensorRange:
                    content = report.UnitMaxSensorRange.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.UnitMaxSensorRange.Value) : content;
                    break;
                case LabelContentID.UnitOffense:
                    content = report.UnitOffensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(report.UnitOffensiveStrength.Value) : content;
                    break;
                case LabelContentID.UnitDefense:
                    content = report.UnitDefensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(report.UnitDefensiveStrength.Value) : content;
                    break;
                case LabelContentID.UnitScience:
                    content = report.UnitScience.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.UnitScience.Value) : content;
                    break;
                case LabelContentID.UnitCulture:
                    content = report.UnitCulture.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.UnitCulture.Value) : content;
                    break;
                case LabelContentID.UnitNetIncome:
                    content = report.UnitIncome.HasValue && report.UnitExpense.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.UnitIncome.Value - report.UnitExpense.Value) : content;
                    break;

                case LabelContentID.Approval:
                    content = new ColoredTexList_Approval(report.Approval);
                    break;
                case LabelContentID.UnitHealth:
                    content = new ColoredTextList_Health(report.UnitHealth, report.UnitMaxHitPoints);
                    break;
                case LabelContentID.Population:
                    content = report.Population.HasValue ? new ColoredTextList<int>(GetFormat(contentID), report.Population.Value) : content;
                    break;
                case LabelContentID.Capacity:
                    content = report.Capacity.HasValue ? new ColoredTextList<int>(GetFormat(contentID), report.Capacity.Value) : content;
                    break;
                case LabelContentID.Resources:
                    content = report.Resources.HasValue ? new ColoredTextList<ResourceYield>(report.Resources.Value) : content;
                    break;

                case LabelContentID.CameraDistance:
                    content = new ColoredTextList_Distance(data.Position);
                    break;
                case LabelContentID.IntelState:
                    content = new ColoredTextList_Intel(data.GetUserIntelCopy());
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(contentID));
            }
            return content != _emptyContent;
        }

        protected override IEnumerable<LabelContentID> GetIncludedContentIDs(DisplayTargetID displayTgtID) {
            return _includedContentLookup[displayTgtID];
        }

        protected override bool TryGetOverridePhrase(DisplayTargetID displayTgtID, LabelContentID contentID, out string overridePhrase) {
            if (_phraseOverrideLookup != null && _phraseOverrideLookup.Keys.Contains(displayTgtID)) {
                return _phraseOverrideLookup[displayTgtID].TryGetValue(contentID, out overridePhrase);
            }
            overridePhrase = null;
            return false;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

