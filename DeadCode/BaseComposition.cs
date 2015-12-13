// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BaseComposition.cs
// Wrapper for Starbase and Settlement Composition dictionary containing FacilityData.
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
    /// Wrapper for Starbase and Settlement Composition dictionary containing FacilityData.
    /// </summary>
    public class BaseComposition {

        /// <summary>
        /// The Categories of the Elements present in this Composition.
        /// </summary>
        public IList<FacilityHullCategory> Categories {
            get {
                return _composition.Keys.ToList();
            }
        }

        public int ElementCount { get { return GetAllData().Count(); } }

        private IDictionary<FacilityHullCategory, IList<FacilityData>> _composition;

        public BaseComposition() {
            _composition = new SortedDictionary<FacilityHullCategory, IList<FacilityData>>();
        }

        public bool Add(FacilityData elementData) {
            FacilityHullCategory category = elementData.HullCategory;
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
            FacilityHullCategory category = elementData.HullCategory;
            bool isRemoved = _composition[category].Remove(elementData);
            if (_composition[category].Count == Constants.Zero) {
                _composition.Remove(category);
            }
            return isRemoved;
        }

        public bool Contains(FacilityData elementData) {
            FacilityHullCategory category = elementData.HullCategory;
            return _composition[category].Contains(elementData);
        }

        public IList<FacilityData> GetData(FacilityHullCategory category) {
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

