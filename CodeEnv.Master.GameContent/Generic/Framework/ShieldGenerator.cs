// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShieldGenerator.cs
// An Element's ShieldGenerator. Can be Short, Medium or Long range.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// An Element's ShieldGenerator. Can be Short, Medium or Long range.
    /// </summary>
    public class ShieldGenerator : ARangedEquipment, ICountermeasure, IDisposable {

        /// <summary>
        /// Occurs when this ShieldGenerator changes from having any charge to having
        /// no charge, or vis-versa.
        /// </summary>
        public event EventHandler hasChargeChanged;

        public override string FullName {
            get { return Shield != null ? _fullNameFormat.Inject(Shield.FullName, Name) : Name; }
        }

        private IShield _shield;
        public IShield Shield {
            get { return _shield; }
            set { SetProperty<IShield>(ref _shield, value, "Shield"); }
        }

        public float ReloadPeriod {
            get {
                float reloadPeriodMultiplier = Shield != null ? Shield.Owner.CountermeasureReloadPeriodMultiplier : Constants.OneF;
                return Stat.ReloadPeriod * reloadPeriodMultiplier;
            }
        }

        /// <summary>
        /// The maximum absorption capacity of this generator.
        /// </summary>
        public float MaximumCharge { get { return Stat.MaximumCharge; } }

        private float _currentCharge;
        /// <summary>
        /// The current absorption capacity of this generator. When this value reaches Zero,
        /// the generator must be reloaded before it can absorb more.
        /// </summary>
        public float CurrentCharge {
            get { return _currentCharge; }
            private set { _currentCharge = Mathf.Clamp(value, Constants.ZeroF, MaximumCharge); }
        }

        private bool _hasCharge;
        /// <summary>
        /// Flag indicating whether this generator has any current absorption capacity.
        /// </summary>
        public bool HasCharge {
            get { return _hasCharge; }
            private set { SetProperty<bool>(ref _hasCharge, value, "HasCharge", HasChargePropChangedHandler); }
        }

        /// <summary>
        /// The rate at which this generator can increase CurrentCharge toward MaximumCharge. Joules per hour.
        /// </summary>
        public float TrickleChargeRate { get { return Stat.TrickleChargeRate; } }

        /// <summary>
        /// The amount of damage this generator can mitigate when the Item it is apart of takes a hit.
        /// This value has nothing to do with the capacity of this generator to absorb impacts.
        /// </summary>
        public DamageStrength DamageMitigation { get { return Stat.DamageMitigation; } }

        protected new ShieldGeneratorStat Stat { get { return base.Stat as ShieldGeneratorStat; } }

        private bool IsReloadJobRunning { get { return _reloadJob != null && _reloadJob.IsRunning; } }

        private bool IsRechargeJobRunning { get { return _rechargeJob != null && _rechargeJob.IsRunning; } }

        private GameTime _gameTime;
        private Job _reloadJob;
        private Job _rechargeJob;
        private bool _isRecharging;
        private bool _isReloading;
        private IGameManager _gameMgr;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShieldGenerator"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public ShieldGenerator(ShieldGeneratorStat stat, string name = null)
            : base(stat, name) {
            _gameTime = GameTime.Instance;
            _gameMgr = References.GameManager;
            CurrentCharge = Constants.ZeroF;
        }

        /// <summary>
        /// Attempts to absorb the impact of an ordnance delivery vehicle. If the full value of the impact is absorbed
        /// by this generator, the method returns <c>true</c> and the unabsorbedImpactValue is Zero.
        /// If not, the method returns <c>false</c> and unabsorbedImpactValue will be > Zero.
        /// </summary>
        /// <param name="deliveryVehicleImpactValue">The impact value of the ordnance delivery vehicle.</param>
        /// <param name="unabsorbedImpactValue">The unabsorbed impact value, if any.</param>
        /// <returns></returns>
        public bool TryAbsorbImpact(float deliveryVehicleImpactValue, out float unabsorbedImpactValue) {
            D.Assert(IsOperational);
            D.Assert(HasCharge);

            D.Log("{0}.{1} with charge {2.0.#) is attempting to absorb a {3:0.#} impact.", Shield.FullName, Name, CurrentCharge, deliveryVehicleImpactValue);
            bool isHitCompletelyAbsorbed = true;
            unabsorbedImpactValue = Constants.ZeroF;
            if (CurrentCharge < deliveryVehicleImpactValue) {
                isHitCompletelyAbsorbed = false;
                unabsorbedImpactValue = deliveryVehicleImpactValue - CurrentCharge;
                HasCharge = false;
            }
            else if (CurrentCharge == deliveryVehicleImpactValue) {
                HasCharge = false;
            }
            else {
                CurrentCharge -= deliveryVehicleImpactValue;
                AssessRechargeState();
            }
            D.Log(!HasCharge, "{0}.{1} has failed.", Shield.FullName, Name);
            return isHitCompletelyAbsorbed;
        }

        private void HandleReloaded() {
            HasCharge = true;
        }

        #region Event and Property Change Handlers

        protected override void IsOperationalPropChangedHandler() {
            base.IsOperationalPropChangedHandler();
            if (IsOperational) {
                InitiateReloadCycle();
            }
            else {
                HasCharge = false;
                AssessRechargeState();
                AssessReloadState();
            }
        }

        private void HasChargePropChangedHandler() {
            if (HasCharge) {
                CurrentCharge = MaximumCharge;
            }
            else {
                CurrentCharge = Constants.ZeroF;
                AssessReloadState();
            }
            OnHasChargeChanged();
        }

        private void OnHasChargeChanged() {
            if (hasChargeChanged != null) {
                hasChargeChanged(this, new EventArgs());
            }
        }

        #endregion

        #region Recharging

        private void AssessRechargeState() {
            if (_isRecharging) {
                if (!HasCharge || CurrentCharge == MaximumCharge) {
                    CancelRecharge();
                }
            }
            else {
                if (IsOperational && HasCharge && CurrentCharge != MaximumCharge) {
                    InitiateRechargeProcess();
                }
            }
        }

        private void InitiateRechargeProcess() {

            D.Assert(HasCharge);
            D.Assert(!_isRecharging);
            D.Log("{0}.{1} initiating recharge process. TrickleRechargeRate: {2} joules/hour.", Shield.FullName, Name, TrickleChargeRate);
            _isRecharging = true;
            string jobName = "ShieldRechargeJob";
            _rechargeJob = _jobMgr.StartGameplayJob(Recharge(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                _isRecharging = false;
                if (!jobWasKilled) {
                    D.Log("{0}.{1} completed recharging.", Shield.FullName, Name);
                }
            });
        }

        private IEnumerator Recharge() {
            while (CurrentCharge < MaximumCharge) {
                CurrentCharge += TrickleChargeRate * _gameTime.GameSpeedAdjustedHoursPerSecond * _gameTime.DeltaTime;
                yield return null;
            }
        }

        private void CancelRecharge() {
            D.Assert(_isRecharging);
            _rechargeJob.Kill();
            _isRecharging = false;
        }

        #endregion

        #region Reloading

        private void AssessReloadState() {
            if (_isReloading) {
                if (!IsOperational || HasCharge) {
                    CancelReload();
                }
            }
            else {
                if (IsOperational && !HasCharge) {
                    InitiateReloadCycle();
                }
            }
        }

        private void InitiateReloadCycle() {
            D.Assert(!HasCharge);
            D.Assert(!_isReloading);
            _isReloading = true;
            D.Log("{0}.{1} is initiating its reload cycle. Duration: {2:0.} hours.", Shield.FullName, Name, ReloadPeriod);
            string jobName = "{0}.ReloadJob".Inject(FullName);
            _reloadJob = _jobMgr.WaitForHours(ReloadPeriod, jobName, waitFinished: (jobWasKilled) => {
                _isReloading = false;
                if (!jobWasKilled) {
                    D.Log("{0}.{1} completed reload.", Shield.FullName, Name);
                    HandleReloaded();
                }
            });
        }

        private void CancelReload() {
            D.Assert(_isReloading);
            _reloadJob.Kill();
            _isReloading = false;
        }

        #endregion

        // 8.12.16 Job pausing moved to JobManager to consolidate handling

        private void Cleanup() {
            //D.Log("{0}.Cleanup() called.", FullName);
            if (_reloadJob != null) {   // can be null if element is destroyed before Running
                //D.Log("{0} reloadJob.Dispose() being called.", FullName);
                _reloadJob.Dispose();
            }
            if (_rechargeJob != null) {
                _rechargeJob.Dispose();
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

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

