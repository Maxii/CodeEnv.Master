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

        /// <summary>
        /// Calculates the combined sensor range (distance) from the provided sensors that are operational. 
        /// The algorithm used takes the sensor with the longest range and adds the Sqrt of the range of each of the remaining sensors.
        /// The provided sensors must have the same DistanceRange value.
        /// </summary>
        /// <param name="sensors">The sensors.</param>
        /// <returns></returns>
        public static float CalcSensorRangeDistance(this IEnumerable<Sensor> sensors) {
            D.Assert(sensors.Select(s => s.RangeCategory).Distinct().Count() <= Constants.One); // validate all the same DistanceRange
            if (!sensors.Any()) {
                return Constants.ZeroF;
            }
            var operationalSensors = sensors.Where(s => s.IsOperational);
            if (!operationalSensors.Any()) {
                return Constants.ZeroF;
            }
            Sensor longestRangeSensor = operationalSensors.MaxBy(s => s.RangeDistance);
            var remainingSensors = operationalSensors.Except(longestRangeSensor);
            return longestRangeSensor.RangeDistance + remainingSensors.Sum(s => Mathf.Sqrt(s.RangeDistance));
        }

        /// <summary>
        /// My brown color, more commonly known as Peru (205, 133, 63) in RGB.
        /// </summary>
        private static Color _brown = new Color(0.80F, 0.52F, 0.25F, 1F);

        /// <summary>
        /// Purple color (128, 0, 128) in RGB.
        /// </summary>
        private static Color _purple = new Color(0.5F, 0F, 0.5F, 1F);

        /// <summary>
        /// Dark green (0, 128, 0) in RGB.
        /// </summary>
        private static Color _darkGreen = new Color(0F, 0.5F, 0F, 1F);

        /// <summary>
        /// Teal, aka Blue/Green (0, 128, 128) in RGB.
        /// </summary>
        private static Color _teal = new Color(0F, 0.5F, 0.5F, 1F);

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
                case GameColor.Brown:
                    return _brown;
                case GameColor.Purple:
                    return _purple;
                case GameColor.DarkGreen:
                    return _darkGreen;
                case GameColor.Teal:
                    return _teal;
                case GameColor.None:
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
        public static string SurroundWith(this string text, GameColor color) {
            string colorHex = GameUtility.ColorToHex(color);
            string colorNgui = UnityConstants.NguiEmbeddedColorFormat.Inject(colorHex);
            return colorNgui + text + UnityConstants.NguiEmbeddedColorTerminator;
        }

    }
}

