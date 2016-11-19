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
// publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished 
// to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies 
// or substantial portions of the Software.
// </remarks>
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    /// <summary>
    /// Abstract base class for classes that wish to communicate changes to their properties. Capabilities include 1) knowing when one or more of their properties have changed ,and 2)
    /// notifying subscribers of said change both before and after it occurs. To subscribe: publisher.SubscribeToPropertyChang{ed, ing]&lt;TSource, TProp&gt;(pub => pub.Foo, OnFooChang[ed, ing]);
    /// Implement OnFooChang[ed, ing]() in the subscriber. There is no need to accommodate a XXXArgs parameter containing the property name as
    /// the OnFooChang[ed, ing] method is property name specific.
    /// </summary>
    public abstract class APropertyChangeTracking : IChangeTracking, INotifyPropertyChanged, INotifyPropertyChanging {

        /// <summary>
        /// Sets the properties backing field to the new value if it has changed and raises PropertyChanged and PropertyChanging
        /// events to any subscribers. Also provides local method access for doing any additional processing work that should be
        /// done outside the setter. This is useful when you have dependent properties in the same object that should change as a 
        /// result of the initial property change.
        /// </summary>
        /// <typeparam name="T">Property Type</typeparam>
        /// <param name="backingStore">The backing store field.</param>
        /// <param name="value">The proposed new value.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="onChanged">Optional local method to call when the property is changed.</param>
        /// <param name="onChanging">Optional local method to call before the property is changed. The proposed new value is provided as the parameter.</param>
        protected void SetProperty<T>(ref T backingStore, T value, string propertyName, Action onChanged = null, Action<T> onChanging = null) {
            __VerifyCallerIsProperty(propertyName);
            if (EqualityComparer<T>.Default.Equals(backingStore, value)) {
                if (!CheckForDestroyedMonobehaviourInterface(backingStore)) {
                    __TryWarn<T>(backingStore, value, propertyName);
                    return;
                }
            }
            //D.Log("SetProperty called. {0} changing to {1}.", propertyName, value);

            if (onChanging != null) { onChanging(value); }
            OnPropertyChanging<T>(propertyName, value);

            backingStore = value;

            if (onChanged != null) { onChanged(); }
            _isChanged = true;
            OnPropertyChanged(propertyName);
        }

        /// <summary>
        /// Returns <c>true</c> if the backingStore of Type T is an interface for a MonoBehaviour that
        /// has been (or is about to be) destroyed, <c>false</c> otherwise.
        /// <remarks>Interfaces for MonoBehaviours have an unusual behaviour when it comes to equality comparisons to null.
        /// If the MonoBehaviour underlying the interface of Type T is slated for destruction, T's DefaultEqualityComparer will return
        /// <c>true</c> when compared to null due to UnityEngine.Object's override of Equals(). However, T <c>backingStore</c> == null
        /// will return <c>false</c> under the same circumstances. As a result, when <c>backingStore</c> is an Interface for a MonoBehaviour
        /// slated for destruction and <c>value</c> is null, the default equality check in SetProperty() sees them as equal and
        /// therefore DOES NOT set <c>backingStore</c> to null. This unexpected behaviour can lead to errors that are hard
        /// to diagnose. This method checks for that condition. If found, SetProperty allows the null <c>value</c> to be assigned
        /// to <c>backingStore</c> resulting in the expected outcome, aka <c>backingStore</c> == null returning <c>true</c>.</remarks>
        /// <see cref="http://answers.unity3d.com/questions/586144/destroyed-monobehaviour-not-comparing-to-null.html"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="backingStore">The backing store.</param>
        /// <returns></returns>
        private bool CheckForDestroyedMonobehaviourInterface<T>(T backingStore) {
            Type tType = typeof(T);
            if (tType.IsInterface) {
                //D.Log("{0} found Interface of Type {1}.", GetType().Name, tType.Name);
                if (backingStore != null && backingStore.Equals(null)) {
                    // backingStore is a destroyed MonoBehaviour Interface and value is null since backingStore.Equals(value) to get here
                    //D.Log("{0} found MonoBehaviour Interface of Type {1} slated for destruction.", GetType().Name, tType.Name);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Warns of equality if T is a Reference type that is not a string.
        /// <remarks>11.9.16 Discontinued use of DebugSettings.EnableVerboseDebugLog 
        /// as ToString's ObjectAnalyzer isn't really useful.</remarks>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="backingStore">The backing store.</param>
        /// <param name="value">The value.</param>
        /// <param name="propertyName">Name of the property.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void __TryWarn<T>(T backingStore, T value, string propertyName) {
            Type tType = typeof(T);
            if (!tType.IsValueType) {
                if (value != null) {
                    if (tType == typeof(string)) {
                        D.Warn("{0} BackingStore {1} and value {2} are equal. Property not changed.", propertyName, backingStore, value);
                    }
                    else {
                        D.Warn("{0} BackingStore and value of Type {1} are equal. Property not changed.", propertyName, tType.Name);
                    }
                }
            }
        }

        protected void OnPropertyChanging<T>(string propertyName, T newValue) {
            var handler = PropertyChanging; // thread safe approach
            if (handler != null) {
                handler(this, new PropertyChangingValueEventArgs<T>(propertyName, newValue));   // My custom modification to provide the newValue
            }
        }

        protected void OnPropertyChanged(string propertyName) {
            var handler = PropertyChanged; // thread safe approach
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Verifies the caller is a property.
        /// <remarks>Very expensive both in time and allocations.
        /// 11.9.16 Reduced allocation from 800 to 100 bytes per use by having only one local variable and not using Inject().
        /// UNCLEAR why this local variable would be on heap rather than stack.</remarks>
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <exception cref="System.InvalidOperationException">Called SetProperty {0} from {1}. Check spelling of Property.".Inject(propertyName, caller.Name)</exception>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void __VerifyCallerIsProperty(string propertyName) {
            string callerName = new System.Diagnostics.StackFrame(2).GetMethod().Name;
            if (callerName.Equals("set_" + propertyName, StringComparison.InvariantCulture)) {
                return;
            }
            throw new InvalidOperationException("Called SetProperty {0} from {1}. Check spelling of Property.".Inject(propertyName, callerName));
        }

        /// <summary>
        /// My addition that sets _isChanged to true.
        /// </summary>
        public void MarkAsChanged() {
            _isChanged = true;
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

