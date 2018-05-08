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

        public IEnumerable<PassiveCountermeasure> PassiveCountermeasures { get; private set; }

        public float MaxHitPoints { get; private set; }

        private float _currentHitPoints;
        public float CurrentHitPoints {
            get { return _currentHitPoints; }
            protected set {
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
        /// <param name="hitPts">The maximum hit points of this item.</param>
        /// <param name="passiveCMs">The item's passive Countermeasures.</param>
        public AMortalItemData(IMortalItem item, Player owner, float hitPts, IEnumerable<PassiveCountermeasure> passiveCMs)
            : base(item, owner) {
            Initialize(passiveCMs);
            MaxHitPoints = hitPts;
            CurrentHitPoints = hitPts;
        }

        private void Initialize(IEnumerable<PassiveCountermeasure> cms) {
            PassiveCountermeasures = cms;
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
            DamageMitigation = undamagedCMs.Select(cm => cm.DmgMitigation).Aggregate(defaultValueIfEmpty, (accum, cmDmgMit) => accum + cmDmgMit);
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

        [Obsolete]
        private void MaxHitPtsPropChangingHandler(float newMaxHitPoints) {
            HandleMaxHitPtsChanging(newMaxHitPoints);
        }

        [Obsolete]
        private void MaxHitPtsPropChangedHandler() {
            Health = MaxHitPoints > Constants.ZeroF ? CurrentHitPoints / MaxHitPoints : Constants.ZeroF;
        }

        private void CurrentHitPtsPropChangedHandler() {
            Utility.ValidateForRange(CurrentHitPoints, Constants.ZeroF, MaxHitPoints);
            Health = CurrentHitPoints / MaxHitPoints;
        }

        private void HealthPropChangedHandler() {
            HandleHealthChanged();
        }

        #endregion

        private void HandleCountermeasureIsDamagedChanged(AEquipment countermeasure) {
            D.Log(ShowDebugLog, "{0}'s {1} is {2}.", DebugName, countermeasure.Name, countermeasure.IsDamaged ? "damaged" : "repaired");
            RecalcDefensiveValues();
        }

        [Obsolete]
        private void HandleMaxHitPtsChanging(float newMaxHitPoints) {
            D.Assert(newMaxHitPoints >= Constants.ZeroF);
            if (newMaxHitPoints < MaxHitPoints) {
                // reduction in max hit points so reduce current hit points to match
                CurrentHitPoints = Mathf.Clamp(CurrentHitPoints, Constants.ZeroF, newMaxHitPoints);
                // changing CurrentHitPoints here sends out a temporary erroneous health change event. The accurate health change event follows shortly
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
            // Can't assert CurrentHitPoints as SingleElement FleetCmds can 'die' when they transfer their Element to another FleetCmd
            IsOperational = false;
            DeactivateAllEquipment();
        }

        protected void ReplacePassiveCMs(IEnumerable<PassiveCountermeasure> passiveCMs) {
            PassiveCountermeasures.ForAll(cm => {
                D.Assert(cm.IsActivated);
                cm.isDamagedChanged -= CountermeasureIsDamagedChangedEventHandler;
                cm.IsActivated = false;
            });

            D.Assert(passiveCMs.All(pCM => !pCM.IsActivated));
            Initialize(passiveCMs);
            PassiveCountermeasures.ForAll(cm => cm.IsActivated = true);
            RecalcDefensiveValues();
        }

        protected virtual void DeactivateAllEquipment() {
            PassiveCountermeasures.ForAll(cm => cm.IsActivated = false);
        }

        protected virtual void RemoveDamageFromAllEquipment() {
            PassiveCountermeasures.Where(cm => cm.IsDamageable).ForAll(cm => cm.IsDamaged = false);
        }

        #region Combat Support

        /// <summary>
        /// Applies the damage to the Item and returns true if the Item survived the hit.
        /// <remarks>Virtual to allow UnitCmds to override as they don't die from hits.</remarks>
        /// </summary>
        /// <param name="damageSustained">The damage sustained.</param>
        /// <param name="damageSeverity">The damage severity.</param>
        public virtual bool ApplyDamage(DamageStrength damageSustained, out float damageSeverity) {
            var initialDamage = damageSustained.__Total;
            if (CurrentHitPoints <= initialDamage) {
                // didn't survive so no point in applying to equipment
                CurrentHitPoints -= initialDamage;
                damageSeverity = Constants.ZeroF;
                return false;
            }
            // not yet dead
            damageSeverity = Mathf.Clamp01(initialDamage / CurrentHitPoints);
            float damageFromEquipmentLosses = AssessDamageToEquipment(damageSeverity);
            float totalDamage = initialDamage + damageFromEquipmentLosses;
            if (CurrentHitPoints <= totalDamage) {
                // didn't survive after equip losses added
                CurrentHitPoints -= totalDamage;
                return false;
            }
            // survived
            CurrentHitPoints -= totalDamage;
            D.AssertNotEqual(Constants.ZeroPercent, Health);
            return true;
        }

        /// <summary>
        /// Assesses and applies any crippling damage to this mortal item's equipment as a result of the hit, returning
        /// the cumulative amount of damage that should be applied to CurrentHitPts.
        /// </summary>
        /// <param name="damageSeverity">The severity of the impact of this hit. A percentage value, it is used to 
        /// determine the likelihood that equipment will be damaged.</param>
        protected virtual float AssessDamageToEquipment(float damageSeverity) {
            Utility.ValidateForRange(damageSeverity, Constants.ZeroPercent, Constants.OneHundredPercent);
            float cumCurrentHitPtReductionFromEquip = Constants.ZeroF;
            var damageChance = damageSeverity;
            var undamagedDamageablePassiveCMs = PassiveCountermeasures.Where(cm => cm.IsDamageable && !cm.IsDamaged);
            foreach (var pCM in undamagedDamageablePassiveCMs) {
                bool toDamage = RandomExtended.Chance(damageChance);
                if (toDamage) {
                    pCM.IsDamaged = true;
                    cumCurrentHitPtReductionFromEquip += pCM.HitPoints;
                }
            }
            return cumCurrentHitPtReductionFromEquip;
        }

        #endregion

        #region Repair

        /// <summary>
        /// Repairs this mortal item using the provided repair points and returns <c>true</c> if the item has fully repaired.
        /// <remarks>If already fully repaired, this method will do nothing and returns true.</remarks>
        /// </summary>
        /// <param name="repairPts">The repair points to use in restoring CurrentHitPts and damaged equipment.</param>
        /// <returns></returns>
        public bool RepairDamage(float repairPts) {
            D.Assert(!IsDead);
            D.AssertNotEqual(Constants.ZeroF, CurrentHitPoints);

            bool isAllDamageRepaired = false;
            float repairPtsNeeded = MaxHitPoints - CurrentHitPoints;
            if (repairPtsNeeded == Constants.ZeroF) {
                D.AssertEqual(Constants.OneHundredPercent, Health);
                // __ValidateAllEquipmentDamageRepaired() confirms CurrentHitPts can equal MaxHitPts with equipment still damaged
                RemoveDamageFromAllEquipment();
                isAllDamageRepaired = true;
            }
            else {
                float repairImpact = Mathf.Clamp01(repairPts / repairPtsNeeded);
                float repairPtsFromEquip = AssessRepairToEquipment(repairImpact);
                float totalRepairPts = repairPts + repairPtsFromEquip;
                CurrentHitPoints += totalRepairPts;
                if (Health == Constants.OneHundredPercent) {
                    // __ValidateAllEquipmentDamageRepaired() confirms CurrentHitPts can equal MaxHitPts with equipment still damaged
                    RemoveDamageFromAllEquipment();
                    isAllDamageRepaired = true;
                }
            }
            return isAllDamageRepaired;
        }

        /// <summary>
        /// Assesses whether any equipment needs repair, and if it does, uses repairImpact
        /// to determine whether the equipment assessed should be repaired by setting isDamaged
        /// to false. The total amount of hit points recovered from the equipment repaired is returned.
        /// </summary>
        /// <param name="repairImpact">The impact of this repair cycle. A percentage value, it is used to 
        /// determine the likelihood that damaged equipment will be repaired.</param>
        /// <returns></returns>
        protected virtual float AssessRepairToEquipment(float repairImpact) {
            Utility.ValidateForRange(repairImpact, Constants.ZeroPercent, Constants.OneHundredPercent);
            float cumRprPtsFromEquip = Constants.ZeroF;

            var rprChance = repairImpact;

            var damagedCMs = PassiveCountermeasures.Where(cm => cm.IsDamaged);
            foreach (var cm in damagedCMs) {
                bool toRpr = RandomExtended.Chance(rprChance);
                if (toRpr) {
                    cm.IsDamaged = false;
                    cumRprPtsFromEquip += cm.HitPoints;
                    D.Log(ShowDebugLog, "{0}'s {1} has been repaired.", DebugName, cm.Name);
                }
            }

            return cumRprPtsFromEquip;
        }

        #endregion

        #region Cleanup

        protected virtual void Cleanup() {
            Unsubscribe();
        }

        protected virtual void Unsubscribe() {
            PassiveCountermeasures.ForAll(cm => cm.isDamagedChanged -= CountermeasureIsDamagedChangedEventHandler);
        }

        #endregion

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        protected virtual void __ValidateAllEquipmentDamageRepaired() {
            PassiveCountermeasures.ForAll(cm => D.Assert(!cm.IsDamaged));
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

