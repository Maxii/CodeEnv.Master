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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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
            foreach (var a in sequence) {
                if (a.HasValue) {
                    isAnyValueFound = true;
                    sum += a.Value;
                }
            }
            return isAnyValueFound ? sum : null;
        }

        /// <summary>
        /// Returns the tag value used by the AStar Pathfinding system. AStar uses
        /// this value to populate an array of penalty values associated with the BitMask
        /// version of the tag. i.e. SystemTagValue = 3, bitMaskVersion = x1000, aka <c>1 << 3</c>.
        ///Note: This is necessary in order to introduce Topography.None as the default
        ///value used for error detection. To be the default value, it must have an int value of 0.
        ///However, AStar uses the tag value 0 (aka x0000) when populating its tagPenaltyArray,
        ///done in FleetCmdItem.GenerateCourse(). Therefore, this method is necessary to get this
        ///tagValue from the Topography constant as (int)TopographyConstant would return
        ///0 for None.
        /// </summary>
        /// <param name="topography">The topography.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static int AStarTagValue(this Topography topography) {
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

        public static Color ToUnityColor(this GameColor color) {
            switch (color) {
                case GameColor.Black:
                    return Color.black;
                case GameColor.Blue:
                    return Color.blue;
                case GameColor.Cyan:
                    return Color.cyan;
                case GameColor.Green:
                    return Color.green;
                case GameColor.Gray:
                    return Color.gray;
                case GameColor.Clear:
                    return Color.clear;
                case GameColor.Magenta:
                    return Color.magenta;
                case GameColor.Red:
                    return Color.red;
                case GameColor.White:
                    return Color.white;
                case GameColor.Yellow:
                    return Color.yellow;
                case GameColor.None:
                    return Color.white;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(color));
            }
        }

        /// <summary>
        /// Embeds the Ngui-recognized Hex value equivalent value for <c>color</c> around this text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="color">The color.</param>
        /// <returns></returns>
        public static string EmbedColor(this string text, GameColor color) {
            string colorHex = GameUtility.ColorToHex(color);
            string colorNgui = UnityConstants.NguiEmbeddedColorFormat.Inject(colorHex);
            return colorNgui + text + UnityConstants.NguiEmbeddedColorTerminator;
        }

    }
}

