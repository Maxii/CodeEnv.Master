// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemData.cs
// All the data associated with a particular system.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// All the data associated with a particular system.
    /// </summary>
    public class SystemData : Data {

        private int _capacity;
        public int Capacity {
            get { return _capacity; }
            set {
                SetProperty<int>(ref _capacity, value, "Capacity");
            }
        }

        private OpeYield _resources;
        public OpeYield Resources {
            get { return _resources; }
            set {
                SetProperty<OpeYield>(ref _resources, value, "Resources");
            }
        }

        private XYield _specialResources;
        public XYield SpecialResources {
            get { return _specialResources; }
            set {
                SetProperty<XYield>(ref _specialResources, value, "SpecialResources");
            }
        }

        private SettlementData _settlement;
        public SettlementData Settlement {
            get { return _settlement; }
            set {
                SetProperty<SettlementData>(ref _settlement, value, "Settlement");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemData" /> class.
        /// </summary>
        /// <param name="transform">The system transform.</param>
        /// <param name="systemName">Name of the system.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        public SystemData(Transform transform, string systemName, float maxHitPoints = Mathf.Infinity)
            : base(transform, systemName, maxHitPoints) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

