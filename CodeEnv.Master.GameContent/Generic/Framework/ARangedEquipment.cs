// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ARangedEquipment.cs
// Abstract base class for Ranged Equipment such as Sensors and Weapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

using System;
using CodeEnv.Master.Common;

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract base class for Ranged Equipment such as Sensors and Weapons.
    /// </summary>
    public abstract class ARangedEquipment : AEquipment {

        private const string NameFormat = "{0}[{1}({2:0.})]";

        protected const string DebugNameFormat = "{0}.{1}";

        public RangeCategory RangeCategory { get { return Stat.RangeCategory; } }

        public abstract string DebugName { get; }

        public override string Name { get { return NameFormat.Inject(base.Name, RangeCategory.GetValueName(), RangeDistance); } }

        private float _rangeDistance;
        /// <summary>
        /// The equipment's range in units adjusted for any range modifiers from owners, etc.
        /// <remarks>Kept updated by the associated RangeMonitor when it refreshes its RangeDistance value.</remarks>
        /// </summary>
        public float RangeDistance {
            get { return _rangeDistance; }
            set {
                if (_rangeDistance != value) {
                    _rangeDistance = value;
                    RangeDistanceChangedPropHandler();
                }
            }
        }

        protected new ARangedEquipmentStat Stat { get { return base.Stat as ARangedEquipmentStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ARangedEquipment"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public ARangedEquipment(ARangedEquipmentStat stat, string name = null) : base(stat, name) { }

        #region Event and Property Change Handlers

        private void RangeDistanceChangedPropHandler() {
            HandleRangeDistanceChanged();
        }

        protected virtual void HandleRangeDistanceChanged() { }

        #endregion

    }
}

