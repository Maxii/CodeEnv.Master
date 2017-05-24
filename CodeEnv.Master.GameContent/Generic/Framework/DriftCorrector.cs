// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DriftCorrector.cs
// General purpose class that corrects drift from using Unity's PhysX propulsion system.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// General purpose class that corrects drift from using Unity's PhysX propulsion system.
    /// </summary>
    public class DriftCorrector : IDisposable {

        private const float CorrectionFactor = 50F;

        private const string DebugNameFormat = "{0}.{1}";

        /// <summary>
        /// The value that DriftVelocityPerSec.sqrMagnitude must 
        /// be reduced too via thrust before the drift velocity value can manually be negated.
        /// </summary>
        private static float DriftVelocityInUnitsPerSecSqrMagnitudeThreshold {
            get {
                var acceptableDriftVelocityMagnitudeInUnitsPerHour = Constants.OneF;
                var acceptableDriftVelocityMagnitudeInUnitsPerSec = acceptableDriftVelocityMagnitudeInUnitsPerHour * _gameTime.GameSpeedAdjustedHoursPerSecond;
                return acceptableDriftVelocityMagnitudeInUnitsPerSec * acceptableDriftVelocityMagnitudeInUnitsPerSec;
            }
        }

        private static GameTime _gameTime = GameTime.Instance;

        public bool IsCorrectionUnderway { get { return _driftCorrectionJob != null; } }

        public string ClientName { private get; set; }

        private string _debugName;
        public virtual string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(ClientName, typeof(DriftCorrector).Name);
                }
                return _debugName;
            }
        }

        /// <summary>
        /// The signed drift velocity (in units per second) in the lateral (x, + = right)
        /// and vertical (y, + = up) axis directions.
        /// </summary>
        private Vector2 CurrentDriftVelocityPerSec { get { return _transform.InverseTransformDirection(_rigidbody.velocity); } }

        private Job _driftCorrectionJob;
        private Rigidbody _rigidbody;
        private Transform _transform;
        private IGameManager _gameMgr;
        private IJobManager _jobMgr;

        public DriftCorrector(Transform transform, Rigidbody rigidbody, string optionalClientName = "Unknown") {
            D.Assert(!rigidbody.useGravity);    // can be isKinematic until operations commence
            ClientName = optionalClientName;
            _transform = transform;
            _rigidbody = rigidbody;
            _gameMgr = GameReferences.GameManager;
            _jobMgr = GameReferences.JobManager;
        }

        public void Engage() {
            D.AssertNull(_driftCorrectionJob);

            string jobName = "{0}.DriftCorrectionJob".Inject(DebugName);
            _driftCorrectionJob = _jobMgr.StartGameplayJob(OperateDriftCorrection(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                if (jobWasKilled) {
                    // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                    // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                    // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                    // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                    // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
                }
                else {
                    NegateRemainingDrift();
                    _driftCorrectionJob = null; // 12.16.16 moved after NegateRemainingDrift so IsCorrectionUnderway is accurate
                }
            });
        }

        private IEnumerator OperateDriftCorrection() {
            //D.Log("{0}: Initiating DriftCorrection.", DebugName);
            Vector2 cumDriftDistanceDuringCorrection = Vector2.zero;
            //int fixedUpdateCount = 0;
            Vector2 currentDriftVelocityPerSec;
            while ((currentDriftVelocityPerSec = CurrentDriftVelocityPerSec).sqrMagnitude > DriftVelocityInUnitsPerSecSqrMagnitudeThreshold) {
                //D.Log("{0}: DriftVelocity/sec at FixedUpdateCount {1} = {2}.", DebugName, fixedUpdateCount, currentDriftVelocityPerSec.ToPreciseString());
                ApplyCorrection(currentDriftVelocityPerSec);
                cumDriftDistanceDuringCorrection += currentDriftVelocityPerSec * Time.fixedDeltaTime;
                //fixedUpdateCount++;
                yield return Yielders.WaitForFixedUpdate;
            }
            if (cumDriftDistanceDuringCorrection.sqrMagnitude > 0.02F) { // HACK > 0.14 magnitude
                //D.Log("{0}: Cumulative Drift during Correction = {1}.", DebugName, cumDriftDistanceDuringCorrection.ToPreciseString());
            }
        }

        private void ApplyCorrection(Vector2 driftVelocityPerSec) {
            _rigidbody.AddRelativeForce(-driftVelocityPerSec.x * CorrectionFactor * Vector3.right, ForceMode.Force);
            _rigidbody.AddRelativeForce(-driftVelocityPerSec.y * CorrectionFactor * Vector3.up, ForceMode.Force);
        }

        private void NegateRemainingDrift() {
            //D.Log("{0}: DriftCorrection completed normally. Negating remaining drift.", DebugName);
            Vector3 localVelocity = _transform.InverseTransformDirection(_rigidbody.velocity);
            Vector3 localVelocityWithoutDrift = localVelocity.SetX(Constants.ZeroF);
            localVelocityWithoutDrift = localVelocityWithoutDrift.SetY(Constants.ZeroF);
            _rigidbody.velocity = _transform.TransformDirection(localVelocityWithoutDrift);
        }

        public void Disengage() {
            if (KillDriftCorrectionJob()) {
                //D.Log("{0}: Disengaging DriftCorrection.", DebugName);
            }
        }

        private bool KillDriftCorrectionJob() {
            if (_driftCorrectionJob != null) {
                _driftCorrectionJob.Kill();
                _driftCorrectionJob = null;
                return true;
            }
            return false;
        }

        // 8.12.16 Handling pausing for all Jobs moved to JobManager

        public sealed override string ToString() {
            return DebugName;
        }

        private void Cleanup() {
            // 12.8.16 Job Disposal centralized in JobManager
            KillDriftCorrectionJob();
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

