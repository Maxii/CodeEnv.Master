// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AHullEquipment.cs
// Abstract base class which holds a hull reference, values associated with that hull along with any
// equipment that uses mounts attached to the hull.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class which holds a hull reference, values associated with that hull along with any
    /// equipment that uses mounts attached to the hull.
    /// </summary>
    public abstract class AHullEquipment : AEquipment {

        private IHull _hull;
        public IHull Hull {
            get { return _hull; }
            set {
                D.AssertNull(_hull);    // happens only once
                _hull = value;
                HullPropSetHandler();
            }
        }

        public IList<AWeapon> Weapons { get; private set; }

        public float MaxHitPoints { get { return Stat.MaxHitPoints; } }

        public DamageStrength DamageMitigation { get { return Stat.DamageMitigation; } }

        public Vector3 HullDimensions { get { return Stat.HullDimensions; } }

        protected new AHullStat Stat { get { return base.Stat as AHullStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="AHullEquipment"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public AHullEquipment(AHullStat stat, string name = null)
            : base(stat, name) {
            Weapons = new List<AWeapon>();
        }

        public void AddWeapon(AWeapon weapon) {
            D.AssertNotNull(weapon.RangeMonitor);
            D.AssertNotNull(weapon.WeaponMount);   // will already have Mount if adding in design screen using drag and drop
            D.Assert(!weapon.IsActivated);    // items activate equipment when the item commences operation
            Weapons.Add(weapon);
        }

        /// <summary>
        /// Returns <c>true</c> if this Hull supports the provided outputID, <c>false</c> otherwise.
        /// <remarks>If false, the yield returned will be zero.</remarks>
        /// </summary>
        /// <param name="outputID">The output identifier.</param>
        /// <param name="yield">The yield this hull produces for the provided outputID.</param>
        /// <returns></returns>
        public abstract bool TryGetYield(OutputID outputID, out float yield);

        protected abstract void HullPropSetHandler();

    }
}

