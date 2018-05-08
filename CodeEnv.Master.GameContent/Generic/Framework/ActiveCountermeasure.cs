﻿// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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
    public class ActiveCountermeasure : ARangedEquipment, ICountermeasure, IDateMinderClient, IDisposable {

        public override string DebugName {
            get {
                return RangeMonitor != null ? DebugNameFormat.Inject(RangeMonitor.DebugName, Name) : Name;
            }
        }

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

        private GameTimeDuration _reloadPeriod;
        public GameTimeDuration ReloadPeriod {
            get { return _reloadPeriod; }
            private set { SetProperty<GameTimeDuration>(ref _reloadPeriod, value, "ReloadPeriod"); }
        }

        public DamageStrength InterceptStrength { get { return Stat.InterceptStrength; } }

        public DamageStrength DmgMitigation { get { return Stat.DamageMitigation; } }

        public CountermeasureAccuracy InterceptAccy { get { return Stat.InterceptAccy; } }

        public Player Owner { get { return RangeMonitor.Owner; } }

        protected bool ShowDebugLog { get { return RangeMonitor != null ? RangeMonitor.ShowDebugLog : true; } }

        protected new ActiveCountermeasureStat Stat { get { return base.Stat as ActiveCountermeasureStat; } }

        /// <summary>
        /// The list of IInterceptableOrdnance threats in range that qualify as targets of this countermeasure.
        /// </summary>
        private HashSet<IInterceptableOrdnance> _qualifiedThreats;

        /// <summary>
        /// The date this CM will be reloaded.
        /// Once reloaded, this date will be default(GameDate) until the CM initiates reloading again.
        /// </summary>
        private GameDate _reloadedDate;
        private bool _isLoaded;
        private GameTime _gameTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveCountermeasure"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public ActiveCountermeasure(ActiveCountermeasureStat stat, string name = null)
            : base(stat, name) {
            _gameTime = GameTime.Instance;
            _qualifiedThreats = new HashSet<IInterceptableOrdnance>();
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

            //D.Log(ShowDebugLog, "{0} is firing on {1}. Qualified Threats = {2}.", DebugName, threat.DebugName, _qualifiedThreats.Select(t => t.DebugName).Concatenate());
            bool isThreatHit = false;

            EquipmentCategory threatCat = threat.WeaponCategory;
            float hitChance = InterceptAccy.GetAccuracy(threatCat);
            if (RandomExtended.Chance(hitChance)) {
                isThreatHit = true;
                threat.TakeHit(InterceptStrength);
            }

            HandleFiringComplete();
            return isThreatHit;
        }

        // Note: Unlike Weapons, there is no reason to have a HandleDeclinedToFire() method as CMs on automatic 
        // should always fire on a threat if there is a FiringSolution.

        /// <summary>
        /// Called by this countermeasure's RangeMonitor when a IInterceptableOrdnance threat enters or exits its range.
        /// </summary>
        /// <param name="threat">The ordnance threat.</param>
        /// <param name="isInRange">if set to <c>true</c> [is in range].</param>
        public void HandleThreatInRangeChanged(IInterceptableOrdnance threat, bool isInRange) {
            //D.Log(ShowDebugLog, "{0} received HandleThreatInRangeChanged. Threat: {1}, InRange: {2}.", DebugName, threat.DebugName, isInRange);
            if (isInRange) {
                if (CheckIfQualified(threat)) {
                    bool isAdded = _qualifiedThreats.Add(threat);
                    D.Assert(isAdded);

                    __TryAddToPeakThreatsInRange();
                }
            }
            else {
                // Note: Some threats going out of range may not have been deemed a threat to this CM's element and therefore 
                // never added. Even if it was added, it might not be qualified as a target for this CM.
                // Also, a qualified threat can be destroyed (goes out of range) by other CMs before it is ever added
                // to this one.
                _qualifiedThreats.Remove(threat);
            }
            IsAnyThreatInRange = _qualifiedThreats.Any();
        }

        private void HandleReadyToFire(IList<CountermeasureFiringSolution> firingSolutions) {
            D.Assert(firingSolutions.Count >= Constants.One);    // must have one or more firingSolutions to be ready to fire
            var bestFiringSolution = PickBestFiringSolution(firingSolutions);
            bool isThreatHit = Fire(bestFiringSolution);
            //D.Log(ShowDebugLog && isThreatHit, "{0} has hit threat {1}.", DebugName, bestFiringSolution.Threat.DebugName);
        }

        /// <summary>
        /// Called when this weapon's firing process against <c>targetFiredOn</c> has begun.
        /// </summary> 
        /// <remarks>Note: Done this way to match the way I handled it with Weapons, where
        /// this was a public method called by the fired ordnance.</remarks>
        /// <param name="threatFiredOn">The target fired on.</param>
        private void HandleFiringInitiated(IInterceptableOrdnance threatFiredOn) {
            D.Assert(IsOperational, DebugName);
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
            InitiateReloadProcess();
        }

        #region Event and Property Change Handlers

        protected override void HandleInitialActivation() {
            base.HandleInitialActivation();
            // IMPROVE If/when Owner's ReloadPeriodMultiplier can change, ReloadPeriod will need to change with it
            ReloadPeriod = new GameTimeDuration(Stat.ReloadPeriod * Owner.CmReloadPeriodMultiplier);
        }

        private void IsReadyPropChangedHandler() {
            AssessReadinessToFire();
        }

        private void IsAnyThreatInRangePropChangedHandler() {
            AssessReadinessToFire();
        }

        protected override void IsOperationalPropChangedHandler() {
            //D.Log(ShowDebugLog, "{0}.IsOperational changed to {1}.", DebugName, IsOperational);
            if (IsOperational) {
                // just became operational so if not already loaded, reload
                if (!_isLoaded) {
                    InitiateReloadProcess();
                }
            }
            else {
                // just lost operational status so kill any reload in process
                KillReloadProcess();
            }
            AssessReadiness();
            OnIsOperationalChanged();
        }

        #endregion

        private void HandleReloaded() {
            //D.Log(ShowDebugLog, "{0} completed reload.", DebugName);
            _isLoaded = true;
            AssessReadiness();
        }

        private bool CheckIfQualified(IInterceptableOrdnance threat) {
            bool isQualified = InterceptAccy.GetAccuracy(threat.WeaponCategory).IsGreaterThan(Constants.ZeroPercent);
            string isQualMsg = isQualified ? "is qualified" : "is not qualified";
            D.Log(ShowDebugLog, "{0} {1} to intercept {2}.", Name, isQualMsg, threat.DebugName);
            return isQualified;
        }

        private void InitiateReloadProcess() {
            //D.Log(ShowDebugLog, "{0} is initiating its reload process. Duration: {1}.", DebugName, ReloadPeriod);
            D.AssertDefault(_reloadedDate);
            _reloadedDate = new GameDate(ReloadPeriod);
            _gameTime.DateMinder.Add(_reloadedDate, this);
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
                D.Error("{0} was unable to find any FiringSolutions.", DebugName);
            }
        }

        private void KillReloadProcess() {
            if (_reloadedDate != default(GameDate)) {
                _gameTime.DateMinder.Remove(_reloadedDate, this);
                _reloadedDate = default(GameDate);
            }
        }

        public override bool AreSpecsEqual(AEquipmentStat otherStat) {
            return Stat == otherStat as ActiveCountermeasureStat;
        }

        private void Cleanup() {
            // 12.8.16 Job Disposal centralized in JobManager
            KillReloadProcess();
        }

        #region Debug

        private static int __peakThreatsInRangeCount;

        private void __TryAddToPeakThreatsInRange() {
            if (_qualifiedThreats.Count > __peakThreatsInRangeCount) {
                __peakThreatsInRangeCount = _qualifiedThreats.Count;
            }
        }

        public static void __ReportPeakThreatsInRange() {
            Debug.LogFormat("ActiveCountermeasures report PeakThreatsInRange = {0}.", __peakThreatsInRangeCount);
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
        //    //D.Log("{0}: Launching FiringSolutionsCheckJob.", DebugName);
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
        //            //D.Log("{0}.CheckForFiringSolutions() Job has uncovered one or more firing solutions.", DebugName);
        //            HandleReadyToFire(firingSolutions);
        //        }
        //        // OPTIMIZE can also handle this changeable waitDuration by subscribing to a GameSpeed change
        //        var waitDuration = TempGameValues.HoursBetweenFiringSolutionChecks / _gameTime.GameSpeedAdjustedHoursPerSecond;
        //        yield return new WaitForSeconds(waitDuration);
        //    }
        //}

        //private void KillFiringSolutionsCheckJob() {
        //    if (_checkForFiringSolutionsJob != null && _checkForFiringSolutionsJob.IsRunning) {
        //        //D.Log("{0} FiringSolutionsCheckJob is being killed.", DebugName);
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

        #region IDateMinderClient Members

        void IDateMinderClient.HandleDateReached(GameDate date) {
            D.AssertEqual(_reloadedDate, date);
            _reloadedDate = default(GameDate);
            HandleReloaded();
        }

        #endregion

    }
}

