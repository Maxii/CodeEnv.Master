// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipLabelTextFactory.cs
// LabelText factory for Ships.
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
    /// LabelText factory for Ships.
    /// </summary>
    [System.Obsolete]
    public class ShipLabelTextFactory : AIntelItemLabelTextFactory<ShipReport, ShipData> {

        private static IDictionary<DisplayTargetID, IList<LabelContentID>> _includedContentLookup = new Dictionary<DisplayTargetID, IList<LabelContentID>>() {
            {DisplayTargetID.CursorHud, new List<LabelContentID>() {
                LabelContentID.Name,
                LabelContentID.ParentName,
                LabelContentID.Owner,
                LabelContentID.Category,

                //LabelContentID.MaxHitPoints,
                //LabelContentID.UnitCurrentHitPts,
                LabelContentID.Health,
                LabelContentID.Defense,
                LabelContentID.Mass,
                LabelContentID.MaxWeaponsRange,
                LabelContentID.MaxSensorRange,
                LabelContentID.Offense,

                                LabelContentID.Science,
                LabelContentID.Culture,
                LabelContentID.NetIncome,


                LabelContentID.Target,
                LabelContentID.TargetDistance,
                LabelContentID.CombatStance,
                LabelContentID.CurrentSpeed,
                LabelContentID.FullSpeed,
                LabelContentID.MaxTurnRate,

                LabelContentID.CameraDistance,
                LabelContentID.IntelState
            }}
        };

#pragma warning disable 0649
        private static IDictionary<DisplayTargetID, IDictionary<LabelContentID, string>> _phraseOverrideLookup;
#pragma warning restore 0649


        public ShipLabelTextFactory() : base() { }

        public override bool TryMakeInstance(DisplayTargetID displayTgtID, LabelContentID contentID, ShipReport report, ShipData data, out IColoredTextList content) {
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
                    content = report.Category != ShipCategory.None ? new ColoredTextList_String(report.Category.GetName()) : content;
                    break;
                //case LabelContentID.MaxHitPoints:
                //    content = report.MaxHitPoints.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.MaxHitPoints.Value) : content;
                //    break;
                //case LabelContentID.CurrentHitPoints:
                //    content = report.CurrentHitPoints.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.CurrentHitPoints.Value) : content;
                //    break;
                case LabelContentID.Health:
                    content = new ColoredTextList_Health(report.Health, report.MaxHitPoints);
                    break;
                case LabelContentID.Defense:
                    content = report.DefensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(report.DefensiveStrength.Value) : content;
                    break;
                case LabelContentID.Mass:
                    content = report.Mass.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.Mass.Value) : content;
                    break;
                case LabelContentID.MaxWeaponsRange:
                    content = report.MaxWeaponsRange.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.MaxWeaponsRange.Value) : content;
                    break;
                case LabelContentID.MaxSensorRange:
                    content = report.MaxSensorRange.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.MaxSensorRange.Value) : content;
                    break;
                case LabelContentID.Offense:
                    content = report.OffensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(report.OffensiveStrength.Value) : content;
                    break;
                case LabelContentID.Target:
                    content = report.Target != null ? new ColoredTextList_String(report.Target.DisplayName) : content;
                    break;
                case LabelContentID.TargetDistance:
                    content = report.Target != null ? new ColoredTextList_Distance(data.Position, report.Target.Position) : content;
                    break;
                case LabelContentID.CombatStance:
                    content = report.CombatStance != ShipCombatStance.None ? new ColoredTextList_String(report.CombatStance.GetName()) : content;
                    break;
                case LabelContentID.CurrentSpeed:
                    content = report.CurrentSpeed.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.CurrentSpeed.Value) : content;
                    break;
                case LabelContentID.FullSpeed:
                    content = report.FullSpeed.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.FullSpeed.Value) : content;
                    break;
                case LabelContentID.MaxTurnRate:
                    content = report.MaxTurnRate.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.MaxTurnRate.Value) : content;
                    break;
                case LabelContentID.Science:
                    content = report.Science.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.Science.Value) : content;
                    break;
                case LabelContentID.Culture:
                    content = report.Culture.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.Culture.Value) : content;
                    break;
                case LabelContentID.NetIncome:
                    content = report.Income.HasValue && report.Expense.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.Income.Value - report.Expense.Value) : content;
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

