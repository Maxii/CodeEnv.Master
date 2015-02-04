// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BaseUnitComposition.cs
// Immutable structure holding the FacilityCategory composition of a BaseUnit (Starbase or Settlement).
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Immutable structure holding the FacilityCategory composition of a BaseUnit (Starbase or Settlement).
    /// </summary>
    public struct BaseUnitComposition {

        private IDictionary<FacilityCategory, int> _categoryCountLookup;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseUnitComposition"/> struct.
        /// Note: Provide the category associated with each and every element of the Unit that you want this
        /// Composition to be aware of - aka due to IntelCoverage limits, not all players should be aware 
        /// of the entire composition of a unit.
        /// </summary>
        /// <param name="nonUniqueUnitCategories">The non-unique categories present in the Unit.</param>
        public BaseUnitComposition(IEnumerable<FacilityCategory> nonUniqueUnitCategories)
            : this() {
            PopulateLookup(nonUniqueUnitCategories);
        }

        private void PopulateLookup(IEnumerable<FacilityCategory> unitCategories) {
            _categoryCountLookup = unitCategories.GroupBy(c => c).ToDictionary(group => group.Key, group => group.Count());
        }

        /// <summary>
        /// Returns a copy of the unique FacilityCategories present in this Composition.
        /// </summary>
        /// <returns></returns>
        public IList<FacilityCategory> GetUniqueElementCategories() {
            return new List<FacilityCategory>(_categoryCountLookup.Keys);
        }

        public int GetElementCount(FacilityCategory elementCategory) {
            return _categoryCountLookup[elementCategory];
        }

        public int GetTotalElementsCount() {
            return _categoryCountLookup.Values.Sum();
        }

        private string ConstructStringRepresentation() {
            var sb = new StringBuilder();
            sb.Append(CommonTerms.Composition);
            sb.Append(": ");
            var uniqueCategories = _categoryCountLookup.Keys.ToList();
            foreach (var cat in uniqueCategories) {
                int count = _categoryCountLookup[cat];
                sb.AppendFormat("{0}[", cat.GetDescription());
                sb.AppendFormat(Constants.FormatInt_1DMin, count);
                if (cat != uniqueCategories.First() && cat != uniqueCategories.Last()) {
                    sb.AppendFormat("], ");
                    continue;
                }
                sb.Append("]");
            }
            return sb.ToString();
        }

        private string _toString;
        public override string ToString() {
            if (_toString.IsNullOrEmpty()) {
                _toString = ConstructStringRepresentation();
            }
            return _toString;
        }

    }

}

