// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItemData.cs
// Abstract class for Data associated with an AMortalItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract class for Data associated with an AMortalItem.
    /// </summary>
    public abstract class AMortalItemData : AIntelItemData, IDisposable {

        public IList<Countermeasure> Countermeasures { get; private set; }

        private float _maxHitPoints;
        public float MaxHitPoints {
            get { return _maxHitPoints; }
            set { SetProperty<float>(ref _maxHitPoints, value, "MaxHitPoints", OnMaxHitPointsChanged, OnMaxHitPointsChanging); }
        }

        private float _currentHitPoints;
        public virtual float CurrentHitPoints {
            get { return _currentHitPoints; }
            set {
                value = Mathf.Clamp(value, Constants.ZeroF, MaxHitPoints);
                SetProperty<float>(ref _currentHitPoints, value, "CurrentHitPoints", OnCurrentHitPointsChanged);
            }
        }

        private float _health;
        /// <summary>
        /// The health of the item, a value between 0 and 1.
        /// </summary>
        public virtual float Health {
            get { return _health; }
            private set {
                value = Mathf.Clamp01(value);
                SetProperty<float>(ref _health, value, "Health", OnHealthChanged);
            }
        }

        private CombatStrength _defensiveStrength;
        public CombatStrength DefensiveStrength {
            get { return _defensiveStrength; }
            private set { SetProperty<CombatStrength>(ref _defensiveStrength, value, "DefensiveStrength"); }
        }

        public abstract Index3D SectorIndex { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AMortalItemData"/> class.
        /// </summary>
        /// <param name="itemTransform">The item transform.</param>
        /// <param name="name">The name.</param>
        /// <param name="maxHitPts">The maximum hit PTS.</param>
        /// <param name="owner">The owner.</param>
        public AMortalItemData(Transform itemTransform, string name, float maxHitPts, Player owner)
            : base(itemTransform, name, owner) {
            Countermeasures = new List<Countermeasure>();
            MaxHitPoints = maxHitPts;
            CurrentHitPoints = maxHitPts;
        }

        /// <summary>
        /// Adds the Countermeasure to this item's data.
        /// </summary>
        /// <param name="cm">The Countermeasure.</param>
        public void AddCountermeasure(Countermeasure cm) {
            D.Assert(!Countermeasures.Contains(cm));
            D.Assert(!cm.IsOperational);
            Countermeasures.Add(cm);
            cm.onIsOperationalChanged += OnCountermeasureIsOperationalChanged;
            // no need to Recalc max countermeasure-related values as this occurs when IsOperational changes
        }

        /// <summary>
        /// Removes the Countermeasure from the item's data.
        /// </summary>
        /// <param name="cm">The Countermeasure.</param>
        public void RemoveCountermeasure(Countermeasure cm) {
            D.Assert(Countermeasures.Contains(cm));
            D.Assert(!cm.IsOperational);
            Countermeasures.Remove(cm);
            cm.onIsOperationalChanged -= OnCountermeasureIsOperationalChanged;
            // no need to Recalc max countermeasure-related values as this occurs when IsOperational changes
        }

        private void RecalcDefensiveStrength() {
            var defaultValueIfEmpty = default(CombatStrength);
            DefensiveStrength = Countermeasures.Where(cm => cm.IsOperational).Select(cm => cm.Strength).Aggregate(defaultValueIfEmpty, (accum, cmStrength) => accum + cmStrength);
        }

        private void OnCountermeasureIsOperationalChanged(Countermeasure cm) {
            D.Log("{0}'s {1}.IsOperational is now {2}.", FullName, cm.Name, cm.IsOperational);
            RecalcDefensiveStrength();
        }

        private void OnMaxHitPointsChanging(float newMaxHitPoints) {
            D.Assert(newMaxHitPoints >= Constants.ZeroF);
            if (newMaxHitPoints < MaxHitPoints) {
                // reduction in max hit points so reduce current hit points to match
                CurrentHitPoints = Mathf.Clamp(CurrentHitPoints, Constants.ZeroF, newMaxHitPoints);
                // FIXME changing CurrentHitPoints here sends out a temporary erroneous health change event. The accurate health change event follows shortly
            }
        }

        private void OnMaxHitPointsChanged() {
            Health = MaxHitPoints > Constants.ZeroF ? CurrentHitPoints / MaxHitPoints : Constants.ZeroF;
        }

        private void OnCurrentHitPointsChanged() {
            Health = MaxHitPoints > Constants.ZeroF ? CurrentHitPoints / MaxHitPoints : Constants.ZeroF;
        }

        protected virtual void OnHealthChanged() {
            D.Log("{0}: Health {1}, CurrentHitPoints {2}, MaxHitPoints {3}.", FullName, _health, CurrentHitPoints, MaxHitPoints);
        }

        #region Cleanup

        protected virtual void Cleanup() {
            Unsubscribe();
        }

        protected virtual void Unsubscribe() {
            Countermeasures.ForAll(cm => cm.onIsOperationalChanged -= OnCountermeasureIsOperationalChanged);
        }

        #endregion

        #region IDisposable
        [DoNotSerialize]
        private bool _alreadyDisposed = false;
        protected bool _isDisposing = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isDisposing) {
            // Allows Dispose(isDisposing) to be called more than once
            if (_alreadyDisposed) {
                D.Warn("{0} has already been disposed.", GetType().Name);
                return;
            }

            _isDisposing = true;
            if (isDisposing) {
                // free managed resources here including unhooking events
                Cleanup();
            }
            // free unmanaged resources here

            _alreadyDisposed = true;
        }

        // Example method showing check for whether the object has been disposed
        //public void ExampleMethod() {
        //    // throw Exception if called on object that is already disposed
        //    if(alreadyDisposed) {
        //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        //    }

        //    // method content here
        //}
        #endregion

    }
}

