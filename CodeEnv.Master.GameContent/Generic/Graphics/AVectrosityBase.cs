// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AVectrosityBase.cs
// Base class for all Vectrosity Classes that generate VectorObjects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;
    using Vectrosity;

    /// <summary>
    /// Base class for all Vectrosity Classes that generate VectorObjects.
    /// </summary>
    public abstract class AVectrosityBase : APropertyChangeTracking, IDisposable {

        public Texture texture;

        public bool IsShowing {
            get {
                if (_line == null || _drawJob == null) { return false; }
                return _line.active && _drawJob.IsRunning;
            }
        }

        // Note: Ability to assign a Vectrosity object to a designated Parent removed as Vectrosity 4.0's
        // use of Unity 4.6's new UICanvas requires that the objects be children of the canvas

        private string _lineName;
        public string LineName {
            get { return _lineName; }
            set { SetProperty<string>(ref _lineName, value, "LineName", LineNamePropChangedHandler); }
        }

        protected Job _drawJob;
        protected VectorLine _line;

        /// <summary>
        /// Initializes a new instance of the <see cref="AVectrosityBase" /> class.
        /// </summary>
        /// <param name="name">The name of the VectorLine.</param>
        public AVectrosityBase(string name) {
            _lineName = name;
        }

        protected abstract void Initialize();

        #region Event and Property Change Handlers

        private void LineNamePropChangedHandler() {
            if (_line != null) {
                _line.name = LineName;
            }
        }

        #endregion

        protected virtual void Cleanup() {
            VectorLine.Destroy(ref _line);
            if (_drawJob != null) {
                _drawJob.Dispose();
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

