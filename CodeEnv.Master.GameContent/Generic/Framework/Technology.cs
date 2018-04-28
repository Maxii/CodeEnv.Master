// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Technology.cs
// A researchable technology holding collections of improvable stats that become enabled once research is completed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// A researchable technology holding collections of improvable stats that become enabled once research is completed.
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

        public string Name { get { return Stat.Name; } }

        public AtlasID ImageAtlasID { get { return Stat.ImageAtlasID; } }

        public string ImageFilename { get { return Stat.ImageFilename; } }

        /// <summary>
        /// The cost in units of science to research this technology.
        /// </summary>
        public float ResearchCost { get; private set; }

        public IEnumerable<Technology> Prerequisites { get; set; }

        public TreeNodeID NodeID { get { return Stat.NodeID; } }

        public TechStat Stat { get; private set; }

        private IEnumerable<AEquipmentStat> _enabledEquipStats;
        private IEnumerable<CapabilityStat> _enabledCapStats;

        public Technology(TechStat techStat, float rschCost, IEnumerable<AEquipmentStat> enabledEquipStats)
            : this(techStat, rschCost, enabledEquipStats, Enumerable.Empty<CapabilityStat>()) { }

        public Technology(TechStat techStat, float rschCost, IEnumerable<AEquipmentStat> enabledEquipStats, IEnumerable<CapabilityStat> enabledCapStats) {
            Stat = techStat;
            ResearchCost = rschCost;
            _enabledEquipStats = enabledEquipStats;
            _enabledCapStats = enabledCapStats;
        }

        public bool TryGetEnabledStats(out IList<AEquipmentStat> eStats) {
            eStats = new List<AEquipmentStat>();
            foreach (var stat in _enabledEquipStats) {
                eStats.Add(stat);
            }
            return !eStats.IsNullOrEmpty();
        }

        public bool TryGetEnabledStats(out IList<CapabilityStat> cStats) {
            cStats = new List<CapabilityStat>();
            foreach (var stat in _enabledCapStats) {
                cStats.Add(stat);
            }
            return !cStats.IsNullOrEmpty();
        }

        public IEnumerable<AEquipmentStat> GetEnabledEquipStats() {
            return _enabledEquipStats;
        }

        public IEnumerable<CapabilityStat> GetEnabledCapStats() {
            return _enabledCapStats;
        }

        public sealed override string ToString() {
            return DebugName;
        }


    }
}

