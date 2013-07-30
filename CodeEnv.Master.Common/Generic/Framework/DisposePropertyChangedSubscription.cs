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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.ComponentModel;

    /// <summary>
    /// My own IDisposable class that allows me to properly dispose of a INotifyPropertyChanged subscription.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    public class DisposePropertyChangedSubscription<TSource> : IDisposable where TSource : INotifyPropertyChanged {

        private TSource _source;
        private PropertyChangedEventHandler _handlerToUnsubscribe;

        public DisposePropertyChangedSubscription(TSource source, PropertyChangedEventHandler handlerToUnsubscribe) {
            _source = source;
            _handlerToUnsubscribe = handlerToUnsubscribe;
        }

        private void UnsubscribeHandler() {
            if (_source != null) {
                _source.PropertyChanged -= _handlerToUnsubscribe;
                D.Log("PropertyChanged handler unsubscribing from an instance of {0}.", typeof(TSource));
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable
        [DoNotSerialize]
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
                UnsubscribeHandler();
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

