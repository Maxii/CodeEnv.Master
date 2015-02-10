// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ALabelTextFactory.cs
//  Abstract base class for LabelText Factories.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for LabelText Factories.
    /// </summary>
    public abstract class ALabelTextFactory {

        protected static IColoredTextList _unknownContent = new ColoredTextList_String(Constants.QuestionMark);

        protected static IColoredTextList _emptyContent = new ColoredTextListBase();

        private static IDictionary<LabelContentID, string> _defaultPhraseLookup = new Dictionary<LabelContentID, string>() {

                {LabelContentID.Name, "Name: {0}"},
                {LabelContentID.ParentName, "Parent: {0}"},
                {LabelContentID.Owner, "Owner: {0}"},
                {LabelContentID.Category, "Category: {0}"},
                {LabelContentID.SectorIndex, "Sector: {0}"},
                {LabelContentID.Density, "Density: {0}"},

                {LabelContentID.Composition, "{0}"},
                {LabelContentID.Formation, "Formation: {0}"},
                {LabelContentID.CurrentCmdEffectiveness, "Cmd Eff: {0}"},
                {LabelContentID.UnitMaxWeaponsRange, "UnitMaxWeaponsRange: {0}"},
                {LabelContentID.UnitMaxSensorRange, "UnitMaxSensorRange: {0}"},
                {LabelContentID.UnitOffense, "UnitOffense: {0}"},
                {LabelContentID.UnitDefense, "UnitDefense: {0}"},
                {LabelContentID.UnitCurrentHitPts, "UnitCurrentHitPts: {0}"},
                {LabelContentID.UnitMaxHitPts, "UnitMaxHitPts: {0}"},
                {LabelContentID.UnitHealth, "UnitHealth: {0}, UnitMaxHP: {1}"},
                {LabelContentID.UnitFullSpeed, "UnitFullSpeed: {0}"},
                {LabelContentID.UnitMaxTurnRate, "UnitMaxTurnRate:  {0}"},

                {LabelContentID.Target, "Target: {0}"},
                {LabelContentID.TargetDistance, "TargetDistance: {0}"},
                {LabelContentID.CurrentSpeed, "Speed: {0}"},
                {LabelContentID.FullSpeed, "FullSpeed: {0}"}, 
                {LabelContentID.MaxTurnRate, "MaxTurnRate: {0}"},
                {LabelContentID.CombatStance, "Stance: {0}"},

                {LabelContentID.Population, "Population: {0}"},
                {LabelContentID.CapacityUsed, "CapacityUsed: {0}"},
                {LabelContentID.ResourcesUsed, "ResourcesUsed: {0}"},
                {LabelContentID.SpecialsUsed, "SpecialsUsed: {0}"},

                {LabelContentID.CurrentHitPoints, "CurrentHitPts: {0}"},
                {LabelContentID.MaxHitPoints, "MaxHitPts: {0}"},
                {LabelContentID.Health, "Health: {0}, MaxHP: {1}"},
                {LabelContentID.Defense, "Defense: {0}"},
                {LabelContentID.Offense, "Offense: {0}"},
                {LabelContentID.Mass, "Mass: {0}"},
                {LabelContentID.MaxWeaponsRange, "MaxWeaponsRange: {0}"},
                {LabelContentID.MaxSensorRange, "MaxSensorRange: {0}"},

                {LabelContentID.Capacity, "Capacity: {0}"},
                {LabelContentID.Resources, "Resources: {0}"},
                {LabelContentID.Specials, "[800080]Specials:[-] {0}"},
                {LabelContentID.OrbitalSpeed, "RelativeOrbitSpeed: {0}"},

                {LabelContentID.CameraDistance, "CameraDistance: {0}"},
                {LabelContentID.IntelState, "< {0} >"}

        };

        private static IDictionary<LabelContentID, string> _defaultNumberFormatLookup = new Dictionary<LabelContentID, string>() {

                {LabelContentID.Density, Constants.FormatFloat_1DpMax},

                {LabelContentID.CurrentCmdEffectiveness, Constants.FormatInt_1DMin},
                {LabelContentID.UnitMaxWeaponsRange, Constants.FormatFloat_0Dp},
                {LabelContentID.UnitMaxSensorRange, Constants.FormatFloat_0Dp},
                {LabelContentID.UnitCurrentHitPts, Constants.FormatFloat_0Dp},
                {LabelContentID.UnitMaxHitPts, Constants.FormatFloat_0Dp},
                {LabelContentID.UnitHealth, Constants.FormatPercent_0Dp},
                {LabelContentID.UnitFullSpeed, Constants.FormatFloat_0Dp},
                {LabelContentID.UnitMaxTurnRate, Constants.FormatFloat_0Dp},

                {LabelContentID.TargetDistance, Constants.FormatFloat_0Dp},
                {LabelContentID.CurrentSpeed, Constants.FormatFloat_1DpMax},
                {LabelContentID.FullSpeed, Constants.FormatFloat_1DpMax}, 
                {LabelContentID.MaxTurnRate, Constants.FormatFloat_0Dp},

                {LabelContentID.Population, Constants.FormatInt_1DMin},
                {LabelContentID.CapacityUsed, Constants.FormatInt_1DMin},

                {LabelContentID.CurrentHitPoints, Constants.FormatFloat_0Dp},
                {LabelContentID.MaxHitPoints, Constants.FormatFloat_0Dp},
                {LabelContentID.Health, Constants.FormatPercent_0Dp},
                {LabelContentID.Mass, Constants.FormatInt_1DMin},
                {LabelContentID.MaxWeaponsRange, Constants.FormatFloat_0Dp},
                {LabelContentID.MaxSensorRange, Constants.FormatFloat_0Dp},

                {LabelContentID.Capacity, Constants.FormatInt_1DMin},
                {LabelContentID.OrbitalSpeed, Constants.FormatFloat_2DpMax}
        };


        protected static IDictionary<LabelID, bool> _includeUnknownLookup = new Dictionary<LabelID, bool>() {
            {LabelID.CursorHud, false }
            // TODO
        };

        protected static IDictionary<LabelID, bool> _dedicatedLinePerContentIDLookup = new Dictionary<LabelID, bool>() {
            {LabelID.CursorHud, true }
            // TODO
        };

        public ALabelTextFactory() { }

        /// <summary>
        /// Gets the numerical format for this contentID.
        /// </summary>
        /// <param name="contentID">The content identifier.</param>
        /// <returns></returns>
        protected string GetFormat(LabelContentID contentID) {
            string valueFormat;
            if (!_defaultNumberFormatLookup.TryGetValue(contentID, out valueFormat)) {
                D.Warn("{0} reports no default numerical format for {1} found.", GetType().Name, contentID.GetName());
                valueFormat = "{0}";
            }
            return valueFormat;
        }

        /// <summary>
        /// Gets the list of <c>LabelContentID</c>s to include in the LabelText.
        /// </summary>
        /// <param name="labelID">The label identifier.</param>
        /// <returns></returns>
        protected abstract IEnumerable<LabelContentID> GetIncludedContentIDs(LabelID labelID);

        /// <summary>
        /// Trys to get the override phrase to pair with the content in the LabelText. 
        /// </summary>
        /// <param name="labelID">The label identifier.</param>
        /// <param name="contentID">The content identifier.</param>
        /// <param name="overridePhrase">The override phrase.</param>
        /// <returns></returns>
        protected abstract bool TryGetOverridePhrase(LabelID labelID, LabelContentID contentID, out string overridePhrase);

        protected string GetDefaultPhrase(LabelContentID contentID) {
            string defaultPhrase;
            if (!_defaultPhraseLookup.TryGetValue(contentID, out defaultPhrase)) {
                D.Warn("{0} reports no defaultPhrase for {1} found.", GetType().Name, contentID.GetName());
                defaultPhrase = "DefaultPhrase {0}";
            }
            return defaultPhrase;
        }

        #region Nested Classes

        public class ColoredTextList_Intel : ColoredTextListBase {

            public ColoredTextList_Intel(AIntel intel) {
                string intelText_formatted = ConstructIntelText(intel);
                _list.Add(new ColoredText(intelText_formatted));
            }

            private string ConstructIntelText(AIntel intel) {
                string intelMsg = intel.CurrentCoverage.GetName();
                string addendum = ". Intel is current.";
                var intelWithDatedCoverage = intel as Intel;
                if (intelWithDatedCoverage != null && intelWithDatedCoverage.IsDatedCoverageValid) {
                    //D.Log("DateStamp = {0}, CurrentDate = {1}.", intelWithDatedCoverage.DateStamp, GameTime.Instance.CurrentDate);
                    GameTimeDuration intelAge = new GameTimeDuration(intelWithDatedCoverage.DateStamp, GameTime.Instance.CurrentDate);
                    addendum = String.Format(". Intel age {0}.", intelAge.ToString());
                }
                intelMsg = intelMsg + addendum;
                D.Log(intelMsg);
                return intelMsg;
            }
        }

        public class ColoredTextList_Distance : ColoredTextListBase {

            /// <summary>
            /// Initializes a new instance of the <see cref="ColoredTextList_Distance"/> class. This version
            /// generates a ColoredTextList containing the distance from the camera's focal plane to the provided
            /// point in world space.
            /// </summary>
            /// <param name="point">The point in world space..</param>
            /// <param name="format">The format.</param>
            public ColoredTextList_Distance(Vector3 point, string format = Constants.FormatFloat_1DpMax) {
                // TODO calculate from Data.Position and <code>static GetSelected()<code>
                //if(nothing selected) return empty
                float distance = point.DistanceToCamera();
                _list.Add(new ColoredText(format.Inject(distance)));
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ColoredTextList_Distance"/> class. This version
            /// generates a ColoredTextList containing the distance from the camera's focal plane to the camera's 
            /// current target point.
            /// </summary>
            /// <param name="format">The format.</param>
            public ColoredTextList_Distance(string format = Constants.FormatFloat_1DpMax) {
                float distance = References.MainCameraControl.DistanceToCamera;
                _list.Add(new ColoredText(format.Inject(distance)));
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ColoredTextList_Distance"/> class. This version
            /// generates a ColoredTextList containing the distance between 2 points in world space.
            /// </summary>
            /// <param name="pointA">The first point in world space.</param>
            /// <param name="pointB">The second point in world space.</param>
            /// <param name="format">The format.</param>
            public ColoredTextList_Distance(Vector3 pointA, Vector3 pointB, string format = Constants.FormatFloat_1DpMax) {
                float distance = Vector3.Distance(pointA, pointB);
                _list.Add(new ColoredText(format.Inject(distance)));
            }

        }

        public class ColoredTextList_Owner : ColoredTextListBase {

            public ColoredTextList_Owner(Player player) {
                _list.Add(new ColoredText(player.LeaderName, player.Color));
            }
        }

        public class ColoredTextList_Health : ColoredTextListBase {

            public ColoredTextList_Health(float? health, float? maxHp) {
                GameColor healthColor = GameColor.White;
                string healthFormatted = Constants.QuestionMark;
                if (health.HasValue) {
                    healthColor = (health.Value > GeneralSettings.Instance.InjuredHealthThreshold) ? GameColor.Green :
                                ((health.Value > GeneralSettings.Instance.CriticalHealthThreshold) ? GameColor.Yellow : GameColor.Red);
                    healthFormatted = Constants.FormatPercent_0Dp.Inject(health.Value);
                }

                GameColor maxHpColor = GameColor.White;
                string maxHpFormatted = Constants.QuestionMark;
                if (maxHp.HasValue) {
                    maxHpColor = GameColor.Green;
                    maxHpFormatted = Constants.FormatFloat_1DpMax.Inject(maxHp.Value);
                }
                _list.Add(new ColoredText(healthFormatted, healthColor));
                _list.Add(new ColoredText(maxHpFormatted, maxHpColor));
            }
        }

        #endregion

    }
}

