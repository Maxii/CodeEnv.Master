// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetComposition.cs
// Wrapper for Fleet Composition dictionary containing shipData.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Linq;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Wrapper for Fleet Composition dictionary containing shipData.
    /// </summary>
    public class FleetComposition {

        /// <summary>
        /// The Categories of the Elements present in this Composition.
        /// </summary>
        public IList<ShipHullCategory> Categories {
            get {
                return _composition.Keys.ToList();
            }
        }

        public int ElementCount { get { return GetAllData().Count(); } }

        private IDictionary<ShipHullCategory, IList<ShipData>> _composition;

        public FleetComposition() {
            _composition = new SortedDictionary<ShipHullCategory, IList<ShipData>>();
        }

        public bool Add(ShipData elementData) {
            ShipHullCategory category = elementData.HullCategory;
            D.Assert(category != default(ShipHullCategory), "{0}.Category is {1}.".Inject(elementData.FullName, default(ShipHullCategory).GetValueName()));
            if (!_composition.ContainsKey(category)) {
                _composition.Add(category, new List<ShipData>());
            }
            if (_composition[category].Contains(elementData)) {
                D.Warn("{0} already contains Data for {1}.", GetType().Name, elementData.Name);
                return false;
            }
            _composition[category].Add(elementData);
            return true;
        }

        public bool Remove(ShipData elementData) {
            ShipHullCategory category = elementData.HullCategory;
            D.Assert(category != default(ShipHullCategory), "{0}.Category is {1}.".Inject(elementData.FullName, default(ShipHullCategory).GetValueName()));
            bool isRemoved = _composition[category].Remove(elementData);
            if (_composition[category].Count == Constants.Zero) {
                _composition.Remove(category);
            }
            return isRemoved;
        }

        public bool Contains(ShipData elementData) {
            ShipHullCategory category = elementData.HullCategory;
            return _composition[category].Contains(elementData);
        }

        public IList<ShipData> GetData(ShipHullCategory category) {
            return _composition[category];
        }

        public IEnumerable<ShipData> GetAllData() {
            IEnumerable<ShipData> allData = new List<ShipData>();
            foreach (var hull in Categories) {
                allData = allData.Concat(GetData(hull));
            }
            return allData;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

