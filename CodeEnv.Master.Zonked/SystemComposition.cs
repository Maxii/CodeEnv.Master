// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemComposition.cs
// Wrapper for System Composition dictionary containing planet and star Data.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Wrapper for System Composition dictionary containing planet and star Data.
    /// </summary>
    [Obsolete]
    public class SystemComposition {
        // SystemComposition no longer contains any SettlementData or Orbit info

        public StarItemData StarData { get; set; }

        public IList<PlanetoidCategory> PlanetCategories {
            get {
                return _composition.Keys.ToList();
            }
        }

        private IDictionary<PlanetoidCategory, IList<PlanetoidItemData>> _composition;

        public SystemComposition() {
            _composition = new SortedDictionary<PlanetoidCategory, IList<PlanetoidItemData>>();
        }

        public bool AddPlanet(PlanetoidItemData data) {
            PlanetoidCategory pType = data.Category;
            if (!_composition.ContainsKey(pType)) {
                _composition.Add(pType, new List<PlanetoidItemData>());
            }
            if (_composition[pType].Contains(data)) {
                return false;
            }
            _composition[pType].Add(data);
            return true;
        }

        public bool RemovePlanet(PlanetoidItemData data) {
            PlanetoidCategory pType = data.Category;
            bool isRemoved = _composition[pType].Remove(data);
            if (_composition[pType].Count == Constants.Zero) {
                _composition.Remove(pType);
            }
            return isRemoved;
        }

        public bool ContainsPlanet(PlanetoidItemData data) {
            PlanetoidCategory hull = data.Category;
            return _composition[hull].Contains(data);
        }

        public IList<PlanetoidItemData> GetPlanetData(PlanetoidCategory pType) {
            return _composition[pType];
        }

        public IEnumerable<PlanetoidItemData> GetPlanetData() {
            IEnumerable<PlanetoidItemData> allData = new List<PlanetoidItemData>();
            foreach (var pType in PlanetCategories) {
                allData = allData.Concat(GetPlanetData(pType));
            }
            //D.Log("PlanetData count = {0}.", allData.Count());
            return allData;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

