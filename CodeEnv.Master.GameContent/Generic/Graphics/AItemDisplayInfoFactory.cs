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

        private static IDictionary<ContentID, string> _defaultPhraseLookup = new Dictionary<ContentID, string>() {

                {ContentID.Name, "Name: {0}"},
                {ContentID.ParentName, "Parent: {0}"},
                {ContentID.Owner, "Owner: {0}"},
                {ContentID.Category, "Category: {0}"},
                {ContentID.SectorIndex, "Sector: {0}"},
                {ContentID.Position, "Position: {0}"},
                //{ContentID.Density, "Density: {0}"},

                {ContentID.Composition, CommonTerms.Composition + ": {0}"},
                {ContentID.Formation, "Formation: {0}"},
                {ContentID.CurrentCmdEffectiveness, "Cmd Eff: {0}"},
                {ContentID.UnitWeaponsRange, "UnitWeaponsRange: {0}"},
                {ContentID.UnitSensorRange, "UnitSensorRange: {0}"},
                {ContentID.UnitOffense, "UnitOffense: {0}"},
                {ContentID.UnitDefense, "UnitDefense: {0}"},
                {ContentID.UnitCurrentHitPts, "UnitCurrentHitPts: {0}"},
                {ContentID.UnitMaxHitPts, "UnitMaxHitPts: {0}"},
                {ContentID.UnitHealth, "UnitHealth: {0}, UnitMaxHP: {1}"},
                {ContentID.UnitFullSpeed, "UnitFullSpeed: {0}"},
                {ContentID.UnitMaxTurnRate, "UnitMaxTurnRate:  {0}"},

                {ContentID.Target, "Target: {0}"},
                {ContentID.TargetDistance, "TargetDistance: {0}"},
                {ContentID.CurrentSpeed, "Speed: {0}"},
                {ContentID.FullSpeed, "FullSpeed: {0}"},
                {ContentID.MaxTurnRate, "MaxTurnRate: {0}"},
                {ContentID.CombatStance, "Stance: {0}"},

                {ContentID.Population, "Population: {0}"},

                {ContentID.CurrentHitPoints, "CurrentHitPts: {0}"},
                {ContentID.MaxHitPoints, "MaxHitPts: {0}"},
                {ContentID.Health, "Health: {0}, MaxHP: {1}"},
                {ContentID.Defense, "Defense: {0}"},
                {ContentID.Offense, "Offense: {0}"},
                {ContentID.Mass, "Mass: {0}"},
                {ContentID.WeaponsRange, "WeaponsRange: {0}"},
                {ContentID.SensorRange, "SensorRange: {0}"},

                {ContentID.Science, "Science: {0}"},
                {ContentID.Culture, "Culture: {0}"},
                {ContentID.NetIncome, "NetIncome: {0}"},
                {ContentID.UnitScience, "UnitScience: {0}"},
                {ContentID.UnitCulture, "UnitCulture: {0}"},
                {ContentID.UnitNetIncome, "UnitNetIncome: {0}"},
                {ContentID.Approval, "Approval: {0}"},

                {ContentID.Capacity, "Capacity: {0}"},
                {ContentID.Resources, "[800080]Resources:[-] {0}"},
                {ContentID.OrbitalSpeed, "RelativeOrbitSpeed: {0}"},

                {ContentID.CameraDistance, "CameraDistance: {0}"},
                {ContentID.IntelState, "< {0} >"}
        };

        private static IDictionary<ContentID, string> _defaultNumberFormatLookup = new Dictionary<ContentID, string>() {

                //{ContentID.Density, Constants.FormatFloat_1DpMax},

                {ContentID.CurrentCmdEffectiveness, Constants.FormatInt_1DMin},
                {ContentID.UnitCurrentHitPts, Constants.FormatFloat_0Dp},
                {ContentID.UnitMaxHitPts, Constants.FormatFloat_0Dp},
                {ContentID.UnitFullSpeed, Constants.FormatFloat_2Dp},
                {ContentID.UnitMaxTurnRate, Constants.FormatFloat_0Dp},

                {ContentID.TargetDistance, Constants.FormatFloat_0Dp},
                {ContentID.CurrentSpeed, Constants.FormatFloat_2DpMax},
                {ContentID.FullSpeed, Constants.FormatFloat_2DpMax},
                {ContentID.MaxTurnRate, Constants.FormatFloat_0Dp},

                {ContentID.Population, Constants.FormatInt_1DMin},
                {ContentID.Capacity, Constants.FormatInt_1DMin},

                {ContentID.Science, Constants.FormatFloat_0Dp},
                {ContentID.Culture, Constants.FormatFloat_0Dp},
                {ContentID.NetIncome, Constants.FormatFloat_0Dp},
                {ContentID.UnitScience, Constants.FormatFloat_0Dp},
                {ContentID.UnitCulture, Constants.FormatFloat_0Dp},
                {ContentID.UnitNetIncome, Constants.FormatFloat_0Dp},

                {ContentID.CurrentHitPoints, Constants.FormatFloat_0Dp},
                {ContentID.MaxHitPoints, Constants.FormatFloat_0Dp},
                {ContentID.Mass, Constants.FormatInt_1DMin},

                {ContentID.OrbitalSpeed, Constants.FormatFloat_3DpMax},
                {ContentID.CameraDistance, Constants.FormatFloat_1DpMax}
        };

        /// <summary>
        /// Gets the numerical format for this contentID.
        /// </summary>
        /// <param name="contentID">The content identifier.</param>
        /// <returns></returns>
        protected static string GetFormat(ContentID contentID) {
            string valueFormat;
            if (!_defaultNumberFormatLookup.TryGetValue(contentID, out valueFormat)) {
                D.Warn("{0} reports no default numerical format for {1} found.", Instance.GetType().Name, contentID.GetValueName());
                valueFormat = "{0}";
            }
            return valueFormat;
        }

        protected static string GetPhrase(ContentID contentID) {
            string phrase;
            if (!_defaultPhraseLookup.TryGetValue(contentID, out phrase)) {
                D.Warn("{0} reports no phrase for {1} found.", Instance.GetType().Name, contentID.GetValueName());
                phrase = "DefaultPhrase {0}";
            }
            return phrase;
        }

        protected abstract ContentID[] ContentIDsToDisplay { get; }

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
            var contentIDs = ContentIDsToDisplay;
            int lastIDIndex = contentIDs.Length - Constants.One;
            for (int i = 0; i <= lastIDIndex; i++) {
                ContentID contentID = contentIDs[i];
                string colorizedText;
                if (TryMakeColorizedText(contentID, report, out colorizedText)) {
                    csb.Append(colorizedText);
                    if (i < lastIDIndex) {
                        csb.AppendLine();
                    }
                }
                else {
                    D.Warn("{0} reports missing {1}: {2}.", GetType().Name, typeof(ContentID).Name, contentID.GetValueName());
                }
            }
            return csb;
        }

        protected virtual bool TryMakeColorizedText(ContentID contentID, ReportType report, out string colorizedText) {
            bool isSuccess = false;
            _phrase = GetPhrase(contentID);
            switch (contentID) {
                case ContentID.Name:
                    isSuccess = true;
                    colorizedText = _phrase.Inject(report.Name != null ? report.Name : _unknown);
                    break;
                case ContentID.Owner:
                    isSuccess = true;
                    colorizedText = _phrase.Inject(report.Owner != null ? report.Owner.LeaderName.SurroundWith(report.Owner.Color) : _unknown);
                    break;
                case ContentID.Position:
                    isSuccess = true;
                    colorizedText = _phrase.Inject(report.Position.HasValue ? report.Position.Value.ToString() : _unknown);
                    break;
                case ContentID.CameraDistance:
                    isSuccess = true;
                    colorizedText = _phrase.Inject(GetFormat(contentID).Inject(References.MainCameraControl.DistanceToCameraTarget));
                    break;
                default:
                    colorizedText = null;
                    break;
            }
            return isSuccess;
        }

        #region Nested Classes

        /// <summary>
        /// Unique identifier for each line item of content present in a text tooltip.
        /// </summary>
        public enum ContentID {

            None,

            Name,
            ParentName,
            Owner,

            IntelState,

            Category,

            Capacity,
            Resources,

            MaxHitPoints,
            CurrentHitPoints,
            Health,
            Defense,
            Mass,

            Science,
            Culture,
            NetIncome,

            Approval,
            Position,

            SectorIndex,

            WeaponsRange,
            SensorRange,
            Offense,

            Target,
            CombatStance,
            CurrentSpeed,
            FullSpeed,
            MaxTurnRate,

            Composition,
            Formation,
            CurrentCmdEffectiveness,

            UnitWeaponsRange,
            UnitSensorRange,
            UnitOffense,
            UnitDefense,
            UnitMaxHitPts,
            UnitCurrentHitPts,
            UnitHealth,

            Population,
            UnitScience,
            UnitCulture,
            UnitNetIncome,

            UnitFullSpeed,
            UnitMaxTurnRate,

            [System.Obsolete]
            Density,

            CameraDistance,
            TargetDistance,
            OrbitalSpeed
        }

        #region Archive

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

        #endregion

    }
}

