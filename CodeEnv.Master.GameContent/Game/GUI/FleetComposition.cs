// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetComposition.cs
// Immutable class holding the ShipCategory composition of a Fleet.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable class holding the ShipCategory composition of a Fleet.
    /// </summary>
    public class FleetComposition : AUnitComposition<ShipHullCategory> {

        public FleetComposition(IEnumerable<ShipHullCategory> nonUniqueUnitCategories) : base(nonUniqueUnitCategories) { }

        protected override string GetCategoryDescription(ShipHullCategory category) {
            return category.GetEnumAttributeText();
        }

    }
}

