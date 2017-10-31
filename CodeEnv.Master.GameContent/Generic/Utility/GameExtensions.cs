// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameExtensions.cs
// Game-specific extensions.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Game-specific extensions.
    /// </summary>
    public static class GameExtensions {

        /// <summary>
        /// Subtracts right from left, returning the difference between left and right, aka left - right.
        /// <remarks>Only possible using Outputs from Data (no null yields) where right will always be a subset of left.</remarks>
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns></returns>
        public static OutputsYield Subtract(this OutputsYield left, OutputsYield right) {
            var leftIDsPresent = left.OutputsPresent;
            var rightIDsPresent = right.OutputsPresent;
            bool isRightSubsetOfLeft = !rightIDsPresent.Except(leftIDsPresent).Any();
            D.Assert(isRightSubsetOfLeft);

            IList<OutputsYield.OutputValuePair> resultingPairs = new List<OutputsYield.OutputValuePair>();

            foreach (var id in leftIDsPresent) {
                float? leftYield = left.GetYield(id);
                D.Assert(leftYield.HasValue);
                float? rightYield = right.IsPresent(id) ? right.GetYield(id) : Constants.ZeroF;
                D.Assert(rightYield.HasValue);
                float result = leftYield.Value - rightYield.Value;
                resultingPairs.Add(new OutputsYield.OutputValuePair(id, result));
            }
            return new OutputsYield(resultingPairs.ToArray());
        }

        /// <summary>
        /// Subtracts right from left, returning the difference between left and right, aka left - right.
        /// <remarks>Only possible using Resources from Data (no null yields) where right will always be a subset of left.</remarks>
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns></returns>
        public static ResourcesYield Subtract(this ResourcesYield left, ResourcesYield right) {
            var leftIDsPresent = left.ResourcesPresent;
            var rightIDsPresent = right.ResourcesPresent;
            bool isRightSubsetOfLeft = !rightIDsPresent.Except(leftIDsPresent).Any();
            D.Assert(isRightSubsetOfLeft);

            IList<ResourcesYield.ResourceValuePair> resultingPairs = new List<ResourcesYield.ResourceValuePair>();

            foreach (var id in leftIDsPresent) {
                float? leftYield = left.GetYield(id);
                D.Assert(leftYield.HasValue);
                float? rightYield = right.IsPresent(id) ? right.GetYield(id) : Constants.ZeroF;
                D.Assert(rightYield.HasValue);
                float result = leftYield.Value - rightYield.Value;
                resultingPairs.Add(new ResourcesYield.ResourceValuePair(id, result));
            }
            return new ResourcesYield(resultingPairs.ToArray());
        }



        /// <summary>
        /// Aggregates the nullable values provided and returns their addition-based sum. If one or more of these
        /// nullable values has no value (its null), it is excluded from the sum. If all values are null, the value returned is null.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public static float? NullableSum(this float? source, params float?[] args) {
            var argList = new List<float?>(args);
            argList.Add(source);
            return argList.NullableSum();
        }

        /// <summary>
        /// Aggregates the nullable values provided and returns their addition-based sum. If one or more of these
        /// nullable values has no value (its null), it is excluded from the sum. If all values are null, the value returned is null.
        /// </summary>
        /// <param name="sequence">The nullable values.</param>
        /// <returns></returns>
        public static float? NullableSum(this IEnumerable<float?> sequence) {
            var result = sequence.Sum();
            D.Assert(result.HasValue);  // Sum() will never return a null result
            if (result.Value == Constants.ZeroF && !sequence.IsNullOrEmpty() && sequence.All(fVal => !fVal.HasValue)) {
                // if the result is zero, then that result is not valid IFF the entire sequence is filled with null
                result = null;
            }
            return result;
        }

        /// <summary>
        /// Aggregates the nullable values provided and returns their addition-based sum. If one or more of these
        /// nullable values has no value (its null), it is excluded from the sum. If all values are null, the value returned is null.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public static int? NullableSum(this int? source, params int?[] args) {
            var argList = new List<int?>(args);
            argList.Add(source);
            return argList.NullableSum();
        }

        /// <summary>
        /// Aggregates the nullable values provided and returns their addition-based sum. If one or more of these
        /// nullable values has no value (its null), it is excluded from the sum. If all values are null, the value returned is null.
        /// </summary>
        /// <param name="sequence">The nullable values.</param>
        /// <returns></returns>
        public static int? NullableSum(this IEnumerable<int?> sequence) {
            var result = sequence.Sum();
            D.Assert(result.HasValue);  // Sum() will never return a null result
            if (result.Value == Constants.Zero && !sequence.IsNullOrEmpty() && sequence.All(fVal => !fVal.HasValue)) {
                // if the result is zero, then that result is not valid IFF the entire sequence is filled with null
                result = null;
            }
            return result;
        }

        /// <summary>
        /// Aggregates the nullable values provided and returns their addition-based sum. If one or more of these
        /// nullable values has no value (its null), it is excluded from the sum. If all values are null, the value returned is null.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        [Obsolete]
        public static ResourceYield? NullableSum(this ResourceYield? source, params ResourceYield?[] args) {
            var argList = new List<ResourceYield?>(args);
            argList.Add(source);
            return argList.NullableSum();
        }

        /// <summary>
        /// Aggregates the nullable values provided and returns their addition-based sum. If one or more of these
        /// nullable values has no value (its null), it is excluded from the sum. If all values are null, the value returned is null.
        /// </summary>
        /// <param name="sequence">The nullable values.</param>
        /// <returns></returns>
        [Obsolete]
        public static ResourceYield? NullableSum(this IEnumerable<ResourceYield?> sequence) {
            bool isAnyValueFound = false;
            ResourceYield? sum = default(ResourceYield);
            foreach (var a in sequence) {
                if (a.HasValue) {
                    isAnyValueFound = true;
                    sum += a.Value;
                }
            }
            return isAnyValueFound ? sum : null;
        }

        public static ResourcesYield Sum(this IEnumerable<ResourcesYield> sequence) {
            ResourcesYield sum = default(ResourcesYield);
            foreach (var ry in sequence) {
                sum += ry;
            }
            return sum;
        }

        public static OutputsYield Sum(this IEnumerable<OutputsYield> sequence) {
            OutputsYield sum = default(OutputsYield);
            foreach (var oy in sequence) {
                sum += oy;
            }
            return sum;
        }

        /// <summary>
        /// Aggregates the nullable values provided and returns their addition-based sum. If one or more of these
        /// nullable values has no value (its null), it is excluded from the sum. If all values are null, the value returned is null.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public static CombatStrength? NullableSum(this CombatStrength? source, params CombatStrength?[] args) {
            var argList = new List<CombatStrength?>(args);
            argList.Add(source);
            return argList.NullableSum();
        }

        /// <summary>
        /// Aggregates the nullable values provided and returns their addition-based sum. If one or more of these
        /// nullable values has no value (its null), it is excluded from the sum. If all values are null, the value returned is null.
        /// </summary>
        /// <param name="sequence">The nullable values.</param>
        /// <returns></returns>
        public static CombatStrength? NullableSum(this IEnumerable<CombatStrength?> sequence) {
            bool isAnyValueFound = false;
            CombatStrength? sum = default(CombatStrength);
            foreach (var cs in sequence) {
                if (cs.HasValue) {
                    isAnyValueFound = true;
                    sum += cs.Value;
                }
            }
            return isAnyValueFound ? sum : null;
        }

        /// <summary>
        /// Sums the sequence of CombatStrengths.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <returns></returns>
        public static CombatStrength Sum(this IEnumerable<CombatStrength> sequence) {
            CombatStrength sum = default(CombatStrength);
            foreach (var cs in sequence) {
                sum += cs;
            }
            return sum;
        }

        /// <summary>
        /// Returns the tag value used by the AStar Pathfinding system for this Topography. 
        /// <remarks>This is necessary in order to introduce Topography.None as the default
        /// value used for error detection. To be the default value, it must have an int value of 0.
        /// However, AStar uses the tag value 0 (aka x0000) when populating its tagPenaltyArray,
        /// done in FleetCmdItem.GenerateCourse(). Therefore, this method is necessary to get this
        /// tagValue from the Topography constant as (int)TopographyConstant would return 0 for None.
        /// </remarks>
        /// <remarks>In an earlier version AStar used
        /// this value to populate an array of penalty values associated with the BitMask
        /// version of the tag. i.e. SystemTagValue = 3, bitMaskVersion = x1000, aka <c>1 &lt;&lt; 3</c>.</remarks>
        /// </summary>
        /// <param name="topography">The topography.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static uint AStarTagValue(this Topography topography) {
            switch (topography) {
                case Topography.OpenSpace:
                    return 0;
                case Topography.Nebula:
                    return 1;
                case Topography.DeepNebula:
                    return 2;
                case Topography.System:
                    return 3;
                case Topography.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(topography));
            }
        }

        private static Color32 _magenta = Color.magenta;    // Hex #FF00FFFF
        private static Color32 _yellow = Color.yellow;      // Hex #FFFF00FF
        private static Color32 _aqua = Color.cyan;          // Hex #00FFFFFF
        private static Color32 _black = Color.black;        // Hex #000000FF
        private static Color32 _blue = Color.blue;          // Hex #0000FFFF
        private static Color32 _green = Color.green;        // Hex #008000FF
        private static Color32 _gray = Color.gray;          // Hex #808080FF
        private static Color32 _clear = Color.clear;        // Hex ?
        private static Color32 _red = Color.red;            // Hex #FF0000FF
        private static Color32 _white = Color.white;        // Hex #FFFFFFFF

        /// <summary>
        /// Dark green (0, 128, 0) in RGB.
        /// </summary>
        private static Color32 _darkGreen = new Color(0F, 0.5F, 0F, 1F);

        /// <summary>
        /// Purple color RGB (128, 0, 128), HtmlHexString "#800080FF"
        /// </summary>
        private static Color32 _purple = new Color(0.5F, 0F, 0.5F, 1F);

        /// <summary>
        /// My brown color, more commonly known as Peru. RGB (205, 133, 63) Hex #A52A2AFF?
        /// </summary>
        private static Color32 _brown = new Color(0.80F, 0.52F, 0.25F, 1F);

        /// <summary>
        /// Teal, aka Blue/Green RGB (0, 128, 128), HtmlHexString "008080FF".
        /// </summary>
        private static Color32 _teal = new Color(0F, 0.5F, 0.5F, 1F);

        private static Color32 _lime;
        private static Color32 _lightBlue;
        private static Color32 _olive;
        private static Color32 _orange;
        private static Color32 _silver;
        private static Color32 _darkBlue;
        private static Color32 _maroon;

        static GameExtensions() {
            _lightBlue = CreateColor("#ADD8E6FF");
            _lime = CreateColor("#00FF00FF");
            _olive = CreateColor("#808000FF");
            _orange = CreateColor("#FFA500FF");
            _silver = CreateColor("#C0C0C0FF");
            _darkBlue = CreateColor("#0000A0FF");
            _maroon = CreateColor("#800000FF");
        }

        private static Color32 CreateColor(string htmlColorString) {
            Color color;
            bool isCreated = ColorUtility.TryParseHtmlString(htmlColorString, out color);
            if (!isCreated) {
                D.Error("Color conversion of hex {0} failed.", htmlColorString);
            }
            return color;
        }

        /// <summary>
        /// Converts the provided GameColor to the UnityEngine.Color equivalent with
        /// the alpha channel value set to alpha. If gameColor is GameColor.Clear, the alpha
        /// value is ignored as Clear, by definition, has an alpha of 0.
        /// </summary>
        /// <param name="gameColor">Color of the game.</param>
        /// <param name="alpha">The optional alpha channel value.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static Color32 ToUnityColor(this GameColor gameColor, float alpha = 1F) {    // OPTIMIZE use Color32 
            Utility.ValidateForRange(alpha, Constants.ZeroF, Constants.OneF);
            Color32 color;
            switch (gameColor) {
                case GameColor.Aqua:
                    color = _aqua;
                    break;
                case GameColor.Black:
                    color = _black;
                    break;
                case GameColor.Blue:
                    color = _blue;
                    break;
                case GameColor.Brown:
                    color = _brown;
                    break;
                case GameColor.Clear:
                    return _clear; // simply return as Color.clear has alpha = 0
                case GameColor.DarkBlue:
                    color = _darkBlue;
                    break;
                case GameColor.DarkGreen:
                    color = _darkGreen;
                    break;
                case GameColor.Gray:
                    color = _gray;
                    break;
                case GameColor.Green:
                    color = _green;
                    break;
                case GameColor.LightBlue:
                    color = _lightBlue;
                    break;
                case GameColor.Lime:
                    color = _lime;
                    break;
                case GameColor.Magenta:
                    color = _magenta;
                    break;
                case GameColor.Maroon:
                    color = _maroon;
                    break;
                case GameColor.Olive:
                    color = _olive;
                    break;
                case GameColor.Orange:
                    color = _orange;
                    break;
                case GameColor.Purple:
                    color = _purple;
                    break;
                case GameColor.Red:
                    color = _red;
                    break;
                case GameColor.Silver:
                    color = _silver;
                    break;
                case GameColor.White:
                    color = _white;
                    break;
                case GameColor.Yellow:
                    color = _yellow;
                    break;
                case GameColor.Teal:
                    color = _teal;
                    break;
                case GameColor.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(gameColor));
            }
            if (alpha < Constants.OneF) {
                color.a = (byte)Mathf.RoundToInt(255 * alpha);
            }
            return color;
        }

        /// <summary>
        /// Embeds the Ngui-recognized Hex value equivalent value for <c>color</c> around this text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="color">The color.</param>
        /// <returns></returns>
        public static string SurroundWith(this string text, GameColor color) {
            string colorHex = GameUtility.ColorToHex(color);
            string colorNgui = UnityConstants.NguiEmbeddedColorFormat.Inject(colorHex);
            return colorNgui + text + UnityConstants.NguiEmbeddedColorTerminator;
        }

    }
}

