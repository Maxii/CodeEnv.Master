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

        //public Material material;
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
            set { SetProperty<string>(ref _lineName, value, "LineName", OnLineNameChanged); }
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

        private void OnLineNameChanged() {
            if (_line != null) {
                _line.name = LineName;
            }
        }

        protected virtual void Cleanup() {
            VectorLine.Destroy(ref _line);
            if (_drawJob != null) {
                _drawJob.Dispose();
            }
        }

        #region IDisposable

        private bool alreadyDisposed = false;

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
            if (alreadyDisposed) {
                return;
            }

            if (isDisposing) {
                // free managed resources here including unhooking events
                Cleanup();
            }
            // free unmanaged resources here

            alreadyDisposed = true;
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

