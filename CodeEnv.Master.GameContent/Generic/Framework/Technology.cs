// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Technology.cs
// A researchable technology holding a collection of stats that are enabled once research is completed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// A researchable technology holding a collection of stats that are enabled once research is completed.
    /// </summary>
    public class Technology {

        private const string DebugNameFormat = "{0}[{1}], ResearchCost = {2:0.}";

        private string _debugName;
        public string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(GetType().Name, Name, ResearchCost);
                }
                return _debugName;
            }
        }

        // Can't have a ResearchStatus (completed, underway, etc.) as that status varies by player

        public string Name { get; private set; }

        public AtlasID ImageAtlasID { get; private set; }

        public string ImageFilename { get; private set; }

        /// <summary>
        /// The cost in units of science to research this technology.
        /// </summary>
        public float ResearchCost { get; private set; }

        private ATechStat[] _enabledStats;

        public Technology(string name, AtlasID imageAtlasID, string imageFilename, string description, float researchCost, ATechStat[] enabledStats) {
            Name = name;
            ImageAtlasID = imageAtlasID;
            ImageFilename = imageFilename;
            ResearchCost = researchCost;
            _enabledStats = enabledStats;
        }

        public bool TryGetEnabledStats(out IList<AEquipmentStat> eStats) {
            eStats = new List<AEquipmentStat>();
            foreach (var tStat in _enabledStats) {
                var eStat = tStat as AEquipmentStat;
                if (eStat != null) {
                    eStats.Add(eStat);
                }
            }
            return !eStats.IsNullOrEmpty();
        }

        public bool TryGetEnabledStats(out IList<CapabilityStat> cStats) {
            cStats = new List<CapabilityStat>();
            foreach (var tStat in _enabledStats) {
                var cStat = tStat as CapabilityStat;
                if (cStat != null) {
                    cStats.Add(cStat);
                }
            }
            return !cStats.IsNullOrEmpty();
        }

        public ATechStat[] GetEnabledStats() {
            return _enabledStats;
        }

        public sealed override string ToString() {
            return DebugName;
        }


    }
}

