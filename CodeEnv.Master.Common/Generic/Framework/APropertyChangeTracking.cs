// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
// 
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APropertyChangeTracking.cs
// Abstract base class for classes that wish to communicate changes to their properties. 
// </summary> 
// <remarks>
// Partially derived from material by Daniel Moore Copyright (C) 2011:
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files
// (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, 
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished 
// to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies 
// or substantial portions of the Software.
// </remarks>
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    /// <summary>
    /// Abstract base class for classes that wish to communicate changes to their properties. Capabilities include 1) knowing when one or more of their properties have changed ,and 2)
    /// notifying subscribers of said change both before and after it occurs. To subscribe: publisher.SubscribeToPropertyChang{ed, ing]&lt;TSource, TProp&gt;(pub => pub.Foo, OnFooChang[ed, ing]);
    /// Implement OnFooChang[ed, ing]() in the subscriber. There is no need to accomodate a XXXArgs parameter containing the property name as
    /// the OnFooChang[ed, ing] method is property name specific.
    /// </summary>
    public abstract class APropertyChangeTracking : IChangeTracking, INotifyPropertyChanged, INotifyPropertyChanging {

        /// <summary>
        /// Sets the properties backing field to the new value if it has changed and raises PropertyChanged and PropertyChanging
        /// events to any subscribers. Also provides local method access for doing any additional processing work that should be
        /// done outside the setter. This is useful when you have dependant properties in the same object that should change as a 
        /// result of the initial property change.
        /// </summary>
        /// <typeparam name="T">Property Type</typeparam>
        /// <param name="backingStore">The backing store field.</param>
        /// <param name="value">The proposed new value.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="onChanged">Optional local method to call when the property is changed.</param>
        /// <param name="onChanging">Optional local method to call before the property is changed. The proposed new value is provided as the parameter.</param>
        protected void SetProperty<T>(ref T backingStore, T value, string propertyName, Action onChanged = null, Action<T> onChanging = null) {
            VerifyCallerIsProperty(propertyName);
            if (EqualityComparer<T>.Default.Equals(backingStore, value)) {
                TryWarn<T>(backingStore, value, propertyName);
                return;
            }
            D.Log("SetProperty called. {0} changing to {1}.", propertyName, value);

            if (onChanging != null) { onChanging(value); }
            OnPropertyChanging(propertyName, value);

            backingStore = value;

            if (onChanged != null) { onChanged(); }
            _isChanged = true;
            OnPropertyChanged(propertyName);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void TryWarn<T>(T backingStore, T value, string propertyName) {
            if (!typeof(T).IsValueType) {
                if (DebugSettings.Instance.EnableVerboseDebugLog) {
                    D.Warn("{0} BackingStore {1} and value {2} are equal. Property not changed.", propertyName, backingStore, value);
                }
                else {
                    D.Warn("{0} BackingStore and value of Type {1} are equal. Property not changed.", propertyName, typeof(T).Name);
                }
            }
        }

        protected void OnPropertyChanging<T>(string propertyName, T newValue) {
            var handler = PropertyChanging; // threadsafe approach
            if (handler != null) {
                handler(this, new PropertyChangingValueEventArgs<T>(propertyName, newValue));   // My custom modification to provide the newValue
            }
        }

        protected void OnPropertyChanged(string propertyName) {
            var handler = PropertyChanged; // threadsafe approach
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void VerifyCallerIsProperty(string propertyName) {
            var stackTrace = new System.Diagnostics.StackTrace();
            var frame = stackTrace.GetFrames()[2];
            var caller = frame.GetMethod();
            if (!caller.Name.Equals("set_" + propertyName, StringComparison.InvariantCulture)) {
                throw new InvalidOperationException("Called SetProperty {0} from {1}.".Inject(propertyName, caller.Name));
            }
        }

        #region IChangeTracking Members

        public void AcceptChanges() {
            _isChanged = false;
        }

        private bool _isChanged;
        public bool IsChanged {
            get { return _isChanged; }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region INotifyPropertyChanging Members

        public event PropertyChangingEventHandler PropertyChanging;

        #endregion
    }
}

