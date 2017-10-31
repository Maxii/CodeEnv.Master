// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitComposition.cs
// Immutable abstract generic class holding the<c>ElementHullCategoryType</c> composition of a Unit.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Immutable abstract class holding the<c>ElementHullCategoryType</c> composition of a Unit.
    /// <remarks>OPTIMIZE Use of ElementHullCategoryType and .ToDictionary doesn't allow use of a
    /// ElementHullCategoryType EqualityComparer when constructing a Dictionary that doesn't produce allocations.</remarks>
    /// </summary>
    /// <typeparam name="ElementHullCategoryType">The type of ElementCategory the Unit is composed of.</typeparam>
    public abstract class AUnitComposition<ElementHullCategoryType> where ElementHullCategoryType : struct {

        private static StringBuilder _stringBuilder = new StringBuilder();

        private string _debugName;
        public string DebugName {
            get {
                _debugName = _debugName ?? ConstructDebugName();
                return _debugName;
            }
        }

        private IDictionary<ElementHullCategoryType, int> _categoryCountLookup;

        /// <summary>
        /// Initializes a new instance of the <see cref="AUnitComposition" /> class.
        /// Note: Provide the category associated with each and every element of the Unit that you want this
        /// Composition to be aware of - aka due to IntelCoverage limits, not all players should be aware
        /// of the entire composition of a unit.
        /// </summary>
        /// <param name="nonUniqueUnitCategories">The non-unique categories present in the Unit.</param>
        public AUnitComposition(IEnumerable<ElementHullCategoryType> nonUniqueUnitCategories) {
            PopulateLookup(nonUniqueUnitCategories);
        }

        private void PopulateLookup(IEnumerable<ElementHullCategoryType> unitFacilityCategories) {
            _categoryCountLookup = unitFacilityCategories.GroupBy(c => c).ToDictionary(group => group.Key, group => group.Count());
        }

        /// <summary>
        /// Returns a copy of the unique <c>ElementHullCategoryType</c>'s this unit is composed of.
        /// </summary>
        /// <returns></returns>
        public IList<ElementHullCategoryType> GetUniqueElementCategories() {
            return new List<ElementHullCategoryType>(_categoryCountLookup.Keys);
        }

        /// <summary>
        /// Gets the count of elements of this <c>elementCategory</c> present in the Unit.
        /// </summary>
        /// <param name="elementCategory">The element category.</param>
        /// <returns></returns>
        public int GetElementCount(ElementHullCategoryType elementCategory) {
            return _categoryCountLookup[elementCategory];
        }

        /// <summary>
        /// Gets the total count of elements present in the Unit.
        /// </summary>
        /// <returns></returns>
        public int GetTotalElementsCount() {
            return _categoryCountLookup.Values.Sum();
        }

        private string ConstructDebugName() {
            _stringBuilder.Clear();
            var uniqueCategories = _categoryCountLookup.Keys.ToList();
            foreach (var cat in uniqueCategories) {
                int count = _categoryCountLookup[cat];
                _stringBuilder.AppendFormat("{0}(", GetCategoryDescription(cat));
                _stringBuilder.AppendFormat(Constants.FormatInt_1DMin, count);
                if (!cat.Equals(uniqueCategories.First()) && !cat.Equals(uniqueCategories.Last())) {
                    _stringBuilder.AppendFormat("), ");
                    continue;
                }
                _stringBuilder.Append(")");
            }
            return _stringBuilder.ToString();
        }

        protected abstract string GetCategoryDescription(ElementHullCategoryType category);

        public sealed override string ToString() {
            return DebugName;
        }

    }
}

