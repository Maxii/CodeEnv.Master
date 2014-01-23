// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseComposition.cs
// Wrapper for Starbase Composition dictionary containing FacilityData.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Wrapper for Starbase Composition dictionary containing FacilityData.
    /// </summary>
    public class StarbaseComposition {

        /// <summary>
        /// The Categories of the Items present in this Composition.
        /// </summary>
        public IList<FacilityCategory> Categories {
            get {
                return _composition.Keys.ToList();
            }
        }

        private IDictionary<FacilityCategory, IList<FacilityData>> _composition;

        public StarbaseComposition() {
            _composition = new SortedDictionary<FacilityCategory, IList<FacilityData>>();
        }

        /// <summary>
        /// Copy Constructor. Initializes a new instance of the <see cref="StarbaseComposition"/> class.
        /// </summary>
        /// <param name="compositionToCopy">The StarbaseComposition to copy.</param>
        public StarbaseComposition(StarbaseComposition compositionToCopy) {
            _composition = compositionToCopy._composition;
            // UNCLEAR does compositionToCopy get collected by the garbage collector now?
        }

        public bool Add(FacilityData elementData) {
            FacilityCategory category = elementData.Category;
            if (!_composition.ContainsKey(category)) {
                _composition.Add(category, new List<FacilityData>());
            }
            if (_composition[category].Contains(elementData)) {
                return false;
            }
            _composition[category].Add(elementData);
            return true;
        }

        public bool Remove(FacilityData elementData) {
            FacilityCategory category = elementData.Category;
            bool isRemoved = _composition[category].Remove(elementData);
            if (_composition[category].Count == Constants.Zero) {
                _composition.Remove(category);
            }
            return isRemoved;
        }

        public bool Contains(FacilityData elementData) {
            FacilityCategory category = elementData.Category;
            return _composition[category].Contains(elementData);
        }

        public IList<FacilityData> GetData(FacilityCategory category) {
            return _composition[category];
        }

        public IEnumerable<FacilityData> GetAllData() {
            IEnumerable<FacilityData> allData = new List<FacilityData>();
            foreach (var category in Categories) {
                allData = allData.Concat(GetData(category));
            }
            return allData;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

