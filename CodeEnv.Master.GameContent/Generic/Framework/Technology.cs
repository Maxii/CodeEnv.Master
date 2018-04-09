// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Technology.cs
// A researchable technology holding a collection of AImprovableStats that can be used once research is completed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// A researchable technology holding a collection of AImprovableStats that are enabled once research is completed.
    /// </summary>
    public class Technology {

        private const string DebugNameFormat = "{0}[{1}, {2:0.}]";

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

        public string Name { get { return _techStat.TechName; } }

        public AtlasID ImageAtlasID { get { return _techStat.ImageAtlasID; } }

        public string ImageFilename { get { return _techStat.ImageFilename; } }

        /// <summary>
        /// The cost in units of science to research this technology.
        /// <remarks>UNCLEAR May want to make this publicly settable to allow adjustment depending on game conditions?</remarks>
        /// </summary>
        public float ResearchCost { get; private set; }

        public Technology[] Prerequisites { get; set; }

        public TreeNodeID NodeID { get { return _techStat.NodeID; } }

        public string[] PrerequisiteTechNames { get { return _techStat.PrerequisiteTechNames; } }

        private AImprovableStat[] _enabledStats;
        private TechStat _techStat;

        public Technology(TechStat techStat, float researchCost, AImprovableStat[] __enabledStats) {
            _techStat = techStat;
            ResearchCost = researchCost;
            // TEMP until I determine how to ID enabled stats in XML - name currently does work as it isn't unique
            _enabledStats = __enabledStats;
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

        public AImprovableStat[] GetEnabledStats() {
            return _enabledStats;
        }

        public sealed override string ToString() {
            return DebugName;
        }


    }
}

