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
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// An Element's ShieldGenerator. Can be Short, Medium or Long range.
    /// </summary>
    public class ShieldGenerator : ARangedEquipment, ICountermeasure {

        private static float _hoursPerSecond = GameTime.HoursPerSecond;
        private static string _editorNameFormat = "{0}[{1}({2:0.})]";

        public event Action<ShieldGenerator> onHasChargeChanged;

        public override string Name {
            get {
#if UNITY_EDITOR
                return _editorNameFormat.Inject(base.Name, RangeCategory.GetEnumAttributeText(), RangeDistance);
#else 
                return base.Name;
#endif
            }
        }

        public IShield Shield { get; set; }

        public float ReloadPeriod {
            get {
                float reloadPeriodMultiplier = Shield != null ? Shield.Owner.CountermeasureReloadPeriodMultiplier : Constants.OneF;
                return Stat.ReloadPeriod * reloadPeriodMultiplier;
            }
        }

        protected override float RangeMultiplier {
            get { return Shield != null ? Shield.Owner.CountermeasureRangeMultiplier : Constants.OneF; }
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
            private set { SetProperty<bool>(ref _hasCharge, value, "HasCharge", OnHasChargeChanged); }
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

        private GameTime _gameTime;
        private WaitJob _reloadJob;
        private Job _rechargeJob;
        private bool _isRecharging;
        private bool _isReloading;

        public ShieldGenerator(ShieldGeneratorStat stat)
            : base(stat) {
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

            D.Log("{0}.{1} with charge {2.0.#) is attempting to absorb a {3:0.#} impact.", Shield.Name, Name, CurrentCharge, deliveryVehicleImpactValue);
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
            D.Warn(!HasCharge, "{0}.{1} has failed.", Shield.Name, Name);
            return isHitCompletelyAbsorbed;
        }

        private void OnReloaded() {
            HasCharge = true;
        }

        protected override void OnIsOperationalChanged() {
            base.OnIsOperationalChanged();
            if (IsOperational) {
                InitiateReloadCycle();
            }
            else {
                HasCharge = false;
                AssessRechargeState();
                AssessReloadState();
            }
        }

        private void OnHasChargeChanged() {
            if (HasCharge) {
                CurrentCharge = MaximumCharge;
            }
            else {
                CurrentCharge = Constants.ZeroF;
                AssessReloadState();
            }
            if (onHasChargeChanged != null) {
                onHasChargeChanged(this);
            }
        }

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
            D.Log("{0}.{1} initiating recharge process. TrickleRechargeRate: {2} joules/hour.", Shield.Name, Name, TrickleChargeRate);
            _isRecharging = true;
            _rechargeJob = new Job(Recharge(), toStart: true, onJobComplete: (jobWasKilled) => {
                _isRecharging = false;
                if (!jobWasKilled) {
                    D.Log("{0}.{1} completed recharging.", Shield.Name, Name);
                }
            });
        }

        private IEnumerator Recharge() {
            while (CurrentCharge < MaximumCharge) {
                CurrentCharge += TrickleChargeRate * _gameTime.GameSpeedAdjustedDeltaTimeOrPaused / _hoursPerSecond;
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
            D.Log("{0}.{1} is initiating its reload cycle. Duration: {2:0.} hours.", Shield.Name, Name, ReloadPeriod);
            _reloadJob = GameUtility.WaitForHours(ReloadPeriod, onWaitFinished: (jobWasKilled) => {
                _isReloading = false;
                if (!jobWasKilled) {
                    D.Log("{0}.{1} completed reload.", Shield.Name, Name);
                    OnReloaded();
                }
            });
        }

        private void CancelReload() {
            D.Assert(_isReloading);
            _reloadJob.Kill();
            _isReloading = false;
        }

        #endregion

        private void Cleanup() {
            if (_reloadJob != null) {   // can be null if element is destroyed before Running
                _reloadJob.Dispose();
            }
            if (_rechargeJob != null) {
                _rechargeJob.Dispose();
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

