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
    public class StarbaseComposition : AComposition<FacilityCategory, FacilityData> {
        //public class StarbaseComposition {

        ///// <summary>
        ///// The Categories of the Items present in this Composition.
        ///// </summary>
        //public IList<FacilityCategory> Categories {
        //    get {
        //        return _composition.Keys.ToList();
        //    }
        //}

        //private IDictionary<FacilityCategory, IList<FacilityData>> _composition;

        //public StarbaseComposition() {
        //    _composition = new SortedDictionary<FacilityCategory, IList<FacilityData>>();
        //}

        public StarbaseComposition() : base() { }

        public StarbaseComposition(StarbaseComposition compositionToCopy) : base(compositionToCopy) { }

        ///// <summary>
        ///// Copy Constructor. Initializes a new instance of the <see cref="StarbaseComposition"/> class.
        ///// </summary>
        ///// <param name="compositionToCopy">The StarbaseComposition to copy.</param>
        //public StarbaseComposition(StarbaseComposition compositionToCopy) {
        //    _composition = compositionToCopy._composition;
        //    // UNCLEAR does compositionToCopy get collected by the garbage collector now?
        //}

        //public bool Add(FacilityData itemData) {
        //    FacilityCategory type = itemData.Category;
        //    if (!_composition.ContainsKey(type)) {
        //        _composition.Add(type, new List<FacilityData>());
        //    }
        //    if (_composition[type].Contains(itemData)) {
        //        return false;
        //    }
        //    _composition[type].Add(itemData);
        //    return true;
        //}

        //public bool Remove(FacilityData itemData) {
        //    FacilityCategory type = itemData.Category;
        //    bool isRemoved = _composition[type].Remove(itemData);
        //    if (_composition[type].Count == Constants.Zero) {
        //        _composition.Remove(type);
        //    }
        //    return isRemoved;
        //}

        //public bool Contains(FacilityData itemData) {
        //    FacilityCategory type = itemData.Category;
        //    return _composition[type].Contains(itemData);
        //}

        //public IList<FacilityData> GetData(FacilityCategory itemType) {
        //    return _composition[itemType];
        //}

        //public IEnumerable<FacilityData> GetAllData() {
        //    IEnumerable<FacilityData> allData = new List<FacilityData>();
        //    foreach (var type in Categories) {
        //        allData = allData.Concat(GetData(type));
        //    }
        //    return allData;
        //}

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

