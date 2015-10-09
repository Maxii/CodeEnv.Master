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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base class which holds a hull reference, values associated with that hull along with any
    /// equipment that uses mounts attached to the hull.
    /// </summary>
    public abstract class AHullEquipment : AEquipment {

        private IHull _hull;
        public IHull Hull {
            get { return _hull; }
            set {
                D.Assert(_hull == null);    // happens only once
                _hull = value;
                OnHullChanged();
            }
        }

        public IList<AWeapon> Weapons { get; private set; }

        public float MaxHitPoints { get { return Stat.MaxHitPoints; } }

        public DamageStrength DamageMitigation { get { return Stat.DamageMitigation; } }

        protected new AHullStat Stat { get { return base.Stat as AHullStat; } }

        public AHullEquipment(AHullStat stat)
            : base(stat) {
            Weapons = new List<AWeapon>();
        }

        public void AddWeapon(AWeapon weapon) {
            D.Assert(weapon.RangeMonitor != null);
            D.Assert(weapon.WeaponMount != null);   // will already have Mount if adding in design screen using drag and drop
            //D.Assert(!weapon.IsOperational);    // Items make equipment operational when the item becomes operational
            D.Assert(!weapon.IsActivated);    // Items make equipment active when the item becomes operational
            Weapons.Add(weapon);
        }

        protected abstract void OnHullChanged();

    }
}

