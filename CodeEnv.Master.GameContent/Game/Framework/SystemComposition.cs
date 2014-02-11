// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemComposition.cs
// Wrapper for System Composition dictionary containing planet,
// star and settlement Data.
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
    /// Wrapper for System Composition dictionary containing planet,
    /// star and settlement Data.
    /// </summary>
    public class SystemComposition {

        private Vector3 _settlementOrbitSlot;
        /// <summary>
        ///  The orbital start position (in local space) of any current or
        /// future settlement. The transform holding the SettlementCreator (whose
        /// children are SettlementCmd and the Settlement's Facilities) has
        /// its localPosition assigned this value. It is then attached to an orbit
        /// object which is parented to the System.
        /// </summary>
        public Vector3 SettlementOrbitSlot {
            get { return _settlementOrbitSlot; }
            set {
                _settlementOrbitSlot = value;
                D.Log("System's SettlementOrbitSlot set to {0}.", _settlementOrbitSlot);
            }
        }

        public SettlementCmdData SettlementData { get; set; }

        public StarData StarData { get; set; }

        public IList<PlanetoidCategory> PlanetCategories {
            get {
                return _composition.Keys.ToList();
            }
        }

        private IDictionary<PlanetoidCategory, IList<PlanetoidData>> _composition;

        public SystemComposition() {
            _composition = new SortedDictionary<PlanetoidCategory, IList<PlanetoidData>>();
        }

        public bool AddPlanet(PlanetoidData data) {
            PlanetoidCategory pType = data.Category;
            if (!_composition.ContainsKey(pType)) {
                _composition.Add(pType, new List<PlanetoidData>());
            }
            if (_composition[pType].Contains(data)) {
                return false;
            }
            _composition[pType].Add(data);
            return true;
        }

        public bool RemovePlanet(PlanetoidData data) {
            PlanetoidCategory pType = data.Category;
            bool isRemoved = _composition[pType].Remove(data);
            if (_composition[pType].Count == Constants.Zero) {
                _composition.Remove(pType);
            }
            return isRemoved;
        }

        public bool ContainsPlanet(PlanetoidData data) {
            PlanetoidCategory hull = data.Category;
            return _composition[hull].Contains(data);
        }

        public IList<PlanetoidData> GetPlanetData(PlanetoidCategory pType) {
            return _composition[pType];
        }

        public IEnumerable<PlanetoidData> GetPlanetData() {
            IEnumerable<PlanetoidData> allData = new List<PlanetoidData>();
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

