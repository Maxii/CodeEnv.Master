// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipHullEquipment.cs
// Holds a reference to a ship hull, values associated with that hull along with any equipment that uses mounts attached to the hull.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Holds a reference to a ship hull, values associated with that hull along with any equipment that uses mounts attached to the hull.
    /// </summary>
    public class ShipHullEquipment : AHullEquipment {

        public new IShipHull Hull {
            get { return base.Hull as IShipHull; }
            set { base.Hull = value; }
        }

        public ShipHullCategory HullCategory { get { return Stat.HullCategory; } }

        public float Drag { get { return Stat.Drag; } }

        public float Science { get { return Stat.Science; } }
        public float Culture { get { return Stat.Culture; } }
        public float Income { get { return Stat.Income; } }

        protected new ShipHullStat Stat { get { return base.Stat as ShipHullStat; } }

        public ShipHullEquipment(ShipHullStat stat) : base(stat) { }

        protected override void OnHullChanged() {
            D.Assert(Hull.HullCategory == HullCategory);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

