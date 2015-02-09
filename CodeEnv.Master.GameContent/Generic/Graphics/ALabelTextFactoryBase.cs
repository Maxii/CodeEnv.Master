// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ALabelTextFactoryBase.cs
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
    public abstract class ALabelTextFactoryBase {

        protected static IColoredTextList _unknownValue = new ColoredTextList_String(Constants.QuestionMark);

        protected static IColoredTextList _emptyValue = new ColoredTextListBase();

        protected static IDictionary<LabelID, bool> _includeUnknownLookup = new Dictionary<LabelID, bool>() {
            {LabelID.CursorHud, false }
            // TODO
        };

        protected static IDictionary<LabelID, bool> _dedicatedLinePerContentIDLookup = new Dictionary<LabelID, bool>() {
            {LabelID.CursorHud, true }
            // TODO
        };

        public ALabelTextFactoryBase() { }

        /// <summary>
        /// Supplies the format lookup table keyed by <c>LabelContentID</c>, sourced from the derived class.
        /// </summary>
        /// <param name="labelID">The label identifier.</param>
        /// <returns></returns>
        protected abstract IDictionary<LabelContentID, string> GetFormatLookup(LabelID labelID);

        protected void ValidateIDs(LabelID labelID, LabelContentID contentID) {
            D.Assert(GetFormatLookup(labelID).ContainsKey(contentID), "Illegal ID combo: {0}, {1}.".Inject(labelID.GetName(), contentID.GetName()));
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

            public ColoredTextList_Health(float health, float maxHp, string format = Constants.FormatFloat_1DpMax) {
                GameColor healthColor = (health > GeneralSettings.Instance.InjuredHealthThreshold) ? GameColor.Green :
                            ((health > GeneralSettings.Instance.CriticalHealthThreshold) ? GameColor.Yellow : GameColor.Red);
                string health_formatted = format.Inject(health * 100);
                string maxHp_formatted = format.Inject(maxHp);
                _list.Add(new ColoredText(health_formatted, healthColor));
                _list.Add(new ColoredText(maxHp_formatted, GameColor.Green));
            }
        }

        public class ColoredTextList_Settlement : ColoredTextListBase {

            public ColoredTextList_Settlement(SettlementCmdItemData settlement) {
                _list.Add(new ColoredText(settlement.Category.GetName()));
                _list.Add(new ColoredText(settlement.Population.ToString()));
                _list.Add(new ColoredText(settlement.CapacityUsed.ToString()));
                _list.Add(new ColoredText("TBD"));  // OPE Used need format
                _list.Add(new ColoredText("TBD"));  // X Used need format
            }
        }

        public class ColoredTextList_Ship : ColoredTextListBase {

            public ColoredTextList_Ship(ShipItemData ship, string valueFormat = Constants.FormatFloat_1DpMax) {
                _list.Add(new ColoredText(ship.Category.GetName()));
                _list.Add(new ColoredText(valueFormat.Inject(ship.Mass)));
                _list.Add(new ColoredText(valueFormat.Inject(ship.MaxTurnRate)));
            }
        }

        #endregion

    }
}

