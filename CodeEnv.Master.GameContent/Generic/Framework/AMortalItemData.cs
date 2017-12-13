﻿// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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

        public IList<PassiveCountermeasure> PassiveCountermeasures { get; private set; }

        private float _maxHitPoints;
        public float MaxHitPoints {
            get { return _maxHitPoints; }
            set { SetProperty<float>(ref _maxHitPoints, value, "MaxHitPoints", MaxHitPtsPropChangedHandler, MaxHitPtsPropChangingHandler); }
        }

        private float _currentHitPoints;
        public float CurrentHitPoints {
            get { return _currentHitPoints; }
            set {
                value = Mathf.Clamp(value, Constants.ZeroF, MaxHitPoints);
                SetProperty<float>(ref _currentHitPoints, value, "CurrentHitPoints", CurrentHitPtsPropChangedHandler);
            }
        }

        private float _health;
        /// <summary>
        /// The health of the item, a value between 0 and 1.
        /// </summary>
        public float Health {
            get { return _health; }
            private set {
                value = Mathf.Clamp01(value);
                SetProperty<float>(ref _health, value, "Health", HealthPropChangedHandler);
            }
        }

        private CombatStrength _defensiveStrength;
        public CombatStrength DefensiveStrength {
            get { return _defensiveStrength; }
            protected set { SetProperty<CombatStrength>(ref _defensiveStrength, value, "DefensiveStrength"); }
        }

        private DamageStrength _damageMitigation;
        public DamageStrength DamageMitigation {
            get { return _damageMitigation; }
            protected set { SetProperty<DamageStrength>(ref _damageMitigation, value, "DamageMitigation"); }
        }

        protected bool _isDead;
        public bool IsDead {
            get { return _isDead; }
            set { SetProperty<bool>(ref _isDead, value, "IsDead", IsDeadPropSetHandler, IsDeadPropSettingHandler); }
        }

        public abstract IntVector3 SectorID { get; }

        #region Initialization 

        /// <summary>
        /// Initializes a new instance of the <see cref="AMortalItemData" /> class.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="maxHitPts">The maximum hit points.</param>
        /// <param name="passiveCMs">The item's passive Countermeasures.</param>
        public AMortalItemData(IMortalItem item, Player owner, float maxHitPts, IEnumerable<PassiveCountermeasure> passiveCMs)
            : base(item, owner) {
            Initialize(passiveCMs);
            MaxHitPoints = maxHitPts;
            CurrentHitPoints = maxHitPts;
        }

        private void Initialize(IEnumerable<PassiveCountermeasure> cms) {
            PassiveCountermeasures = cms.ToList();
            PassiveCountermeasures.ForAll(cm => {
                D.Assert(!cm.IsActivated);  // items activate equipment when the item commences operation
                cm.isDamagedChanged += CountermeasureIsDamagedChangedEventHandler;
            });
        }

        public override void FinalInitialize() {
            base.FinalInitialize();
            PassiveCountermeasures.ForAll(cm => cm.IsActivated = true);
            RecalcDefensiveValues();
        }

        #endregion

        protected virtual void RecalcDefensiveValues() {
            var undamagedCMs = PassiveCountermeasures.Where(cm => !cm.IsDamaged);
            var defaultValueIfEmpty = default(DamageStrength);
            DamageMitigation = undamagedCMs.Select(cm => cm.DamageMitigation).Aggregate(defaultValueIfEmpty, (accum, cmDmgMit) => accum + cmDmgMit);
            DefensiveStrength = new CombatStrength(undamagedCMs.Cast<ICountermeasure>());
        }

        #region Event and Property Change Handlers

        private void IsDeadPropSettingHandler(bool incomingIsDead) {
            if (!IsOperational) {
                // 11.19.17 Death before being operational used to occur to FleetCmds when they were merged immediately after they were built
                // in their creator. This was when the creator would defer CommenceOperations when paused and GameScriptsUtility.Merge() 
                // would transfer all ships to the best Cmd chosen to survive and the other Cmds would die before they were operational.
                // This is no longer the case as creators no longer defer CommenceOperations when paused.
                D.Error("{0} is being destroyed before it becomes operational.", DebugName);
            }
        }

        private void IsDeadPropSetHandler() {
            HandleIsDeadPropSet();
        }

        protected void CountermeasureIsDamagedChangedEventHandler(object sender, EventArgs e) {
            var cm = sender as AEquipment;
            HandleCountermeasureIsDamagedChanged(cm);
        }

        private void MaxHitPtsPropChangingHandler(float newMaxHitPoints) {
            HandleMaxHitPtsChanging(newMaxHitPoints);
        }

        private void MaxHitPtsPropChangedHandler() {
            Health = MaxHitPoints > Constants.ZeroF ? CurrentHitPoints / MaxHitPoints : Constants.ZeroF;
        }

        private void CurrentHitPtsPropChangedHandler() {
            Health = MaxHitPoints > Constants.ZeroF ? CurrentHitPoints / MaxHitPoints : Constants.ZeroF;
        }

        private void HealthPropChangedHandler() {
            HandleHealthChanged();
        }

        #endregion

        private void HandleCountermeasureIsDamagedChanged(AEquipment countermeasure) {
            D.Log(ShowDebugLog, "{0}'s {1} is {2}.", DebugName, countermeasure.Name, countermeasure.IsDamaged ? "damaged" : "repaired");
            RecalcDefensiveValues();
        }

        private void HandleMaxHitPtsChanging(float newMaxHitPoints) {
            D.Assert(newMaxHitPoints >= Constants.ZeroF);
            if (newMaxHitPoints < MaxHitPoints) {
                // reduction in max hit points so reduce current hit points to match
                CurrentHitPoints = Mathf.Clamp(CurrentHitPoints, Constants.ZeroF, newMaxHitPoints);
                // FIXME changing CurrentHitPoints here sends out a temporary erroneous health change event. The accurate health change event follows shortly
            }
        }

        protected virtual void HandleHealthChanged() {
            //D.Log(ShowDebugLog, "{0}: Health {1}, CurrentHitPoints {2}, MaxHitPoints {3}.", DebugName, _health, CurrentHitPoints, MaxHitPoints);
        }

        protected sealed override void HandleIsOperationalChanged() {
            // override the AItemData Assert as AMortalItem sets IsDead which sets IsOperational to false when dieing
            if (!IsOperational) {
                D.Assert(IsDead);
            }
        }

        private void HandleIsDeadPropSet() {
            D.Assert(IsDead);
            // Can't assert CurrentHitPoints as LoneFleetCmds can 'die' when they transfer their LoneElement to another Fleet
            IsOperational = false;
            DeactivateAllCmdModuleEquipment();
        }

        protected virtual void DeactivateAllCmdModuleEquipment() {
            PassiveCountermeasures.ForAll(cm => cm.IsActivated = false);
        }

        #region Cleanup

        protected virtual void Cleanup() {
            Unsubscribe();
        }

        protected virtual void Unsubscribe() {
            PassiveCountermeasures.ForAll(cm => cm.isDamagedChanged -= CountermeasureIsDamagedChangedEventHandler);
        }

        #endregion

        #region IDisposable

        private bool _alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {

            Dispose(true);

            // This object is being cleaned up by you explicitly calling Dispose() so take this object off
            // the finalization queue and prevent finalization code from 'disposing' a second time
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="isExplicitlyDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isExplicitlyDisposing) {
            if (_alreadyDisposed) { // Allows Dispose(isExplicitlyDisposing) to mistakenly be called more than once
                D.Warn("{0} has already been disposed.", GetType().Name);
                return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
            }

            if (isExplicitlyDisposing) {
                // Dispose of managed resources here as you have called Dispose() explicitly
                Cleanup();
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            _alreadyDisposed = true;
        }

        #endregion



    }
}

