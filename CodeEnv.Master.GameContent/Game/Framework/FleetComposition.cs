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
        /// The Categories of the Items present in this Composition.
        /// </summary>
        public IList<ShipCategory> Categories {
            get {
                return _composition.Keys.ToList();
            }
        }

        private IDictionary<ShipCategory, IList<ShipData>> _composition;

        public FleetComposition() {
            _composition = new SortedDictionary<ShipCategory, IList<ShipData>>();
        }

        /// <summary>
        /// Copy Constructor. Initializes a new instance of the <see cref="FleetComposition"/> class.
        /// </summary>
        /// <param name="compositionToCopy">The fleet composition to copy.</param>
        public FleetComposition(FleetComposition compositionToCopy) {
            _composition = compositionToCopy._composition;
            // UNCLEAR does fleetCompositionToCopy get collected by the garbage collector now?
        }


        public bool Add(ShipData elementData) {
            ShipCategory category = elementData.Category;
            if (!_composition.ContainsKey(category)) {
                _composition.Add(category, new List<ShipData>());
            }
            if (_composition[category].Contains(elementData)) {
                return false;
            }
            _composition[category].Add(elementData);
            return true;
        }

        public bool Remove(ShipData elementData) {
            ShipCategory category = elementData.Category;
            bool isRemoved = _composition[category].Remove(elementData);
            if (_composition[category].Count == Constants.Zero) {
                _composition.Remove(category);
            }
            return isRemoved;
        }

        public bool Contains(ShipData elementData) {
            ShipCategory category = elementData.Category;
            return _composition[category].Contains(elementData);
        }

        public IList<ShipData> GetData(ShipCategory category) {
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

