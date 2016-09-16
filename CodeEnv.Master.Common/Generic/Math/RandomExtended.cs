// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RandomExtended.cs
//RandomExtended adds more functionality to Unity class Random.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Linq;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// RandomExtended adds more functionality to the Unity class Random.
    /// </summary>
    public static class RandomExtended {

        /// <summary>
        /// This method sets a random seed for the RNG using 2 convoluted formulas. Using this method at any time other than during downtime is not recommended. 
        /// This method will never see use 99.9% of the time, but is available in cases where a truly random seed is required.
        /// </summary>
        public static void RandomizeSeed() {
            //Random.seed = value... deprecated by Unity 5.4
            Random.InitState(System.Math.Abs(((int)(System.DateTime.Now.Ticks % 2147483648L) - (int)(Time.realtimeSinceStartup + 2000f)) / ((int)System.DateTime.Now.Day - (int)System.DateTime.Now.DayOfWeek * System.DateTime.Now.DayOfYear)));
            Random.InitState(System.Math.Abs((int)((Random.value * (float)System.DateTime.Now.Ticks * (float)Random.Range(0, 2)) + (Random.value * Time.realtimeSinceStartup * Random.Range(1f, 3f))) + 1));
        }

        /// <summary>
        /// Same as Random.Range, but the returned value is between min and max, inclusive.
        /// Unity's Random.Range does not include max as a possible outcome, unless min == max.
        /// This means Range(0,1) produces 0 instead of 0 or 1. That's unacceptable per ArenMook.
        /// </summary>
        public static int Range(int min, int max) {
            if (min == max) { return min; }
            return UnityEngine.Random.Range(min, max + 1);
        }

        /// <summary>
        /// The Random.Range method with a power modifier that skews the returned results.
        /// If power = 1F, results are the same as Random.Range(). If power = 2F, it increases
        /// the likelihood that a value closer to min will be returned. If power = 0.5F, it increases
        /// the likelihood that a value closer to max will be returned.
        /// <remarks>See http://forum.unity3d.com/threads/random-range-with-decreasing-probability.50596/ </remarks>
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="power">The power.</param>
        /// <returns></returns>
        public static float RangeSkewed(float min, float max, float power) {
            Utility.ValidateNotNegative(max - min);
            if (min == max) { return min; }
            return Mathf.Pow(Random.value, power) * (max - min) + min;
        }

        /// <summary>
        /// This method returns either true or false with an equal chance.
        /// </summary>
        /// <returns></returns>
        public static bool SplitChance() {
            return Range(0, 1) == 0 ? true : false;
        }

        /// <summary>
        /// This method returns either true or false with the chance of the former derived from the parameters passed to the method.
        /// Usage: result = RandomExtended.Chance(1, 9); // 10% chance of true
        /// </summary>
        /// <param name="probabilityFactor">The probability factor.</param>
        /// <param name="probabilitySpace">The probability space.</param>
        /// <returns></returns>
        public static bool Chance(int probabilityFactor, int probabilitySpace) {
            return Range(0, probabilitySpace) < probabilityFactor ? true : false;
        }

        /// <summary>
        /// Returns either true or false with the chance of a true return provided.
        /// </summary>
        /// <param name="truePercentage">The probability of true being returned.</param>
        /// <returns></returns>
        public static bool Chance(float truePercentage) {
            Utility.ValidateForRange(truePercentage, Constants.ZeroPercent, Constants.OneHundredPercent);
            return truePercentage >= UnityEngine.Random.Range(Constants.ZeroPercent, Constants.OneHundredPercent);
        }

        /// <summary>
        /// Returns either true or false with the chance of a true return (assuming power = 1F) provided.
        /// If power is 2F, the chance of a true return is reduced, if 0.5F, the chance of a true return is increased.
        /// </summary>
        /// <param name="truePercentage">The true percentage.</param>
        /// <param name="power">The power.</param>
        /// <returns></returns>
        public static bool ChanceSkewed(float truePercentage, float power) {
            Utility.ValidateForRange(truePercentage, Constants.ZeroPercent, Constants.OneHundredPercent);
            return truePercentage >= RangeSkewed(Constants.ZeroPercent, Constants.OneHundredPercent, power);
        }

        /// <summary>
        /// This method returns a random element chosen from an IEnumerable of elements.
        /// Cannot be empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <returns></returns>
        public static T Choice<T>(IEnumerable<T> collection) {
            return collection.ToArray<T>()[Random.Range(0, collection.Count())];
        }

        /// <summary>
        /// This method returns a random element chosen from an array of elements based on the respective weights of the elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array">The array.</param>
        /// <param name="weights">The weights.</param>
        /// <returns></returns>
        public static T WeightedChoice<T>(T[] array, int[] weights) {
            int totalWeight = 0;
            for (int i = 0; i < array.Length; i++) {
                totalWeight += weights[i];
            }
            int choiceIndex = Random.Range(0, totalWeight);
            for (int i = 0; i < array.Length; i++) {
                if (choiceIndex < weights[i]) {
                    choiceIndex = i;
                    break;
                }
                choiceIndex -= weights[i];
            }

            return array[choiceIndex];
        }

        /// <summary>
        /// This method returns a random element chosen from a list of elements based on the respective weights of the elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="weights">The weights.</param>
        /// <returns></returns>
        public static T WeightedChoice<T>(IList<T> list, int[] weights) {
            int totalWeight = 0;
            for (int i = 0; i < list.Count; i++) {
                totalWeight += weights[i];
            }
            int choiceIndex = Random.Range(0, totalWeight);
            for (int i = 0; i < list.Count; i++) {
                if (choiceIndex < weights[i]) {
                    choiceIndex = i;
                    break;
                }
                choiceIndex -= weights[i];
            }

            return list[choiceIndex];
        }

        /// <summary>
        /// This method rearranges the elements of an array randomly and returns the rearranged array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array">The array.</param>
        /// <returns></returns>
        public static T[] Shuffle<T>(T[] array) {
            T[] shuffledArray = new T[array.Length];
            List<int> elementIndices = new List<int>(0);
            for (int i = 0; i < array.Length; i++) {
                elementIndices.Add(i);
            }
            int arrayIndex;
            for (int i = 0; i < array.Length; i++) {
                arrayIndex = elementIndices[Random.Range(0, elementIndices.Count)];
                shuffledArray[i] = array[arrayIndex];
                elementIndices.Remove(arrayIndex);
            }

            return shuffledArray;
        }

        /// <summary>
        /// This method rearranges the elements of a list randomly and returns the rearranged list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <returns></returns>
        public static List<T> Shuffle<T>(IList<T> list) {
            List<T> shuffledList = new List<T>(0);
            int listCount = list.Count;
            int elementIndex;
            for (int i = 0; i < listCount; i++) {
                elementIndex = Random.Range(0, list.Count);
                shuffledList.Add(list[elementIndex]);
                list.RemoveAt(elementIndex);
            }

            return shuffledList;
        }

        /// <summary>
        /// Get a random point on a circle.
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <returns></returns>
        public static Vector2 PointOnCircle(float radius) {
            float randomAngle = Random.Range(0F, 360F);
            float angleRadians = randomAngle * Mathf.PI / 180F;
            float x = radius * Mathf.Cos(angleRadians);
            float y = radius * Mathf.Sin(angleRadians);
            return new Vector2(x, y);
        }
    }
}

