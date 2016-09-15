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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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

        private const string NameFormat = "{0}.{1}";

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

        public string ClientName { private get; set; }

        private bool IsDriftCorrectionEngaged { get { return _driftCorrectionJob != null && _driftCorrectionJob.IsRunning; } }

        private string Name { get { return NameFormat.Inject(ClientName, typeof(DriftCorrector).Name); } }

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
            _gameMgr = References.GameManager;
            _jobMgr = References.JobManager;
        }

        public void Engage() {
            D.Assert(!IsDriftCorrectionEngaged);
            string jobName = "{0}.DriftCorrectionJob".Inject(Name);
            _driftCorrectionJob = _jobMgr.StartGameplayJob(OperateDriftCorrection(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                if (!jobWasKilled) {
                    //D.Log("{0}: DriftCorrection completed normally. Negating remaining drift.", Name);
                    Vector3 localVelocity = _transform.InverseTransformDirection(_rigidbody.velocity);
                    Vector3 localVelocityWithoutDrift = localVelocity.SetX(Constants.ZeroF);
                    localVelocityWithoutDrift = localVelocityWithoutDrift.SetY(Constants.ZeroF);
                    _rigidbody.velocity = _transform.TransformDirection(localVelocityWithoutDrift);
                }
                else {
                    //D.Log("{0}: DriftCorrection killed.", Name);
                }
            });
        }

        private IEnumerator OperateDriftCorrection() {
            //D.Log("{0}: Initiating DriftCorrection.", Name);
            Vector2 cumDriftDistanceDuringCorrection = Vector2.zero;
            int fixedUpdateCount = 0;
            Vector2 currentDriftVelocityPerSec;
            while ((currentDriftVelocityPerSec = CurrentDriftVelocityPerSec).sqrMagnitude > DriftVelocityInUnitsPerSecSqrMagnitudeThreshold) {
                //D.Log("{0}: DriftVelocity/sec at FixedUpdateCount {1} = {2}.", Name, fixedUpdateCount, currentDriftVelocityPerSec.ToPreciseString());
                ApplyCorrection(currentDriftVelocityPerSec);
                cumDriftDistanceDuringCorrection += currentDriftVelocityPerSec * Time.fixedDeltaTime;
                fixedUpdateCount++;
                yield return Yielders.WaitForFixedUpdate;
            }
            if (cumDriftDistanceDuringCorrection.sqrMagnitude > 0.02F) { // HACK > 0.14 magnitude
                //D.Log("{0}: Cumulative Drift during Correction = {1}.", Name, cumDriftDistanceDuringCorrection.ToPreciseString());
            }
        }

        private void ApplyCorrection(Vector2 driftVelocityPerSec) {
            _rigidbody.AddRelativeForce(-driftVelocityPerSec.x * CorrectionFactor * Vector3.right, ForceMode.Force);
            _rigidbody.AddRelativeForce(-driftVelocityPerSec.y * CorrectionFactor * Vector3.up, ForceMode.Force);
        }

        public void Disengage() {
            if (IsDriftCorrectionEngaged) {
                //D.Log("{0}: Disengaging DriftCorrection.", Name);
                _driftCorrectionJob.Kill();
            }
        }

        // 8.12.16 Handling pausing for all Jobs moved to JobManager

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        private void Cleanup() {
            if (_driftCorrectionJob != null) {
                _driftCorrectionJob.Dispose();
            }
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

