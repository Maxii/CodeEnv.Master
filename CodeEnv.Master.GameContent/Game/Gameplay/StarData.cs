// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarData.cs
// All the data associated with a particular star.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// All the data associated with a particular star.
    /// </summary>
    public class StarData : AData {

        public StarType StarType { get; private set; }

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

        /// <summary>
        /// Initializes a new instance of the <see cref="StarData"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="name">The name.</param>
        /// <param name="parentName">Name of the parent.</param>
        public StarData(StarType type, string name, string parentName)
            : base(name, parentName) {
            StarType = type;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

