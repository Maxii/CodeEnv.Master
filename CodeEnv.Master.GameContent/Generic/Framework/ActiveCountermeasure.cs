// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ActiveCountermeasure.cs
// Countermeasure that has a PassiveCountermeasure's DamageMitigation capability 
// combined with the ability to intercept a weapon delivery vehicle.
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

    /// <summary>
    /// Countermeasure that has a PassiveCountermeasure's DamageMitigation capability combined with the ability to intercept a weapon delivery vehicle.
    /// </summary>
    public class ActiveCountermeasure : ARangedEquipment, ICountermeasure, IDisposable {

        private static string _editorNameFormat = "{0}[{1}({2:0.})]";

        /// <summary>
        /// Occurs when IsReadyToInterceptAThreat changes.
        /// </summary>
        public event Action<ActiveCountermeasure> onIsReadyToInterceptAThreatChanged;

        /// <summary>
        /// Occurs when a qualified incoming ordnance threat enters this operational 
        /// countermeasure's range. Only raised when the countermeasure IsOperational.
        /// </summary>
        public event Action<ActiveCountermeasure> onThreatEnteringRange;

        private bool _isReadyToInterceptAThreat;
        /// <summary>
        /// Indicates whether this countermeasure is ready to intercept a threat. A countermeasure is ready when 
        /// it is both operational and loaded. This property is not affected by whether 
        /// there are any threats within range.
        /// </summary>
        public bool IsReadyToInterceptAThreat {
            get { return _isReadyToInterceptAThreat; }
            private set { SetProperty<bool>(ref _isReadyToInterceptAThreat, value, "IsReadyToInterceptAThreat", OnIsReadyToInterceptAThreatChanged); }
        }

        /// <summary>
        /// Indicates whether there are one or more qualified enemy targets within range.
        /// </summary>
        public bool IsThreatInRange { get { return _qualifiedThreats.Any(); } }

        public IActiveCountermeasureRangeMonitor RangeMonitor { get; set; }

        public override string Name {
            get {
#if UNITY_EDITOR
                return _editorNameFormat.Inject(base.Name, RangeCategory.GetEnumAttributeText(), RangeDistance);
#else 
                return base.Name;
#endif
            }
        }

        public float ReloadPeriod {
            get {
                float reloadPeriodMultiplier = RangeMonitor != null ? RangeMonitor.Owner.CountermeasureReloadPeriodMultiplier : Constants.OneF;
                return Stat.ReloadPeriod * reloadPeriodMultiplier;
            }
        }

        public WDVStrength InterceptStrength { get { return Stat.InterceptStrength; } }

        public DamageStrength DamageMitigation { get { return Stat.DamageMitigation; } }

        public float InterceptAccuracy { get { return Stat.InterceptAccuracy; } }

        protected override float RangeMultiplier {
            get { return RangeMonitor != null ? RangeMonitor.Owner.CountermeasureRangeMultiplier : Constants.OneF; }
        }

        /// <summary>
        /// The list of enemy targets in range that qualify as targets of this weapon.
        /// </summary>
        private IList<IInterceptableOrdnance> _qualifiedThreats;
        private bool _isLoaded;
        private WaitJob _reloadJob;

        protected new ActiveCountermeasureStat Stat { get { return base.Stat as ActiveCountermeasureStat; } }

        public ActiveCountermeasure(ActiveCountermeasureStat stat)
            : base(stat) {
            _qualifiedThreats = new List<IInterceptableOrdnance>();
        }

        // Copy Constructor makes no sense when a RangeMonitor must be attached

        /*****************************************************************************************************************************************
                    * This countermeasure does not need to track Owner changes. When the owner of the item with this countermeasure changes, the countermeasure's 
                    * RangeMonitor drops and then reacquires all detectedItems. As a result, all reacquired items are categorized correctly. 
                    * When the owner of an item detected by this countermeasure changes, the Monitor recategorizes the detectedItem into the right list 
                    * taking appropriate action as a result.
                    *****************************************************************************************************************************************/

        /**********************************************************************************************************************************************
                     * ParentDeath Note: No need to track it as the parent element will turn off the operational state of all equipment when it initiates dying.
                     *********************************************************************************************************************************************/

        /// <summary>
        /// Tries to pick the best (most advantageous) qualified target in range.
        /// Returns <c>true</c> if a target was picked, <c>false</c> otherwise.
        /// The hint provided is the initial choice for primary target as determined
        /// by the Element. It is within sensor range (although not necessarily weapons
        /// range) and is associated with the target indicated by Command that our
        /// Element is to attack.
        /// </summary>
        /// <param name="hint">The hint.</param>
        /// <param name="threatPicked">The enemy target picked.</param>
        /// <returns></returns>
        public bool TryPickMostDangerousThreat(out IInterceptableOrdnance threatPicked) {
            if (_qualifiedThreats.Count == Constants.ZeroF) {
                threatPicked = null;
                return false;
            }
            threatPicked = _qualifiedThreats.First();   // IMPROVE closest? biggest payload?, most vulnerable?
            return true;
        }

        /// <summary>
        /// Fires this active countermeasure against the provided WDV threat.
        /// Returns <c>true</c> if the threat was hit, <c>false</c> if not.
        /// </summary>
        /// <param name="threat">The threat.</param>
        /// <returns></returns>
        public bool Fire(IInterceptableOrdnance threat) {
            OnFiringInitiated(threat);

            D.Log("{0} is attempting to fire on {1}.", Name, threat.Name);
            bool threatHit = false;
            float hitChance = InterceptAccuracy;
            if (RandomExtended.Chance(hitChance)) {
                threatHit = true;
                threat.TakeHit(InterceptStrength);
            }
            OnFiringComplete();
            return threatHit;
        }

        /// <summary>
        /// Called by this weapon's RangeMonitor when an enemy target enters or exits the weapon's range.
        /// </summary>
        /// <param name="threat">The enemy threat.</param>
        /// <param name="isInRange">if set to <c>true</c> [is in range].</param>
        public void OnThreatInRangeChanged(IInterceptableOrdnance threat, bool isInRange) {
            D.Log("{0} received OnThreatInRangeChanged. Threat: {1}, InRange: {2}.", Name, threat.Name, isInRange);
            if (isInRange) {
                if (CheckIfQualified(threat)) {
                    D.Assert(!_qualifiedThreats.Contains(threat));
                    _qualifiedThreats.Add(threat);
                    if (IsOperational) {
                        OnThreatEnteringRange(threat);
                    }
                }
            }
            else {
                if (_qualifiedThreats.Contains(threat)) {
                    _qualifiedThreats.Remove(threat);
                }
            }
        }

        private void OnThreatEnteringRange(IInterceptableOrdnance threat) {
            D.Assert(_qualifiedThreats.Contains(threat));
            if (onThreatEnteringRange != null) {
                D.Log("{0} is raising onThreatEnteringRange event.", Name);
                onThreatEnteringRange(this);
            }
        }

        /// <summary>
        /// Called when this weapon's firing process against <c>targetFiredOn</c> has begun.
        /// </summary>
        /// <param name="threatFiredOn">The target fired on.</param>
        private void OnFiringInitiated(IInterceptableOrdnance threatFiredOn) {
            D.Assert(IsOperational, "{0} fired at {1} while not operational.".Inject(Name, threatFiredOn.Name));
            D.Assert(_qualifiedThreats.Contains(threatFiredOn));

            _isLoaded = false;
            AssessReadiness();
        }

        /// <summary>
        /// Called when this weapon's firing process launching the provided ordnance is complete. Projectile
        /// and Missile Weapons initiate and complete the firing process at the same time. Beam Weapons
        /// don't complete the firing process until their Beam is terminated.
        /// </summary>
        private void OnFiringComplete() {
            D.Assert(!_isLoaded);

            UnityUtility.WaitOneToExecute(onWaitFinished: () => {
                // give time for _reloadJob to exit before starting another
                InitiateReloadCycle();
            });
        }

        protected override void OnIsOperationalChanged() {
            D.Log("{0}.IsOperational changed to {1}.", Name, IsOperational);
            if (IsOperational) {
                // just became operational so if not already loaded, reload
                if (!_isLoaded) {
                    InitiateReloadCycle();
                }
            }
            else {
                // just lost operational status so kill any reload in process
                if (_reloadJob != null && _reloadJob.IsRunning) {
                    _reloadJob.Kill();
                }
            }
            AssessReadiness();
            NotifyIsOperationalChanged();
        }

        private void OnIsReadyToInterceptAThreatChanged() {
            if (onIsReadyToInterceptAThreatChanged != null) {
                onIsReadyToInterceptAThreatChanged(this);
            }
        }

        private void OnReloaded() {
            D.Log("{0}.{1} completed reload.", RangeMonitor.Name, Name);
            _isLoaded = true;
            AssessReadiness();
        }

        private bool CheckIfQualified(IInterceptableOrdnance enemyTarget) {
            bool isQualified = InterceptStrength.Category == enemyTarget.DeliveryVehicleStrength.Category;
            D.Log("{0} isQualified = {1}, Vehicles: {2}, {3}.", Name, isQualified, InterceptStrength.Category.GetValueName(), enemyTarget.DeliveryVehicleStrength.Category.GetValueName());
            return isQualified;
        }

        private void InitiateReloadCycle() {
            D.Log("{0} is initiating its reload cycle. Duration: {1} hours.", Name, ReloadPeriod);
            if (_reloadJob != null && _reloadJob.IsRunning) {
                // UNCLEAR can this happen?
                D.Warn("{0}.{1}.InitiateReloadCycle() called while already Running.", RangeMonitor.Name, Name);
            }
            _reloadJob = GameUtility.WaitForHours(ReloadPeriod, onWaitFinished: (jobWasKilled) => {
                OnReloaded();
            });
        }

        private void AssessReadiness() {
            IsReadyToInterceptAThreat = IsOperational && _isLoaded;
        }

        private void Cleanup() {
            if (_reloadJob != null) {   // can be null if element is destroyed before Running
                _reloadJob.Dispose();
            }
        }

        public sealed override string ToString() { return Stat.ToString(); }

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

