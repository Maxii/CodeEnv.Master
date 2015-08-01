// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AEquipment.cs
// Abstract base class for Equipment such as Sensors, Countermeasures and Weapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base class for Equipment such as Sensors, Countermeasures and Weapons.
    /// </summary>
    public abstract class AEquipment : APropertyChangeTracking {

        public event Action<AEquipment> onIsOperationalChanged;

        public virtual string Name { get { return Stat.Name; } }

        public AtlasID ImageAtlasID { get { return Stat.ImageAtlasID; } }

        public string ImageFilename { get { return Stat.ImageFilename; } }

        public string Description { get { return Stat.Description; } }

        public float PhysicalSize { get { return Stat.PhysicalSize; } }

        public float PowerRequirement { get { return Stat.PowerRequirement; } }

        private bool _isOperational;
        /// <summary>
        /// Indicates whether this equipment is operational (undamaged). 
        /// </summary>
        public bool IsOperational {
            get { return _isOperational; }
            set { SetProperty<bool>(ref _isOperational, value, "IsOperational", OnIsOperationalChanged); }
        }

        protected AEquipmentStat Stat { get; private set; }

        public AEquipment(AEquipmentStat stat) {
            Stat = stat;
        }

        protected virtual void OnIsOperationalChanged() {
            D.Log("{0}.IsOperational changed to {1}.", Name, IsOperational);
            NotifyIsOperationalChanged();
        }

        protected void NotifyIsOperationalChanged() {
            if (onIsOperationalChanged != null) {
                onIsOperationalChanged(this);
            }
        }

    }
}

