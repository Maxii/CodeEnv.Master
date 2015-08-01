// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RangeCategoryExtensions.cs
// Extension methods for RangeCategory values.
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
    ///  Extension methods for RangeCategory values.
    /// </summary>
    public static class RangeCategoryExtensions {

        /// <summary>
        /// Gets the base weapon range distance (prior to any owner modifiers being applied) for this RangeCategory.
        /// </summary>
        /// <param name="rangeCategory">The weapon range category.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static float GetBaseWeaponRange(this RangeCategory rangeCategory) {
            switch (rangeCategory) {
                case RangeCategory.Short:
                    return 4F;
                case RangeCategory.Medium:
                    return 7F;
                case RangeCategory.Long:
                    return 10F;
                case RangeCategory.None:
                    return Constants.ZeroF;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCategory));
            }
        }

        /// <summary>
        /// Gets the base sensor range distance spread (prior to any player modifiers being applied) for this RangeCategory.
        /// Allows me to create different distance values for a specific RangeCategory.
        /// </summary>
        /// <param name="rangeCategory">The sensor range category.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static ValueRange<float> __GetBaseSensorRangeSpread(this RangeCategory rangeCategory) {
            switch (rangeCategory) {
                case RangeCategory.Short:
                    return new ValueRange<float>(100F, 200F);
                case RangeCategory.Medium:
                    return new ValueRange<float>(500F, 1000F);
                case RangeCategory.Long:
                    return new ValueRange<float>(2000F, 3000F);
                case RangeCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCategory));
            }
        }

        /// <summary>
        /// Gets the base active countermeasure range distance (prior to any owner modifiers being applied) for this RangeCategory.
        /// </summary>
        /// <param name="rangeCategory">The countermeasure range category.</param>
        /// <returns></returns>
        public static float GetBaseActiveCountermeasureRange(this RangeCategory rangeCategory) {
            return rangeCategory.GetBaseWeaponRange() / 2F;
        }

    }
}

