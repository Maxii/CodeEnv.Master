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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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
    public class ShieldGenerator : ARangedEquipment, ICountermeasure, IDateMinderClient, IDisposable {

        /// <summary>
        /// Occurs when this ShieldGenerator changes from having any charge to having
        /// no charge, or vis-versa.
        /// </summary>
        public event EventHandler hasChargeChanged;

        public override string DebugName {
            get { return Shield != null ? DebugNameFormat.Inject(Shield.DebugName, Name) : Name; }
        }

        private IShield _shield;
        public IShield Shield {
            get { return _shield; }
            set { SetProperty<IShield>(ref _shield, value, "Shield"); }
        }

        private GameTimeDuration _reloadPeriod;
        public GameTimeDuration ReloadPeriod {
            get { return _reloadPeriod; }
            private set { SetProperty<GameTimeDuration>(ref _reloadPeriod, value, "ReloadPeriod"); }
        }

        /// <summary>
        /// The maximum absorption capacity of this generator.
        /// </summary>
        public float MaximumCharge { get { return Stat.MaximumCharge; } }

        public Player Owner { get { return Shield.Owner; } }

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

        protected bool ShowDebugLog { get { return Shield != null ? Shield.ShowDebugLog : true; } }

        protected new ShieldGeneratorStat Stat { get { return base.Stat as ShieldGeneratorStat; } }

        private bool IsReloading { get { return _reloadedDate != default(GameDate); } }

        private bool IsRecharging { get { return _rechargeJob != null; } }

        /// <summary>
        /// The date this ShieldGenerator will be reloaded.
        /// Once reloaded, this date will be default(GameDate) until the ShieldGenerator initiates reloading again.
        /// </summary>
        private GameDate _reloadedDate;
        private Job _rechargeJob;
        private GameTime _gameTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShieldGenerator"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public ShieldGenerator(ShieldGeneratorStat stat, string name = null)
            : base(stat, name) {
            _gameTime = GameTime.Instance;
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

            //D.Log(ShowDebugLog, "{0}.{1} with charge {2.0.#) is attempting to absorb a {3:0.#} impact.", Shield.DebugName, Name, CurrentCharge, deliveryVehicleImpactValue);
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
            if (!HasCharge) {
                D.Log(ShowDebugLog, "{0}.{1} has failed.", Shield.DebugName, Name);
            }
            return isHitCompletelyAbsorbed;
        }

        private void HandleReloaded() {
            HasCharge = true;
        }

        #region Event and Property Change Handlers

        protected override void HandleInitialActivation() {
            base.HandleInitialActivation();
            ReloadPeriod = new GameTimeDuration(Stat.ReloadPeriod * Owner.CountermeasureReloadPeriodMultiplier);
        }

        protected override void IsOperationalPropChangedHandler() {
            base.IsOperationalPropChangedHandler();
            if (IsOperational) {
                InitiateReloadProcess();
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
                hasChargeChanged(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Recharging

        private void AssessRechargeState() {
            if (IsRecharging) {
                if (!HasCharge || CurrentCharge == MaximumCharge) {
                    KillRechargeProcess();
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
            D.AssertNull(_rechargeJob);
            //D.Log(ShowDebugLog, "{0}.{1} initiating recharge process. TrickleRechargeRate: {2} joules/hour.", Shield.DebugName, Name, TrickleChargeRate);

            string jobName = "ShieldRechargeJob";
            _rechargeJob = _jobMgr.StartGameplayJob(Recharge(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                if (jobWasKilled) {
                    // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                    // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                    // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                    // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                    // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
                }
                else {
                    _rechargeJob = null;
                    //D.Log(ShowDebugLog, "{0}.{1} completed recharging.", Shield.DebugName, Name);
                }
            });
        }

        private IEnumerator Recharge() {
            while (CurrentCharge < MaximumCharge) {
                CurrentCharge += TrickleChargeRate * _gameTime.GameSpeedAdjustedHoursPerSecond * _gameTime.DeltaTime;
                yield return null;
            }
        }

        private void KillRechargeProcess() {
            if (_rechargeJob != null) {
                _rechargeJob.Kill();
                _rechargeJob = null;
            }
        }

        #endregion

        #region Reloading

        private void AssessReloadState() {
            if (IsReloading) {
                if (!IsOperational || HasCharge) {
                    KillReloadProcess();
                }
            }
            else {
                if (IsOperational && !HasCharge) {
                    InitiateReloadProcess();
                }
            }
        }

        private void InitiateReloadProcess() {
            D.Assert(!HasCharge);
            D.AssertDefault(_reloadedDate);
            //D.Log(ShowDebugLog, "{0} is initiating its reload cycle. Duration: {1}.", DebugName, ReloadPeriod);

            _reloadedDate = new GameDate(ReloadPeriod);
            _gameTime.DateMinder.Add(_reloadedDate, this);
        }

        private void KillReloadProcess() {
            if (_reloadedDate != default(GameDate)) {
                _gameTime.DateMinder.Remove(_reloadedDate, this);
                _reloadedDate = default(GameDate);
            }
        }

        #endregion

        // 8.12.16 Job pausing moved to JobManager to consolidate handling

        private void Cleanup() {
            // 12.8.16 Job Disposal centralized in JobManager
            KillReloadProcess();
            KillRechargeProcess();
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

        #region IDateMinderClient Members

        void IDateMinderClient.HandleDateReached(GameDate date) {
            D.AssertEqual(_reloadedDate, date);
            _reloadedDate = default(GameDate);
            HandleReloaded();
        }

        #endregion

    }
}

