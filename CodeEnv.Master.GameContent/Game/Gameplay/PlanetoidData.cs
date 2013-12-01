// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidData.cs
// All the data associated with a particular Planetoid in a System.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// All the data associated with a particular Planetoid in a System.
    /// </summary>
    public class PlanetoidData : Data {

        public PlanetoidType PlanetoidType { get; private set; }

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

        public PlanetoidData(PlanetoidType type, string name, float maxHitPoints, string parentName)
            : base(name, maxHitPoints, parentName) {
            PlanetoidType = type;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

