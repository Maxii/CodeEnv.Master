// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AApTask.cs
// COMMENT - one line to give a brief idea of what the file does.
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
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    public abstract class AApTask : IDisposable {

        private const string DebugNameFormat = "{0}.{1}";

        public abstract bool IsEngaged { get; }



        protected string DebugName { get { return DebugNameFormat.Inject(_autoPilot.DebugName, GetType().Name); } }

        protected bool ShowDebugLog { get { return _autoPilot.ShowDebugLog; } }


        protected MoveAutoPilot _autoPilot;
        protected IJobManager _jobMgr;

        public AApTask(MoveAutoPilot autoPilot) {
            _autoPilot = autoPilot;
            InitializeValuesAndReferences();
        }

        protected virtual void InitializeValuesAndReferences() {
            _jobMgr = References.JobManager;
        }

        public abstract void Execute(AutoPilotDestinationProxy destProxy);

        public virtual void ResetForReuse() {
            KillJob();
        }

        protected abstract void KillJob();

        protected abstract void Cleanup();

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

