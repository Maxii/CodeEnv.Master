// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LayerMaskExtensions.cs
// Extension access to manipulating and debugging LayerMasks.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Extension access to manipulating and debugging LayerMasks.
    /// </summary>
    public static class LayerMaskExtensions {

        /// <summary>
        /// Creates a LayerMask that includes only the provided layers.
        /// </summary>
        /// <param name="layers">The layers.</param>
        /// <returns></returns>
        public static LayerMask CreateInclusiveMask(params Layers[] layers) {
            return LayersToMask(layers);
        }

        /// <summary>
        /// Creates a LayerMask that includes all Layers except those provided.
        /// </summary>
        /// <param name="layers">The layers.</param>
        /// <returns></returns>
        public static LayerMask CreateExclusiveMask(params Layers[] layers) {
            return LayersToMask(layers).Inverse();
        }

        private static LayerMask Create(params string[] layerNames) {
            return NamesToMask(layerNames);
        }

        private static LayerMask Create(params int[] layerNumbers) {
            return LayerNumbersToMask(layerNumbers);
        }

        private static LayerMask NamesToMask(params string[] layerNames) {
            LayerMask ret = (LayerMask)0;
            foreach (var name in layerNames) {
                ret |= (1 << LayerMask.NameToLayer(name));
            }
            return ret;
        }

        private static LayerMask LayerNumbersToMask(params int[] layerNumbers) {
            LayerMask ret = (LayerMask)0;
            foreach (var layer in layerNumbers) {
                ret |= (1 << layer);
            }
            return ret;
        }

        private static LayerMask LayersToMask(params Layers[] layers) {
            LayerMask ret = (LayerMask)(int)Layers.Default;
            foreach (var layer in layers) {
                ret |= (1 << (int)layer);
            }
            return ret;
        }

        /// <summary>
        /// Inverts this LayerMask.
        /// </summary>
        /// <param name="original">The original.</param>
        /// <returns></returns>
        public static LayerMask Inverse(this LayerMask original) {
            return ~original;
        }

        /// <summary>
        /// Returns a new LayerMask with the provided Layers added.
        /// </summary>
        /// <param name="original">The original.</param>
        /// <param name="layers">The layers.</param>
        /// <returns></returns>
        public static LayerMask AddToMask(this LayerMask original, params Layers[] layers) {
            return original | LayersToMask(layers);
        }

        private static LayerMask AddToMask(this LayerMask original, params string[] layerNames) {
            return original | NamesToMask(layerNames);
        }

        /// <summary>
        /// Returns a new LayerMask with the provided Layers removed.
        /// </summary>
        /// <param name="original">The original.</param>
        /// <param name="layers">The layers.</param>
        /// <returns></returns>
        public static LayerMask RemoveFromMask(this LayerMask original, params Layers[] layers) {
            LayerMask invertedOriginal = ~original;
            return ~(invertedOriginal | LayersToMask(layers));
        }

        private static LayerMask RemoveFromMask(this LayerMask original, params string[] layerNames) {
            LayerMask invertedOriginal = ~original;
            return ~(invertedOriginal | NamesToMask(layerNames));
        }

        private static string[] MaskToNames(this LayerMask original) {
            var output = new List<string>();

            for (int i = 0; i < 32; ++i) {
                int shifted = 1 << i;
                if ((original & shifted) == shifted) {
                    string layerName = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(layerName)) {
                        output.Add(layerName);
                    }
                }
            }
            return output.ToArray();
        }

        /// <summary>
        /// Converts a LayerMask to a string separated by commas.
        /// </summary>
        /// <param name="original">The original.</param>
        /// <returns></returns>
        public static string MaskToString(this LayerMask original) {
            return MaskToString(original, ", ");
        }

        /// <summary>
        /// Converts a LayerMask to a string separated by the provided delimiter.
        /// </summary>
        /// <param name="original">The original.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <returns></returns>
        public static string MaskToString(this LayerMask original, string delimiter) {
            return string.Join(delimiter, MaskToNames(original));
        }

    }
}

