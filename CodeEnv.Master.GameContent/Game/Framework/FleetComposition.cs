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
    public class FleetComposition : AComposition<ShipCategory, ShipData> {
        //public class FleetComposition {

        ///// <summary>
        ///// The Categories of the Items present in this Composition.
        ///// </summary>
        //public IList<ShipCategory> Categories {
        //    get {
        //        return _composition.Keys.ToList();
        //    }
        //}

        //private IDictionary<ShipCategory, IList<ShipData>> _composition;

        //public FleetComposition() {
        //    _composition = new SortedDictionary<ShipCategory, IList<ShipData>>();
        //}

        public FleetComposition() : base() { }

        /// <summary>
        /// Copy Constructor. Initializes a new instance of the <see cref="FleetComposition"/> class.
        /// </summary>
        /// <param name="compositionToCopy">The fleet composition to copy.</param>
        public FleetComposition(FleetComposition compositionToCopy)
            : base(compositionToCopy) {
            //_composition = compositionToCopy._composition;
            // UNCLEAR does fleetCompositionToCopy get collected by the garbage collector now?
        }

        ///// <summary>
        ///// Copy Constructor. Initializes a new instance of the <see cref="FleetComposition"/> class.
        ///// </summary>
        ///// <param name="compositionToCopy">The fleet composition to copy.</param>
        //public FleetComposition(FleetComposition compositionToCopy) {
        //    _composition = compositionToCopy._composition;
        //    // UNCLEAR does fleetCompositionToCopy get collected by the garbage collector now?
        //}


        //public bool Add(ShipData data) {
        //    ShipCategory hull = data.Category;
        //    if (!_composition.ContainsKey(hull)) {
        //        _composition.Add(hull, new List<ShipData>());
        //    }
        //    if (_composition[hull].Contains(data)) {
        //        return false;
        //    }
        //    _composition[hull].Add(data);
        //    return true;
        //}

        //public bool Remove(ShipData data) {
        //    ShipCategory hull = data.Category;
        //    bool isRemoved = _composition[hull].Remove(data);
        //    if (_composition[hull].Count == Constants.Zero) {
        //        _composition.Remove(hull);
        //    }
        //    return isRemoved;
        //}

        //public bool Contains(ShipData data) {
        //    ShipCategory hull = data.Category;
        //    return _composition[hull].Contains(data);
        //}

        //public IList<ShipData> GetData(ShipCategory hull) {
        //    return _composition[hull];
        //}

        //public IEnumerable<ShipData> GetAllData() {
        //    IEnumerable<ShipData> allData = new List<ShipData>();
        //    foreach (var hull in Categories) {
        //        allData = allData.Concat(GetData(hull));
        //    }
        //    return allData;
        //}

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

