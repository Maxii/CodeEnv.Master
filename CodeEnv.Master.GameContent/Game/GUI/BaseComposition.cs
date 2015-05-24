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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable class holding the FacilityCategory composition of a BaseUnit (Starbase or Settlement).
    /// </summary>
    public class BaseComposition : AUnitComposition<FacilityCategory> {

        public BaseComposition(IEnumerable<FacilityCategory> nonUniqueUnitCategories) : base(nonUniqueUnitCategories) { }

        protected override string GetCategoryDescription(FacilityCategory category) {
            return category.GetEnumAttributeText();
        }

    }
}

