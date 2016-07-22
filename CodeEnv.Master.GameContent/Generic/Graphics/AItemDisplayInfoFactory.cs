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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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

        protected static string _unknown = Constants.QuestionMark;

        private static IDictionary<AccessControlInfoID, string> _defaultPhraseLookup = new Dictionary<AccessControlInfoID, string>() {

                {AccessControlInfoID.Name, "Name: {0}"},
                {AccessControlInfoID.ParentName, "Parent: {0}"},
                {AccessControlInfoID.Owner, "Owner: {0}"},
                {AccessControlInfoID.Category, "Category: {0}"},
                {AccessControlInfoID.SectorIndex, "Sector: {0}"},
                {AccessControlInfoID.Position, "Position: {0}"},

                {AccessControlInfoID.Composition, CommonTerms.Composition + ": {0}"},
                {AccessControlInfoID.Formation, "Formation: {0}"},
                {AccessControlInfoID.CurrentCmdEffectiveness, "Cmd Eff: {0}"},
                {AccessControlInfoID.UnitWeaponsRange, "UnitWeaponsRange: {0}"},
                {AccessControlInfoID.UnitSensorRange, "UnitSensorRange: {0}"},
                {AccessControlInfoID.UnitOffense, "UnitOffense: {0}"},
                {AccessControlInfoID.UnitDefense, "UnitDefense: {0}"},
                {AccessControlInfoID.UnitCurrentHitPts, "UnitCurrentHitPts: {0}"},
                {AccessControlInfoID.UnitMaxHitPts, "UnitMaxHitPts: {0}"},
                {AccessControlInfoID.UnitHealth, "UnitHealth: {0}, UnitMaxHP: {1}"},
                {AccessControlInfoID.UnitFullSpeed, "UnitFullSpeedValue: {0}"},
                {AccessControlInfoID.UnitMaxTurnRate, "UnitMaxTurnRate:  {0}"},

                {AccessControlInfoID.Target, "Target: {0}"},
                {AccessControlInfoID.TargetDistance, "TargetDistance: {0}"},
                {AccessControlInfoID.CurrentSpeed, "Speed: {0}"},
                {AccessControlInfoID.FullSpeed, "FullSpeedValue: {0}"},
                {AccessControlInfoID.MaxTurnRate, "MaxTurnRate: {0}"},
                {AccessControlInfoID.CombatStance, "Stance: {0}"},

                {AccessControlInfoID.Population, "Population: {0}"},

                {AccessControlInfoID.CurrentHitPoints, "CurrentHitPts: {0}"},
                {AccessControlInfoID.MaxHitPoints, "MaxHitPts: {0}"},
                {AccessControlInfoID.Health, "Health: {0}, MaxHP: {1}"},
                {AccessControlInfoID.Defense, "Defense: {0}"},
                {AccessControlInfoID.Offense, "Offense: {0}"},
                {AccessControlInfoID.Mass, "Mass: {0}"},
                {AccessControlInfoID.WeaponsRange, "WeaponsRange: {0}"},
                {AccessControlInfoID.SensorRange, "SensorRange: {0}"},

                {AccessControlInfoID.Science, "Science: {0}"},
                {AccessControlInfoID.Culture, "Culture: {0}"},
                {AccessControlInfoID.NetIncome, "NetIncome: {0}"},
                {AccessControlInfoID.UnitScience, "UnitScience: {0}"},
                {AccessControlInfoID.UnitCulture, "UnitCulture: {0}"},
                {AccessControlInfoID.UnitNetIncome, "UnitNetIncome: {0}"},
                {AccessControlInfoID.Approval, "Approval: {0}"},

                {AccessControlInfoID.Capacity, "Capacity: {0}"},
                {AccessControlInfoID.Resources, "[800080]Resources:[-] {0}"},
                {AccessControlInfoID.OrbitalSpeed, "RelativeOrbitSpeed: {0}"},

                {AccessControlInfoID.CameraDistance, "CameraDistance: {0}"},
                {AccessControlInfoID.IntelState, "< {0} >"}
        };

        private static IDictionary<AccessControlInfoID, string> _defaultNumberFormatLookup = new Dictionary<AccessControlInfoID, string>() {

                {AccessControlInfoID.CurrentCmdEffectiveness, Constants.FormatFloat_2Dp},
                {AccessControlInfoID.UnitCurrentHitPts, Constants.FormatFloat_0Dp},
                {AccessControlInfoID.UnitMaxHitPts, Constants.FormatFloat_0Dp},
                {AccessControlInfoID.UnitFullSpeed, Constants.FormatFloat_2Dp},
                {AccessControlInfoID.UnitMaxTurnRate, Constants.FormatFloat_0Dp},

                {AccessControlInfoID.TargetDistance, Constants.FormatFloat_0Dp},
                {AccessControlInfoID.CurrentSpeed, Constants.FormatFloat_2DpMax},
                {AccessControlInfoID.FullSpeed, Constants.FormatFloat_2DpMax},
                {AccessControlInfoID.MaxTurnRate, Constants.FormatFloat_0Dp},

                {AccessControlInfoID.Population, Constants.FormatInt_1DMin},
                {AccessControlInfoID.Capacity, Constants.FormatInt_1DMin},

                {AccessControlInfoID.Science, Constants.FormatFloat_0Dp},
                {AccessControlInfoID.Culture, Constants.FormatFloat_0Dp},
                {AccessControlInfoID.NetIncome, Constants.FormatFloat_0Dp},
                {AccessControlInfoID.UnitScience, Constants.FormatFloat_0Dp},
                {AccessControlInfoID.UnitCulture, Constants.FormatFloat_0Dp},
                {AccessControlInfoID.UnitNetIncome, Constants.FormatFloat_0Dp},

                {AccessControlInfoID.CurrentHitPoints, Constants.FormatFloat_0Dp},
                {AccessControlInfoID.MaxHitPoints, Constants.FormatFloat_0Dp},
                {AccessControlInfoID.Mass, Constants.FormatInt_1DMin},

                {AccessControlInfoID.OrbitalSpeed, Constants.FormatFloat_3DpMax},
                {AccessControlInfoID.CameraDistance, Constants.FormatFloat_1DpMax}
        };

        /// <summary>
        /// Gets the numerical format for this infoID.
        /// </summary>
        /// <param name="infoID">The info identifier.</param>
        /// <returns></returns>
        protected static string GetFormat(AccessControlInfoID infoID) {
            string valueFormat;
            if (!_defaultNumberFormatLookup.TryGetValue(infoID, out valueFormat)) {
                D.Warn("{0} reports no default numerical format for {1} found.", Instance.GetType().Name, infoID.GetValueName());
                valueFormat = "{0}";
            }
            return valueFormat;
        }

        protected static string GetPhrase(AccessControlInfoID infoID) {
            string phrase;
            if (!_defaultPhraseLookup.TryGetValue(infoID, out phrase)) {
                D.Warn("{0} reports no phrase for {1} found.", Instance.GetType().Name, infoID.GetValueName());
                phrase = "DefaultPhrase {0}";
            }
            return phrase;
        }

        protected abstract AccessControlInfoID[] InfoIDsToDisplay { get; }

        /// <summary>
        /// The _phrase to use when constructing each ColorizedText string. 
        /// </summary>
        protected string _phrase;

        /// <summary>
        /// Makes an instance of ColoredStringBuilder from the provided report, for 
        /// use populating an Item tooltip.
        /// </summary>
        /// <param name="report">The report.</param>
        /// <returns></returns>
        public ColoredStringBuilder MakeInstance(ReportType report) {
            ColoredStringBuilder csb = new ColoredStringBuilder();
            var infoIDs = InfoIDsToDisplay;
            int lastIDIndex = infoIDs.Length - Constants.One;
            for (int i = 0; i <= lastIDIndex; i++) {
                AccessControlInfoID infoID = infoIDs[i];
                string colorizedText;
                if (TryMakeColorizedText(infoID, report, out colorizedText)) {
                    csb.Append(colorizedText);
                    if (i < lastIDIndex) {
                        csb.AppendLine();
                    }
                }
                else {
                    D.Warn("{0} reports missing {1}: {2}.", GetType().Name, typeof(AccessControlInfoID).Name, infoID.GetValueName());
                }
            }
            return csb;
        }

        protected virtual bool TryMakeColorizedText(AccessControlInfoID infoID, ReportType report, out string colorizedText) {
            bool isSuccess = false;
            _phrase = GetPhrase(infoID);
            switch (infoID) {
                case AccessControlInfoID.Name:
                    isSuccess = true;
                    colorizedText = _phrase.Inject(report.Name != null ? report.Name : _unknown);
                    break;
                case AccessControlInfoID.Owner:
                    isSuccess = true;
                    colorizedText = _phrase.Inject(report.Owner != null ? report.Owner.LeaderName.SurroundWith(report.Owner.Color) : _unknown);
                    break;
                case AccessControlInfoID.Position:
                    isSuccess = true;
                    colorizedText = _phrase.Inject(report.Position.HasValue ? report.Position.Value.ToString() : _unknown);
                    break;
                case AccessControlInfoID.CameraDistance:
                    isSuccess = true;
                    colorizedText = _phrase.Inject(GetFormat(infoID).Inject(report.__PositionForCameraDistance.DistanceToCamera()));
                    // This approach returns the distance to DummyTarget in Item tooltip after doing a Freeform zoom on open space
                    // colorizedText = _phrase.Inject(GetFormat(contentID).Inject(References.MainCameraControl.DistanceToCameraTarget));
                    break;
                default:
                    colorizedText = null;
                    break;
            }
            return isSuccess;
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

        //    SectorIndex,

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

        //    public ColoredTextList_Owner(Player player) {
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

