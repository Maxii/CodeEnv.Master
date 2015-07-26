// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RangeDistanceCategoryExtensions.cs
// Extension methods for RangeDistanceCategory values.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    ///  Extension methods for RangeDistanceCategory values.
    /// </summary>
    public static class RangeDistanceCategoryExtensions {

        /// <summary>
        /// Gets the base weapon range distance (prior to any owner modifiers being applied) for this RangeDistanceCategory.
        /// </summary>
        /// <param name="rangeCategory">The weapon range category.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static float GetBaseWeaponRange(this RangeDistanceCategory rangeCategory) {
            switch (rangeCategory) {
                case RangeDistanceCategory.Short:
                    return 4F;
                case RangeDistanceCategory.Medium:
                    return 7F;
                case RangeDistanceCategory.Long:
                    return 10F;
                case RangeDistanceCategory.None:
                    return Constants.ZeroF;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCategory));
            }
        }

        /// <summary>
        /// Gets the base sensor range distance spread (prior to any player modifiers being applied) for this RangeDistanceCategory.
        /// Allows me to create different distance values for a specific RangeDistanceCategory.
        /// </summary>
        /// <param name="rangeCategory">The sensor range category.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static ValueRange<float> __GetBaseSensorRangeSpread(this RangeDistanceCategory rangeCategory) {
            switch (rangeCategory) {
                case RangeDistanceCategory.Short:
                    return new ValueRange<float>(100F, 200F);
                case RangeDistanceCategory.Medium:
                    return new ValueRange<float>(500F, 1000F);
                case RangeDistanceCategory.Long:
                    return new ValueRange<float>(2000F, 3000F);
                case RangeDistanceCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCategory));
            }
        }

    }
}

