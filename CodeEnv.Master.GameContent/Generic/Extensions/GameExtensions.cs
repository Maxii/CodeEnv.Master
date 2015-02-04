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

using System.Collections.Generic;
namespace CodeEnv.Master.GameContent {

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
        public static OpeYield? Sum(this OpeYield? source, params OpeYield?[] args) {
            var argList = new List<OpeYield?>(args);
            argList.Add(source);
            return argList.Sum();
        }

        /// <summary>
        /// Aggregates the nullable values provided and returns their addition-based sum. If one or more of these
        /// nullable values has no value (its null), it is excluded from the sum. If all values are null, the value returned is null.
        /// </summary>
        /// <param name="args">The nullable values.</param>
        /// <returns></returns>
        public static OpeYield? Sum(this IEnumerable<OpeYield?> args) {
            bool valueFound = false;
            OpeYield sum = default(OpeYield);
            foreach (var a in args) {
                if (a.HasValue) {
                    valueFound = true;
                    sum += a.Value;
                }
            }
            if (!valueFound) {
                return null;
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
        public static XYield? Sum(this XYield? source, params XYield?[] args) {
            var argList = new List<XYield?>(args);
            argList.Add(source);
            return argList.Sum();
        }

        /// <summary>
        /// Aggregates the nullable values provided and returns their addition-based sum. If one or more of these
        /// nullable values has no value (its null), it is excluded from the sum. If all values are null, the value returned is null.
        /// </summary>
        /// <param name="args">The nullable values.</param>
        /// <returns></returns>
        public static XYield? Sum(this IEnumerable<XYield?> args) {
            bool valueFound = false;
            XYield sum = default(XYield);
            foreach (var a in args) {
                if (a.HasValue) {
                    valueFound = true;
                    sum += a.Value;
                }
            }
            if (!valueFound) {
                return null;
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
        public static CombatStrength? Sum(this CombatStrength? source, params CombatStrength?[] args) {
            var argList = new List<CombatStrength?>(args);
            argList.Add(source);
            return argList.Sum();
        }

        /// <summary>
        /// Aggregates the nullable values provided and returns their addition-based sum. If one or more of these
        /// nullable values has no value (its null), it is excluded from the sum. If all values are null, the value returned is null.
        /// </summary>
        /// <param name="args">The nullable values.</param>
        /// <returns></returns>
        public static CombatStrength? Sum(this IEnumerable<CombatStrength?> args) {
            bool valueFound = false;
            CombatStrength sum = default(CombatStrength);
            foreach (var a in args) {
                if (a.HasValue) {
                    valueFound = true;
                    sum += a.Value;
                }
            }
            if (!valueFound) {
                return null;
            }
            return sum;
        }


    }
}

