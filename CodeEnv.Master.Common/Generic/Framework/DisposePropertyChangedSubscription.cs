// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DisposePropertyChangedSubscription.cs
// My own IDisposable class that allows me to properly dispose of a INotifyPropertyChanged subscription.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.ComponentModel;

    /// <summary>
    /// My own IDisposable class that allows me to properly dispose of a INotifyPropertyChanged subscription.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    public class DisposePropertyChangedSubscription<TSource> : IDisposable where TSource : INotifyPropertyChanged {

        public string DebugName { get { return GetType().Name; } }

        public TSource Source { get; private set; }
        private PropertyChangedEventHandler _handlerToUnsubscribe;

        public DisposePropertyChangedSubscription(TSource source, PropertyChangedEventHandler handlerToUnsubscribe) {
            Source = source;
            _handlerToUnsubscribe = handlerToUnsubscribe;
        }

        private void UnsubscribeHandler() {
            if (Source != null) {
                Source.PropertyChanged -= _handlerToUnsubscribe;
                //D.Log("PropertyChanged handler unsubscribing from an instance of {0}.", typeof(TSource));
            }
        }

        public override string ToString() {
            return DebugName;
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

        protected virtual void Dispose(bool isExplicitlyDisposing) {
            // Allows Dispose(isDisposing) to be called more than once
            if (_alreadyDisposed) {
                D.Warn("{0} has already been disposed.", GetType().Name);
                return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
            }

            if (isExplicitlyDisposing) {
                // Dispose of managed resources here as you have called Dispose() explicitly
                UnsubscribeHandler();
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            _alreadyDisposed = true;

        }

        #endregion



    }
}

