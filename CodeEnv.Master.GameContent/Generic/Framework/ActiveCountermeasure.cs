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
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Countermeasure that has a PassiveCountermeasure's DamageMitigation capability combined with the ability to intercept a weapon delivery vehicle.
    /// </summary>
    public class ActiveCountermeasure : ARangedEquipment, ICountermeasure, IDisposable {

        private bool _isReady;
        private bool IsReady {
            get { return _isReady; }
            set { SetProperty<bool>(ref _isReady, value, "IsReady", IsReadyPropChangedHandler); }
        }

        private bool _isAnyThreatInRange;
        /// <summary>
        /// Indicates whether there are one or more qualified threats in range of this countermeasure.
        /// </summary>
        private bool IsAnyThreatInRange {
            get { return _isAnyThreatInRange; }
            set { SetProperty<bool>(ref _isAnyThreatInRange, value, "IsAnyThreatInRange", IsAnyThreatInRangePropChangedHandler); }
        }

        private IActiveCountermeasureRangeMonitor _rangeMonitor;
        public IActiveCountermeasureRangeMonitor RangeMonitor {
            get { return _rangeMonitor; }
            set { SetProperty<IActiveCountermeasureRangeMonitor>(ref _rangeMonitor, value, "RangeMonitor"); }
        }

        public override string FullName {
            get {
                return RangeMonitor != null ? _fullNameFormat.Inject(RangeMonitor.FullName, Name) : Name;
            }
        }

        public float ReloadPeriod {
            get {
                float reloadPeriodMultiplier = RangeMonitor != null ? RangeMonitor.Owner.CountermeasureReloadPeriodMultiplier : Constants.OneF;
                return Stat.ReloadPeriod * reloadPeriodMultiplier;
            }
        }

        public WDVStrength[] InterceptStrengths { get { return Stat.InterceptStrengths; } }

        public DamageStrength DamageMitigation { get { return Stat.DamageMitigation; } }

        public float InterceptAccuracy { get { return Stat.InterceptAccuracy; } }

        protected new ActiveCountermeasureStat Stat { get { return base.Stat as ActiveCountermeasureStat; } }

        private bool IsReloadJobRunning { get { return _reloadJob != null && _reloadJob.IsRunning; } }

        /// <summary>
        /// The list of IInterceptableOrdnance threats in range that qualify as targets of this countermeasure.
        /// </summary>
        private IList<IInterceptableOrdnance> _qualifiedThreats;
        private bool _isLoaded;
        private Job _reloadJob;
        private GameTime _gameTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveCountermeasure"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public ActiveCountermeasure(ActiveCountermeasureStat stat, string name = null)
            : base(stat, name) {
            _gameTime = GameTime.Instance;
            _qualifiedThreats = new List<IInterceptableOrdnance>();
        }

        // Copy Constructor makes no sense when a RangeMonitor must be attached

        /*****************************************************************************************************************************************
        * This countermeasure does not need to track Owner changes. When the owner of the item with this countermeasure changes, the countermeasure's 
        * RangeMonitor drops and then reacquires all detectedItems. As a result, all reacquired items are categorized correctly. 
        * When the owner of an item detected by this countermeasure changes, the Monitor re-categorizes the detectedItem into the right list 
        * taking appropriate action as a result.
        *****************************************************************************************************************************************/

        /**********************************************************************************************************************************************
        * ParentDeath Note: No need to track it as the parent element will turn off the activate state of all equipment when it initiates dying.
        *********************************************************************************************************************************************/

        /// <summary>
        /// Fires this countermeasure using the provided firingSolution which attempts to intercept an incoming threat.
        /// </summary>
        /// <param name="firingSolution">The firing solution.</param>
        /// <returns></returns>
        private bool Fire(CountermeasureFiringSolution firingSolution) {
            var threat = firingSolution.Threat;
            HandleFiringInitiated(threat);

            D.Log("{0} is firing on {1}. Qualified Threats = {2}.", FullName, threat.FullName, _qualifiedThreats.Select(t => t.FullName).Concatenate());
            bool isThreatHit = false;
            float hitChance = InterceptAccuracy;
            if (RandomExtended.Chance(hitChance)) {
                isThreatHit = true;
                var threatWdvCategory = threat.DeliveryVehicleStrength.Category;
                WDVStrength interceptStrength = GetInterceptStrength(threatWdvCategory);
                threat.TakeHit(interceptStrength);
            }
            HandleFiringComplete();
            return isThreatHit;
        }

        private WDVStrength GetInterceptStrength(WDVCategory threatWdvCategory) {
            return InterceptStrengths.Single(intS => intS.Category == threatWdvCategory);
        }

        // Note: Unlike Weapons, there is no reason to have a HandleDeclinedToFire() method as CMs on automatic 
        // should always fire on a threat if there is a FiringSolution.

        /// <summary>
        /// Called by this countermeasure's RangeMonitor when a IInterceptableOrdnance threat enters or exits its range.
        /// </summary>
        /// <param name="threat">The ordnance threat.</param>
        /// <param name="isInRange">if set to <c>true</c> [is in range].</param>
        public void HandleThreatInRangeChanged(IInterceptableOrdnance threat, bool isInRange) {
            D.Log("{0} received HandleThreatInRangeChanged. Threat: {1}, InRange: {2}.", Name, threat.FullName, isInRange);
            if (isInRange) {
                if (CheckIfQualified(threat)) {
                    D.Assert(!_qualifiedThreats.Contains(threat));
                    _qualifiedThreats.Add(threat);
                }
            }
            else {
                // Note: Some threats going out of range may not have been qualified as targets for this CM.
                // Also, a qualified threat can be destroyed (goes out of range) by other CMs before it is ever added
                // to this one, so if it is not present, it was never added to this CM because it was immediately destroyed
                // by other CMs as it was being added to them.
                if (_qualifiedThreats.Contains(threat)) {
                    _qualifiedThreats.Remove(threat);
                }
            }
            IsAnyThreatInRange = _qualifiedThreats.Any();
        }

        private void HandleReadyToFire(IList<CountermeasureFiringSolution> firingSolutions) {
            D.Assert(firingSolutions.Count >= Constants.One);    // must have one or more firingSolutions to be ready to fire
            var bestFiringSolution = PickBestFiringSolution(firingSolutions);
            bool isThreatHit = Fire(bestFiringSolution);
            //D.Log(isThreatHit, "{0} has hit threat {1}.", Name, bestFiringSolution.Threat.FullName);
        }

        /// <summary>
        /// Called when this weapon's firing process against <c>targetFiredOn</c> has begun.
        /// </summary> 
        /// <remarks>Note: Done this way to match the way I handled it with Weapons, where
        /// this was a public method called by the fired ordnance.</remarks>
        /// <param name="threatFiredOn">The target fired on.</param>
        private void HandleFiringInitiated(IInterceptableOrdnance threatFiredOn) {
            D.Assert(IsOperational, Name);
            D.Assert(_qualifiedThreats.Contains(threatFiredOn));

            _isLoaded = false;
            AssessReadiness();
        }

        /// <summary>
        /// Called when the firing process launching the ActiveCM is complete. 
        /// </summary>
        /// <remarks>Note: Done this way to match the way I handled it with Weapons, 
        /// where this was a public method called by the weapon's ordnance, and
        /// some ordnance (beams) didn't complete firing until the beam was terminated.</remarks>
        private void HandleFiringComplete() {
            D.Assert(!_isLoaded);
            InitiateReloadCycle();
        }

        #region Event and Property Change Handlers

        private void IsReadyPropChangedHandler() {
            AssessReadinessToFire();
        }

        private void IsAnyThreatInRangePropChangedHandler() {
            AssessReadinessToFire();
        }

        protected override void IsOperationalPropChangedHandler() {
            //D.Log("{0}.IsOperational changed to {1}.", Name, IsOperational);
            if (IsOperational) {
                // just became operational so if not already loaded, reload
                if (!_isLoaded) {
                    InitiateReloadCycle();
                }
            }
            else {
                // just lost operational status so kill any reload in process
                if (IsReloadJobRunning) {
                    _reloadJob.Kill();
                }
            }
            AssessReadiness();
            OnIsOperationalChanged();
        }

        #endregion

        private void HandleReloaded() {
            //D.Log("{0} completed reload.", Name);
            _isLoaded = true;
            AssessReadiness();
        }

        private bool CheckIfQualified(IInterceptableOrdnance threat) {
            bool isQualified = InterceptStrengths.Select(intS => intS.Category).Contains(threat.DeliveryVehicleStrength.Category);
            string isQualMsg = isQualified ? "is qualified" : "is not qualified";
            D.Log("{0} {1} to intercept {2} which uses a {3} WDV.", Name, isQualMsg, threat.FullName, threat.DeliveryVehicleStrength.Category.GetValueName());
            return isQualified;
        }

        private void InitiateReloadCycle() {
            //D.Log("{0} is initiating its reload cycle. Duration: {1} hours.", Name, ReloadPeriod);
            D.Assert(!IsReloadJobRunning, Name);
            string jobName = "{0}.ReloadJob".Inject(Name);
            _reloadJob = _jobMgr.WaitForHours(ReloadPeriod, jobName, waitFinished: (jobWasKilled) => {
                if (!jobWasKilled) {
                    HandleReloaded();
                }
            });
        }

        private CountermeasureFiringSolution PickBestFiringSolution(IList<CountermeasureFiringSolution> firingSolutions) {
            return firingSolutions.First();     // IMPROVE closest? biggest payload?, most damaged?, softerTarget? greaterHitPwr?
        }

        /// <summary>
        /// Tries to get firing solutions on all the qualified threats in range. Returns <c>true</c> if one or more
        /// firing solutions were found, <c>false</c> otherwise. 
        /// Note: In this version, for each qualified threat, there is NO chance that the CM can't 'bear' on the threat, resulting in no firing solution.
        /// </summary>
        /// <param name="firingSolutions">The firing solutions.</param>
        /// <returns></returns>
        private bool TryGetFiringSolutions(out IList<CountermeasureFiringSolution> firingSolutions) {
            int threatCount = _qualifiedThreats.Count;
            D.Assert(threatCount > Constants.Zero);
            firingSolutions = new List<CountermeasureFiringSolution>(threatCount);
            foreach (var threat in _qualifiedThreats) {
                CountermeasureFiringSolution firingSolution = new CountermeasureFiringSolution(this, threat);
                firingSolutions.Add(firingSolution);
            }
            return firingSolutions.Any();
        }

        private void AssessReadiness() {
            IsReady = IsOperational && _isLoaded;
        }

        private void AssessReadinessToFire() {
            if (!IsReady || !IsAnyThreatInRange) {
                return;
            }

            IList<CountermeasureFiringSolution> firingSolutions;
            if (TryGetFiringSolutions(out firingSolutions)) {
                HandleReadyToFire(firingSolutions);
            }
            else {
                // With one or more qualified threats in range, there should always be a firing solution
                // This is because all ActiveCMs now always can bear on their target
                D.Error("{0} was unable to find any FiringSolutions.", Name);
            }
        }

        private void Cleanup() {
            if (_reloadJob != null) {   // can be null if element is destroyed before Running
                _reloadJob.Dispose();
            }
        }

        public sealed override string ToString() { return Stat.ToString(); }

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

        #region Firing Solutions Check Job Archive

        //private Job _checkForFiringSolutionsJob;

        /// <summary>
        /// Tries to get firing solutions on all the qualified threats in range. Returns <c>true</c> if one or more
        /// firing solutions were found, <c>false</c> otherwise. For each qualified threat, there is a finite chance
        /// that the CM can't 'bear' on the threat, resulting in no firing solution.
        /// </summary>
        /// <param name="firingSolutions">The firing solutions.</param>
        /// <returns></returns>
        //private bool TryGetFiringSolutions(out IList<CountermeasureFiringSolution> firingSolutions) {
        //    int threatCount = _qualifiedThreats.Count;
        //    D.Assert(threatCount > Constants.Zero);
        //    float chanceOfBearingOnThreat = Stat.EngagePercent;
        //    firingSolutions = new List<CountermeasureFiringSolution>(threatCount);
        //    foreach (var threat in _qualifiedThreats) {
        //        bool canBearOnThreat = RandomExtended.Chance(chanceOfBearingOnThreat);
        //        if (canBearOnThreat) {
        //            CountermeasureFiringSolution firingSolution = new CountermeasureFiringSolution(this, threat);
        //            firingSolutions.Add(firingSolution);
        //        }
        //    }
        //    return firingSolutions.Any();
        //}

        //private void IsReadyPropChangedHandler() {
        //    if (!IsReady) {
        //        KillFiringSolutionsCheckJob();
        //    }
        //    AssessReadinessToFire();
        //}

        //private void IsAnyThreatInRangePropChangedHandler() {
        //    if (!IsAnyThreatInRange) {
        //        KillFiringSolutionsCheckJob();
        //    }
        //    AssessReadinessToFire();
        //}

        //private void AssessReadinessToFire() {
        //    if (!IsReady || !IsAnyThreatInRange) {
        //        return;
        //    }

        //    IList<CountermeasureFiringSolution> firingSolutions;
        //    if (TryGetFiringSolutions(out firingSolutions)) {
        //        HandleReadyToFire(firingSolutions);
        //    }
        //    else {
        //        LaunchFiringSolutionsCheckJob();
        //    }
        //}

        /// <summary>
        /// Launches a process to continuous check for newly uncovered firing solutions
        /// against threats in range. Only initiated when the countermeasure is ready to fire with
        /// qualified threats in range. If either of these conditions change, the job is immediately
        /// killed using KillFiringSolutionsCheckJob().
        /// <remarks>This fill-in check job is needed as firing solution checks otherwise
        /// occur only when 1) the CM becomes ready to fire, or 2) the first qualified threat comes
        /// into range. If a firing solution is not discovered during these event checks, no more
        /// checks would take place until another one of the above conditions arise. This process fills that
        /// gap, continuously looking for newly uncovered firing solutions which are to be
        /// expected, given movement and attitude changes of both the firing element and
        /// the threats.</remarks>
        /// </summary>
        //private void LaunchFiringSolutionsCheckJob() {
        //    KillFiringSolutionsCheckJob();
        //    D.Assert(IsReady);
        //    D.Assert(IsAnyThreatInRange);
        //    //D.Log("{0}: Launching FiringSolutionsCheckJob.", Name);
        //    _checkForFiringSolutionsJob = new Job(CheckForFiringSolutions(), toStart: true, onJobComplete: (jobWasKilled) => {
        //        //TODO
        //    });
        //}

        /// <summary>
        /// Continuously checks for firing solutions against any qualified threat in range. When it finds
        /// one or more, it signals the CM's readiness to fire and the job terminates.
        /// </summary>
        /// <returns></returns>
        //private IEnumerator CheckForFiringSolutions() {
        //    bool hasFiringSolutions = false;
        //    while (!hasFiringSolutions) {
        //        IList<CountermeasureFiringSolution> firingSolutions;
        //        if (TryGetFiringSolutions(out firingSolutions)) {
        //            hasFiringSolutions = true;
        //            //D.Log("{0}.CheckForFiringSolutions() Job has uncovered one or more firing solutions.", Name);
        //            HandleReadyToFire(firingSolutions);
        //        }
        //        // OPTIMIZE can also handle this changeable waitDuration by subscribing to a GameSpeed change
        //        var waitDuration = TempGameValues.HoursBetweenFiringSolutionChecks / _gameTime.GameSpeedAdjustedHoursPerSecond;
        //        yield return new WaitForSeconds(waitDuration);
        //    }
        //}

        //private void KillFiringSolutionsCheckJob() {
        //    if (_checkForFiringSolutionsJob != null && _checkForFiringSolutionsJob.IsRunning) {
        //        //D.Log("{0} FiringSolutionsCheckJob is being killed.", Name);
        //        _checkForFiringSolutionsJob.Kill();
        //    }
        //}

        //private void Cleanup() {
        //    if (_reloadJob != null) {   // can be null if element is destroyed before Running
        //        _reloadJob.Dispose();
        //    }
        //    if (_checkForFiringSolutionsJob != null) {
        //        _checkForFiringSolutionsJob.Dispose();
        //    }
        //}



        #endregion

    }
}

