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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base class for Equipment such as Sensors, Countermeasures and Weapons.
    /// </summary>
    public abstract class AEquipment : APropertyChangeTracking {

        public event EventHandler isOperationalChanged;

        public event EventHandler isDamagedChanged;

        public virtual string Name { get; private set; }

        public AtlasID ImageAtlasID { get { return Stat.ImageAtlasID; } }
        public string ImageFilename { get { return Stat.ImageFilename; } }
        public string Description { get { return Stat.Description; } }

        public float Size { get { return Stat.Size; } }
        public float Mass { get { return Stat.Mass; } }
        public float PowerRequirement { get { return Stat.PowerRequirement; } }
        public float Expense { get { return Stat.Expense; } }

        private bool _isActivated;
        public bool IsActivated {
            get { return _isActivated; }
            set { SetProperty<bool>(ref _isActivated, value, "IsActivated", IsActivatedPropChangedHandler); }
        }

        private bool _isDamaged;
        public bool IsDamaged {
            get { return _isDamaged; }
            set { SetProperty<bool>(ref _isDamaged, value, "IsDamaged", IsDamagedPropChangedHandler); }
        }

        private bool _isOperational;
        public bool IsOperational {
            get { return _isOperational; }
            private set { SetProperty<bool>(ref _isOperational, value, "IsOperational", IsOperationalPropChangedHandler); }
        }

        protected AEquipmentStat Stat { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AEquipment"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public AEquipment(AEquipmentStat stat, string name = null) {
            Stat = stat;
            Name = name != null ? name : stat.Name;
        }

        #region Event and Property Change Handlers

        private void IsActivatedPropChangedHandler() {
            D.Log("{0}.IsActivated changed to {1}.", Name, IsActivated);
            AssessIsOperational();
        }

        private void IsDamagedPropChangedHandler() {
            D.Log("{0}.IsDamaged changed to {1}.", Name, IsDamaged);
            OnIsDamagedChanged();
            AssessIsOperational();
        }

        private void AssessIsOperational() {
            IsOperational = IsActivated && !IsDamaged;
        }

        protected virtual void IsOperationalPropChangedHandler() {
            D.Log("{0}.IsOperational changed to {1}.", Name, IsOperational);
            OnIsOperationalChanged();
        }

        protected void OnIsOperationalChanged() {
            if (isOperationalChanged != null) {
                isOperationalChanged(this, new EventArgs());
            }
        }

        private void OnIsDamagedChanged() {
            if (isDamagedChanged != null) {
                isDamagedChanged(this, new EventArgs());
            }
        }

        #endregion

    }
}

