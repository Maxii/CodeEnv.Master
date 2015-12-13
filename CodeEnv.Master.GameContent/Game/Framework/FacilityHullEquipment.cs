// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityHullEquipment.cs
// Holds a reference to a facility hull, values associated with that hull along with any equipment that uses mounts attached to the hull.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Holds a reference to a facility hull, values associated with that hull along with any equipment that uses mounts attached to the hull.
    /// </summary>
    public class FacilityHullEquipment : AHullEquipment {

        public new IFacilityHull Hull {
            get { return base.Hull as IFacilityHull; }
            set { base.Hull = value; }
        }

        public FacilityHullCategory HullCategory { get { return Stat.HullCategory; } }

        public float Science { get { return Stat.Science; } }
        public float Culture { get { return Stat.Culture; } }
        public float Income { get { return Stat.Income; } }

        protected new FacilityHullStat Stat { get { return base.Stat as FacilityHullStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityHullEquipment"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public FacilityHullEquipment(FacilityHullStat stat, string name = null) : base(stat, name) { }

        protected override void HullPropSetHandler() {
            D.Assert(Hull.HullCategory == HullCategory);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

