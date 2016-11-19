// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CelestialDesigns.cs
// Holds a collection of CelestialDesigns (Stars, Planets and Moons) organized by the design's name.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Holds a collection of CelestialDesigns (Stars, Planets and Moons) organized by the design's name.
    /// </summary>
    public class CelestialDesigns {

        private IDictionary<string, StarDesign> _starDesignsLookup;
        private IDictionary<string, PlanetDesign> _planetDesignsLookup;
        private IDictionary<string, MoonDesign> _moonDesignsLookup;


        public CelestialDesigns() {
            _starDesignsLookup = new Dictionary<string, StarDesign>();
            _planetDesignsLookup = new Dictionary<string, PlanetDesign>();
            _moonDesignsLookup = new Dictionary<string, MoonDesign>();
        }

        public void Add(StarDesign design) {
            string designName = design.DesignName;
            D.Assert(!_starDesignsLookup.ContainsKey(designName));
            _starDesignsLookup.Add(designName, design);
            //D.Log("{0} added {1} {2}.", GetType().Name, design.GetType().Name, designName);
        }

        public void Add(PlanetDesign design) {
            string designName = design.DesignName;
            D.Assert(!_planetDesignsLookup.ContainsKey(designName));
            _planetDesignsLookup.Add(designName, design);
            //D.Log("{0} added {1} {2}.", GetType().Name, design.GetType().Name, designName);
        }

        public void Add(MoonDesign design) {
            string designName = design.DesignName;
            D.Assert(!_moonDesignsLookup.ContainsKey(designName));
            _moonDesignsLookup.Add(designName, design);
            //D.Log("{0} added {1} {2}.", GetType().Name, design.GetType().Name, designName);
        }

        public StarDesign GetStarDesign(string designName) {
            if (!_starDesignsLookup.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", GetType().Name, typeof(StarDesign).Name, designName, _starDesignsLookup.Keys.Concatenate());
            }
            return _starDesignsLookup[designName];
        }

        public PlanetDesign GetPlanetDesign(string designName) {
            if (!_planetDesignsLookup.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", GetType().Name, typeof(PlanetDesign).Name, designName, _planetDesignsLookup.Keys.Concatenate());
            }
            return _planetDesignsLookup[designName];
        }

        public MoonDesign GetMoonDesign(string designName) {
            if (!_moonDesignsLookup.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", GetType().Name, typeof(MoonDesign).Name, designName, _moonDesignsLookup.Keys.Concatenate());
            }
            return _moonDesignsLookup[designName];
        }

        public void Reset() {
            _starDesignsLookup.Clear();
            _planetDesignsLookup.Clear();
            _moonDesignsLookup.Clear();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

