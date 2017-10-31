// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemDisplayInfoFactory.cs
// Abstract generic base factory that makes instances of text containing info about Items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Abstract generic base factory that makes instances of text containing info about Items.
    /// Currently used to generate ColoredStringBuilders for use by the Tooltip when used as a HUD.
    /// </summary>
    public abstract class AItemDisplayInfoFactory<ReportType, FactoryType> : AGenericSingleton<FactoryType>
        where ReportType : AItemReport
        where FactoryType : class {

        protected const string Unknown = Constants.QuestionMark;

        private static IDictionary<ItemInfoID, string> _defaultLineTemplateLookup =
            new Dictionary<ItemInfoID, string>(ItemInfoIDEqualityComparer.Default) {

                {ItemInfoID.Separator, "--------------------"},

                {ItemInfoID.Name, "Name: {0}"},
                {ItemInfoID.UnitName, "Unit: {0}"},
                {ItemInfoID.Owner, "Owner: {0}, UserRelations: {1}"},
                {ItemInfoID.Category, "Category: {0}"},
                {ItemInfoID.SectorID, "Sector: {0}"},
                {ItemInfoID.Position, "Position: {0}"},
                {ItemInfoID.AlertStatus, "AlertStatus: {0}"},
                {ItemInfoID.Hero, "Hero: {0}"},

                {ItemInfoID.Composition, CommonTerms.Composition + ": {0}"},
                {ItemInfoID.Formation, "Formation: {0}"},
                {ItemInfoID.CurrentCmdEffectiveness, "CmdEffect: {0}"},
                {ItemInfoID.UnitWeaponsRange, "UnitWeaponsRange: {0}"},
                {ItemInfoID.UnitSensorRange, "UnitSensorRange: {0}"},
                {ItemInfoID.UnitOffense, "UnitOffense: {0}"},
                {ItemInfoID.UnitDefense, "UnitDefense: {0}"},
                {ItemInfoID.UnitCurrentHitPts, "UnitCurrentHitPts: {0}"},
                {ItemInfoID.UnitMaxHitPts, "UnitMaxHitPts: {0}"},
                {ItemInfoID.UnitHealth, "UnitHealth: {0}, UnitMaxHP: {1}"},
                {ItemInfoID.UnitFullSpeed, "UnitFullSpeed: {0}"},
                {ItemInfoID.UnitMaxTurnRate, "UnitMaxTurnRate:  {0}"},

                {ItemInfoID.Target, "Target: {0}"},
                {ItemInfoID.TargetDistance, "TgtDistance: {0}"},
                {ItemInfoID.CurrentSpeedSetting, "Speed: {0}({1})"}, // TwoThirds(intendedValue), TwoThirds(?), ?(intendedValue), ?(?)
                {ItemInfoID.FullSpeed, "FullSpeed: {0}"},
                {ItemInfoID.MaxTurnRate, "MaxTurnRate: {0}"},
                {ItemInfoID.CombatStance, "CombatStance: {0}"},

                {ItemInfoID.Population, "Population: {0}"},

                {ItemInfoID.CurrentHitPoints, "CurrentHitPts: {0}"},
                {ItemInfoID.MaxHitPoints, "MaxHitPts: {0}"},
                {ItemInfoID.Health, "Health: {0}, MaxHP: {1}"},
                {ItemInfoID.Defense, "Defense: {0}"},
                {ItemInfoID.Offense, "Offense: {0}"},
                {ItemInfoID.Mass, "Mass: {0}"},
                {ItemInfoID.WeaponsRange, "WeaponsRange: {0}"},
                {ItemInfoID.SensorRange, "SensorRange: {0}"},

                {ItemInfoID.Outputs, "Outputs: {0}"},
                {ItemInfoID.UnitOutputs, "UnitOutputs: {0}"},

                {ItemInfoID.Approval, "Approval: {0}"},

                {ItemInfoID.Capacity, "Capacity: {0}"},

                {ItemInfoID.Resources, "[800080]Resources:[-] {0}"},

                {ItemInfoID.OrbitalSpeed, "RelativeOrbitSpeed: {0}"},
                {ItemInfoID.ConstructionCost, "ConstructionCost: {0}"},
                {ItemInfoID.CurrentConstruction, "CurrentConstruction: {0}"},

                {ItemInfoID.IntelState, "< {0} >"},

                {ItemInfoID.ActualSpeed, "ActualSpeed: {0}"},
                {ItemInfoID.CameraDistance, "CameraDistance: {0}"}
        };

        private static IDictionary<ItemInfoID, string> _defaultNumberFormatLookup =
            new Dictionary<ItemInfoID, string>(ItemInfoIDEqualityComparer.Default) {

                {ItemInfoID.CurrentCmdEffectiveness, Constants.FormatFloat_2Dp},
                {ItemInfoID.UnitCurrentHitPts, Constants.FormatFloat_0Dp},
                {ItemInfoID.UnitMaxHitPts, Constants.FormatFloat_0Dp},
                {ItemInfoID.UnitFullSpeed, Constants.FormatFloat_2Dp},
                {ItemInfoID.UnitMaxTurnRate, Constants.FormatFloat_0Dp},

                {ItemInfoID.TargetDistance, Constants.FormatFloat_0Dp},
                {ItemInfoID.CurrentSpeedSetting, Constants.FormatFloat_1Dp},
                {ItemInfoID.FullSpeed, Constants.FormatFloat_1Dp},
                {ItemInfoID.MaxTurnRate, Constants.FormatFloat_0Dp},

                {ItemInfoID.Population, Constants.FormatInt_1DMin},
                {ItemInfoID.Capacity, Constants.FormatInt_1DMin},

                {ItemInfoID.ConstructionCost, Constants.FormatFloat_0Dp},

                {ItemInfoID.CurrentHitPoints, Constants.FormatFloat_0Dp},
                {ItemInfoID.MaxHitPoints, Constants.FormatFloat_0Dp},
                {ItemInfoID.Mass, Constants.FormatInt_1DMin},

                {ItemInfoID.ActualSpeed, Constants.FormatFloat_2Dp},
                {ItemInfoID.OrbitalSpeed, Constants.FormatFloat_3Dp},
                {ItemInfoID.CameraDistance, Constants.FormatFloat_1Dp}
        };

        /// <summary>
        /// Gets the numerical format for this infoID.
        /// </summary>
        /// <param name="infoID">The info identifier.</param>
        /// <returns></returns>
        protected static string GetFormat(ItemInfoID infoID) {
            string valueFormat;
            if (!_defaultNumberFormatLookup.TryGetValue(infoID, out valueFormat)) {
                D.Warn("{0} reports no default numerical format for {1} found.", Instance.GetType().Name, infoID.GetValueName());
                valueFormat = "{0}";
            }
            return valueFormat;
        }

        protected static string GetLineTemplate(ItemInfoID infoID) {
            string lineTemplate;
            if (!_defaultLineTemplateLookup.TryGetValue(infoID, out lineTemplate)) {
                D.Warn("{0} reports no line template for {1} found.", Instance.GetType().Name, infoID.GetValueName());
                lineTemplate = "DefaultLineTemplate {0}";
            }
            return lineTemplate;
        }

        /// <summary>
        /// The IDs of the info to display, in the order they should be displayed.
        /// </summary>
        protected abstract ItemInfoID[] OrderedInfoIDsToDisplay { get; }

        /// <summary>
        /// The injectable line template to use when constructing each ColorizedText string. 
        /// </summary>
        protected string _lineTemplate;

        /// <summary>
        /// Makes an instance of ColoredStringBuilder from the provided report, for 
        /// use populating an Item tooltip.
        /// </summary>
        /// <param name="report">The report.</param>
        /// <returns></returns>
        public ColoredStringBuilder MakeInstance(ReportType report) {
            ColoredStringBuilder csb = new ColoredStringBuilder();
            var orderedInfoIDs = OrderedInfoIDsToDisplay;
            int lastIDIndex = orderedInfoIDs.Length - Constants.One;
            for (int i = 0; i <= lastIDIndex; i++) {
                ItemInfoID infoID = orderedInfoIDs[i];
                string colorizedText;
                if (TryMakeColorizedText(infoID, report, out colorizedText)) {
                    csb.Append(colorizedText);
                    if (i < lastIDIndex) {
                        csb.AppendLine();
                    }
                }
                else {
                    D.Warn("{0} reports missing {1}: {2}.", DebugName, typeof(ItemInfoID).Name, infoID.GetValueName());
                }
            }
            //D.Log("{0}.MakeInstance() returning {1}.", DebugName, csb.ToString());
            return csb;
        }

        protected virtual bool TryMakeColorizedText(ItemInfoID infoID, ReportType report, out string colorizedText) {
            bool isSuccess = false;
            _lineTemplate = GetLineTemplate(infoID);
            switch (infoID) {
                case ItemInfoID.Separator:
                    isSuccess = true;
                    colorizedText = _lineTemplate;
                    break;
                case ItemInfoID.Name:
                    isSuccess = true;
                    colorizedText = _lineTemplate.Inject(report.Name != null ? report.Name : Unknown);
                    break;
                case ItemInfoID.Owner:
                    isSuccess = true;
                    colorizedText = GetColorizedOwnerText(report.Owner);
                    break;
                case ItemInfoID.Position:
                    isSuccess = true;
                    colorizedText = _lineTemplate.Inject(report.Position.HasValue ? report.Position.Value.ToString() : Unknown);
                    break;
                case ItemInfoID.CameraDistance:
                    isSuccess = true;
                    colorizedText = _lineTemplate.Inject(GetFormat(infoID).Inject(report.__PositionForCameraDistance.Value.DistanceToCamera()));
                    // This approach returns the distance to DummyTarget in Item tooltip after doing a Freeform zoom on open space
                    // colorizedText = _phrase.Inject(GetFormat(contentID).Inject(References.MainCameraControl.DistanceToCameraTarget));
                    break;
                default:
                    colorizedText = null;
                    break;
            }
            return isSuccess;
        }

        private string GetColorizedOwnerText(Player owner) {
            string colorizedOwnerText = Unknown;
            string userRelationsText = Unknown;
            if (owner != null) {
                colorizedOwnerText = owner.LeaderName.SurroundWith(owner.Color);
                userRelationsText = owner.UserRelations.GetValueName();
            }
            string text = _lineTemplate.Inject(colorizedOwnerText, userRelationsText);
            return text;
        }

        #region Archive

        /// <summary>
        /// Unique identifier for each line item of content present in a text tooltip.
        /// </summary>
        //public enum ContentID {

        //    None,

        //    Name,
        //    ParentName,
        //    Owner,

        //    IntelState,

        //    Category,

        //    Capacity,
        //    Resources,

        //    MaxHitPoints,
        //    CurrentHitPoints,
        //    Health,
        //    Defense,
        //    Mass,

        //    Science,
        //    Culture,
        //    NetIncome,

        //    Approval,
        //    Position,

        //    SectorID,

        //    WeaponsRange,
        //    SensorRange,
        //    Offense,

        //    Target,
        //    CombatStance,
        //    CurrentSpeed,
        //    FullSpeed,
        //    MaxTurnRate,

        //    Composition,
        //    Formation,
        //    CurrentCmdEffectiveness,

        //    UnitWeaponsRange,
        //    UnitSensorRange,
        //    UnitOffense,
        //    UnitDefense,
        //    UnitMaxHitPts,
        //    UnitCurrentHitPts,
        //    UnitHealth,

        //    Population,
        //    UnitScience,
        //    UnitCulture,
        //    UnitNetIncome,

        //    UnitFullSpeed,
        //    UnitMaxTurnRate,

        //    [System.Obsolete]
        //    Density,

        //    CameraDistance,
        //    TargetDistance,
        //    OrbitalSpeed
        //}


        //public class ColoredTextList_Intel : ColoredTextList {

        //    public ColoredTextList_Intel(AIntel intel) {
        //        string intelText_formatted = ConstructIntelText(intel);
        //        _list.Add(new ColoredText(intelText_formatted));
        //    }

        //    private string ConstructIntelText(AIntel intel) {
        //        string intelMsg = intel.CurrentCoverage.GetValueName();
        //        string addendum = ". Intel is current.";
        //        var intelWithDatedCoverage = intel as Intel;
        //        if (intelWithDatedCoverage != null && intelWithDatedCoverage.IsDatedCoverageValid) {
        //            //D.Log("DateStamp = {0}, CurrentDate = {1}.", intelWithDatedCoverage.DateStamp, GameTime.Instance.CurrentDate);
        //            GameTimeDuration intelAge = new GameTimeDuration(intelWithDatedCoverage.DateStamp, GameTime.Instance.CurrentDate);
        //            addendum = String.Format(". Intel age {0}.", intelAge.ToString());
        //        }
        //        intelMsg = intelMsg + addendum;
        //        //D.Log(intelMsg);
        //        return GetDefaultPhrase(ContentID.IntelState).Inject(intelMsg);
        //    }

        //    public override string ToString() {
        //        return TextElements.Concatenate();
        //    }
        //}

        //public class ColoredTextList_Distance : ColoredTextList {

        //    /// <summary>
        //    /// Initializes a new instance of the <see cref="ColoredTextList_Distance"/> class. This version
        //    /// generates a ColoredTextList containing the distance from the camera's focal plane to the provided
        //    /// point in world space.
        //    /// </summary>
        //    /// <param name="point">The point in world space..</param>
        //    /// <param name="format">The format.</param>
        //    public ColoredTextList_Distance(Vector3 point, string format = Constants.FormatFloat_1DpMax) {
        //        //TODO calculate from Data.Position and <code>static GetSelected()<code>
        //        //if(nothing selected) return empty
        //        float distance = point.DistanceToCamera();
        //        string text = GetDefaultPhrase(ContentID.CameraDistance).Inject(format.Inject(distance));
        //        _list.Add(new ColoredText(text));
        //    }

        //    /// <summary>
        //    /// Initializes a new instance of the <see cref="ColoredTextList_Distance"/> class. This version
        //    /// generates a ColoredTextList containing the distance from the camera's focal plane to the camera's 
        //    /// current target point.
        //    /// </summary>
        //    /// <param name="format">The format.</param>
        //    public ColoredTextList_Distance(string format = Constants.FormatFloat_1DpMax) {
        //        float distance = References.MainCameraControl.DistanceToCamera;
        //        string text = GetDefaultPhrase(ContentID.CameraDistance).Inject(format.Inject(distance));
        //        _list.Add(new ColoredText(text));
        //    }

        //    /// <summary>
        //    /// Initializes a new instance of the <see cref="ColoredTextList_Distance"/> class. This version
        //    /// generates a ColoredTextList containing the distance between 2 points in world space.
        //    /// </summary>
        //    /// <param name="pointA">The first point in world space.</param>
        //    /// <param name="pointB">The second point in world space.</param>
        //    /// <param name="format">The format.</param>
        //    public ColoredTextList_Distance(Vector3 pointA, Vector3 pointB, string format = Constants.FormatFloat_1DpMax) {
        //        float distance = Vector3.Distance(pointA, pointB);
        //        string text = GetDefaultPhrase(ContentID.TargetDistance).Inject(format.Inject(distance));
        //        _list.Add(new ColoredText(text));
        //    }

        //    public override string ToString() {
        //        return TextElements.Concatenate();
        //    }
        //}

        //public class ColoredTextList_Owner : ColoredTextList {

        //    public ColoredTextList_Owner(Player _player) {
        //        var leaderCText = new ColoredText(player.LeaderName, player.Color);
        //        var phrase = GetDefaultPhrase(ContentID.Owner);
        //        _list.Add(MakeInstance(phrase, leaderCText));
        //    }

        //    public override string ToString() {
        //        return TextElements.Concatenate();
        //    }
        //}

        //public class ColoredTextList_Health : ColoredTextList {

        //    public ColoredTextList_Health(float? health, float? maxHp, bool unitHealth = false) {
        //        GameColor healthColor = GameColor.White;
        //        string healthFormatted = Constants.QuestionMark;
        //        if (health.HasValue) {
        //            healthColor = (health.Value > GeneralSettings.Instance.InjuredHealthThreshold) ? GameColor.Green :
        //                        (health.Value > GeneralSettings.Instance.CriticalHealthThreshold) ? GameColor.Yellow :
        //                                                                                                GameColor.Red;
        //            healthFormatted = Constants.FormatPercent_0Dp.Inject(health.Value);
        //        }

        //        GameColor maxHpColor = GameColor.White;
        //        string maxHpFormatted = Constants.QuestionMark;
        //        if (maxHp.HasValue) {
        //            maxHpColor = GameColor.Green;
        //            maxHpFormatted = Constants.FormatFloat_1DpMax.Inject(maxHp.Value);
        //        }
        //        var healthCText = new ColoredText(healthFormatted, healthColor);
        //        var hitPtCText = new ColoredText(maxHpFormatted, maxHpColor);
        //        var phrase = GetDefaultPhrase(unitHealth ? ContentID.UnitHealth : ContentID.Health);
        //        _list.Add(MakeInstance(phrase, healthCText, hitPtCText));
        //    }

        //    public override string ToString() {
        //        return TextElements.Concatenate();
        //    }
        //}

        //public class ColoredTextList_Approval : ColoredTextList {

        //    public ColoredTextList_Approval(float? approval) {
        //        GameColor approvalColor = GameColor.White;
        //        string approvalText = Constants.QuestionMark;
        //        if (approval.HasValue) {
        //            approvalColor = (approval.Value > GeneralSettings.Instance.ContentApprovalThreshold) ? GameColor.Green :
        //                (approval.Value > GeneralSettings.Instance.UnhappyApprovalThreshold) ? GameColor.White :
        //                (approval.Value > GeneralSettings.Instance.RevoltApprovalThreshold) ? GameColor.Yellow :
        //                                                                                      GameColor.Red;
        //            approvalText = Constants.FormatPercent_0Dp.Inject(approval.Value);
        //        }
        //        var approvalCText = new ColoredText(approvalText, approvalColor);
        //        var phrase = GetDefaultPhrase(ContentID.Approval);
        //        _list.Add(MakeInstance(phrase, approvalCText));
        //    }

        //    public override string ToString() {
        //        return TextElements.Concatenate();
        //    }
        //}
        #endregion

    }
}

