// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetComposition.cs
// Wrapper for Fleet Composition dictionary.
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
    /// Wrapper for Fleet Composition dictionary.
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
        /// <param name="fleetCompositionToCopy">The fleet composition to copy.</param>
        public FleetComposition(FleetComposition fleetCompositionToCopy) {
            _composition = fleetCompositionToCopy._composition;
            // UNCLEAR does fleetCompositionToCopy get collected by the garbage collector now?
        }

        public bool AddShip(ShipData shipData) {
            ShipHull hull = shipData.Hull;
            if (!_composition.ContainsKey(hull)) {
                _composition.Add(hull, new List<ShipData>());
            }
            if (_composition[hull].Contains(shipData)) {
                return false;
            }
            _composition[hull].Add(shipData);
            return true;
        }

        public bool RemoveShip(ShipData shipData) {
            ShipHull hull = shipData.Hull;
            bool isRemoved = _composition[hull].Remove(shipData);
            if (_composition[hull].Count == Constants.Zero) {
                _composition[hull] = null;
            }
            return isRemoved;
        }

        public bool ContainsShip(ShipData shipData) {
            ShipHull hull = shipData.Hull;
            return _composition[hull].Contains(shipData);
        }

        public IList<ShipData> GetShipData(ShipHull hull) {
            return _composition[hull];
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

