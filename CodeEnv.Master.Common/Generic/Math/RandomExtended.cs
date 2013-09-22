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

    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// ExtRandom is an extension of the Unity class Random. Its main purpose is to automate common operations desired
    /// when using the Random class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class RandomExtended<T> {

        /// <summary>
        /// This method sets a random seed for the RNG using 2 convoluted formulas. Using this method at any time other than during downtime is not recommended. 
        /// This method will never see use 99.9% of the time, but is available in cases where a truly random seed is required.
        /// </summary>
        public static void RandomizeSeed() {
            Random.seed = System.Math.Abs(((int)(System.DateTime.Now.Ticks % 2147483648L) - (int)(Time.realtimeSinceStartup + 2000f)) / ((int)System.DateTime.Now.Day - (int)System.DateTime.Now.DayOfWeek * System.DateTime.Now.DayOfYear));
            Random.seed = System.Math.Abs((int)((Random.value * (float)System.DateTime.Now.Ticks * (float)Random.Range(0, 2)) + (Random.value * Time.realtimeSinceStartup * Random.Range(1f, 3f))) + 1);
        }

        /// <summary>
        /// This method returns either true or false with an equal chance.
        /// </summary>
        /// <returns></returns>
        public static bool SplitChance() {
            return Random.Range(0, 2) == 0 ? true : false;
        }

        /// <summary>
        /// This method returns either true or false with the chance of the former derived from the parameters passed to the method.
        /// </summary>
        /// <param name="probabilityFactor">The probability factor.</param>
        /// <param name="probabilitySpace">The probability space.</param>
        /// <returns></returns>
        public static bool Chance(int probabilityFactor, int probabilitySpace) {
            return Random.Range(0, probabilitySpace) < probabilityFactor ? true : false;
        }

        /// <summary>
        ///This method returns a random element chosen from an array of elements.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns></returns>
        public static T Choice(T[] array) {
            return array[Random.Range(0, array.Length)];
        }

        /// <summary>
        /// This method returns a random element chosen from a list of elements.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns></returns>
        public static T Choice(IList<T> list) {
            return list[Random.Range(0, list.Count)];
        }

        /// <summary>
        /// This method returns a random element chosen from an array of elements based on the respective weights of the elements.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="weights">The weights.</param>
        /// <returns></returns>
        public static T WeightedChoice(T[] array, int[] weights) {
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
        /// <param name="list">The list.</param>
        /// <param name="weights">The weights.</param>
        /// <returns></returns>
        public static T WeightedChoice(IList<T> list, int[] weights) {
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
        /// <param name="array">The array.</param>
        /// <returns></returns>
        public static T[] Shuffle(T[] array) {
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
        /// <param name="list">The list.</param>
        /// <returns></returns>
        public static List<T> Shuffle(IList<T> list) {
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
    }
}

