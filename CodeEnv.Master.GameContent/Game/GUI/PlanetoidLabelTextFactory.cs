// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidLabelTextFactory.cs
//  LabelText factory for Planetoids.
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
    /// LabelText factory for Planetoids.
    /// </summary>
    public class PlanetoidLabelTextFactory : ALabelTextFactory<PlanetoidReport, APlanetoidData> {


        private static IDictionary<LabelID, IDictionary<LabelContentID, string>> _formatLookupByLabelID = new Dictionary<LabelID, IDictionary<LabelContentID, string>>() {
            { LabelID.CursorHud, new Dictionary<LabelContentID, string>() {
                {LabelContentID.IntelCoverage, "IntelCoverage: {0}"},
                {LabelContentID.Name, "Name: {0}"},
                {LabelContentID.ParentName, "ParentName: {0}"},
                {LabelContentID.Owner, "Owner: {0}"},
                {LabelContentID.Category, "Category: {0}"},
                {LabelContentID.Capacity, "Capacity: {0}"},
                {LabelContentID.Resources, "Resources: {0}"},
                {LabelContentID.Specials, "[800080]Specials:[-] {0}"},
                {LabelContentID.MaxHitPoints, "MaxHitPts: {0}"},
                {LabelContentID.CurrentHitPoints, "CurrentHitPts: {0}"},
                {LabelContentID.Health, "Health: {0}"},
                {LabelContentID.Defense, "Defense: {0}"},
                {LabelContentID.Mass, "Mass: {0}"},

                {LabelContentID.CameraDistance, "CameraDistance: {0}"},
                {LabelContentID.IntelState, "< {0} >"}
            }}
            // TODO more LabelIDs
        };

        public PlanetoidLabelTextFactory() : base() { }

        protected override bool TryMakeInstance(LabelID labelID, LabelContentID contentID, PlanetoidReport report, APlanetoidData data, out IColoredTextList content) {
            content = _includeUnknownLookup[labelID] ? _unknownValue : _emptyValue;
            switch (contentID) {
                case LabelContentID.IntelCoverage:
                    content = new ColoredTextList_String(report.IntelCoverage.GetName());
                    break;
                case LabelContentID.Name:
                    content = !report.Name.IsNullOrEmpty() ? new ColoredTextList_String(report.Name) : content;
                    break;
                case LabelContentID.ParentName:
                    content = !report.ParentName.IsNullOrEmpty() ? new ColoredTextList_String(report.ParentName) : content;
                    break;
                case LabelContentID.Owner:
                    content = report.Owner != null ? new ColoredTextList_String(report.Owner.LeaderName) : content;
                    break;
                case LabelContentID.Category:
                    content = report.Category != PlanetoidCategory.None ? new ColoredTextList<PlanetoidCategory>(report.Category) : content;
                    break;
                case LabelContentID.Capacity:
                    content = report.Capacity.HasValue ? new ColoredTextList<int>(report.Capacity.Value) : content;
                    break;
                case LabelContentID.Resources:
                    content = report.Resources.HasValue ? new ColoredTextList<OpeYield>(report.Resources.Value) : content;
                    break;
                case LabelContentID.Specials:
                    content = report.SpecialResources.HasValue ? new ColoredTextList<XYield>(report.SpecialResources.Value) : content;
                    break;
                case LabelContentID.MaxHitPoints:
                    content = report.MaxHitPoints.HasValue ? new ColoredTextList<float>(report.MaxHitPoints.Value) : content;
                    break;
                case LabelContentID.CurrentHitPoints:
                    content = report.CurrentHitPoints.HasValue ? new ColoredTextList<float>(report.CurrentHitPoints.Value) : content;
                    break;
                case LabelContentID.Health:
                    content = report.Health.HasValue ? new ColoredTextList<float>(report.Health.Value) : content;
                    break;
                case LabelContentID.Defense:
                    content = report.DefensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(report.DefensiveStrength.Value) : content;
                    break;
                case LabelContentID.Mass:
                    content = report.Mass.HasValue ? new ColoredTextList<float>(report.Mass.Value) : content;
                    break;

                case LabelContentID.CameraDistance:
                case LabelContentID.IntelState:
                    content = MakeInstance(labelID, contentID, data);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(contentID));
            }
            return content != _emptyValue;
        }

        protected override IDictionary<LabelContentID, string> GetFormatLookup(LabelID labelID) {
            return _formatLookupByLabelID[labelID];
        }
        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

