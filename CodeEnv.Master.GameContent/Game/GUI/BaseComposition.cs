// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BaseComposition.cs
// Immutable class holding the FacilityCategory composition of a BaseUnit (Starbase or Settlement).
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable class holding the FacilityCategory composition of a BaseUnit (Starbase or Settlement).
    /// </summary>
    public class BaseComposition : AUnitComposition<FacilityHullCategory> {

        public BaseComposition(IEnumerable<FacilityHullCategory> nonUniqueUnitCategories) : base(nonUniqueUnitCategories) { }

        protected override string GetCategoryDescription(FacilityHullCategory category) {
            return category.GetEnumAttributeText();
        }

    }
}

