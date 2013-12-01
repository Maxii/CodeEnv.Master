// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetComposition.cs
//  Wrapper for Fleet Composition dictionary containing shipData.
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

        public IList<ShipHull> Hulls {
            get {
                return _composition.Keys.ToList();
            }
        }

        private IDictionary<ShipHull, IList<ShipData>> _composition;

        public FleetComposition() {
            _composition = new SortedDictionary<ShipHull, IList<ShipData>>();
        }

        /// <summary>
        /// Copy Constructor. Initializes a new instance of the <see cref="FleetComposition"/> class.
        /// </summary>
        /// <param name="compositionToCopy">The fleet composition to copy.</param>
        public FleetComposition(FleetComposition compositionToCopy) {
            _composition = compositionToCopy._composition;
            // UNCLEAR does fleetCompositionToCopy get collected by the garbage collector now?
        }

        public bool AddShip(ShipData data) {
            ShipHull hull = data.Hull;
            if (!_composition.ContainsKey(hull)) {
                _composition.Add(hull, new List<ShipData>());
            }
            if (_composition[hull].Contains(data)) {
                return false;
            }
            _composition[hull].Add(data);
            return true;
        }

        public bool RemoveShip(ShipData data) {
            ShipHull hull = data.Hull;
            bool isRemoved = _composition[hull].Remove(data);
            if (_composition[hull].Count == Constants.Zero) {
                _composition.Remove(hull);
            }
            return isRemoved;
        }

        public bool ContainsShip(ShipData data) {
            ShipHull hull = data.Hull;
            return _composition[hull].Contains(data);
        }

        public IList<ShipData> GetShipData(ShipHull hull) {
            return _composition[hull];
        }

        public IEnumerable<ShipData> GetShipData() {
            IEnumerable<ShipData> allData = new List<ShipData>();
            foreach (var hull in Hulls) {
                allData = allData.Concat(GetShipData(hull));
            }
            return allData;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

