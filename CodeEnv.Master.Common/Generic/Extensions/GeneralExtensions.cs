﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GenericExtensions.cs
// General purpose Extensions. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using UnityEngine;
    /// <summary>
    /// General purpose Extensions. 
    /// </summary>
    public static class GeneralExtensions {

        /// <summary>
        /// Allows syntax like: <c>if(reallyLongStringVariableName.EqualsAnyOf(string1, string2, string3)</c>, or
        /// <c>if(reallyLongIntVariableName.EqualsAnyOf(1, 2, 4, 8)</c>, or
        /// <c>if(reallyLongMethodParameterName.EqualsAnyOf(SomeEnum.value1, SomeEnum.value2, SomeEnum.value3)</c>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source calling the Extension method.</param>
        /// <param name="itemsToCompare">The array of items to compare to the source.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Source argument cannot be IEnumerable.</exception>
        /// <exception cref="System.ArgumentNullException">source</exception>
        public static bool EqualsAnyOf<T>(this T source, params T[] itemsToCompare) {
            //  'this' source can never be null without the CLR throwing a Null reference exception
            if (source is IEnumerable<T>) {
                throw new ArgumentException("Source argument cannot be IEnumerable.");
            }
            Arguments.ValidateNotNullOrEmpty(itemsToCompare);
            return itemsToCompare.Contains<T>(source);
        }

        /// <summary>
        /// Evaluates the equality of two sequences with an option to ignore order of the members.
        /// Sequences must implement IComparable<typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence">The source sequence.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="ignoreOrder">if set to <c>true</c> [ignore order].</param>
        /// <returns></returns>
        public static bool SequenceEquals<T>(this IEnumerable<T> sequence, IEnumerable<T> second, bool ignoreOrder = false) where T : IComparable<T> {
            if (ignoreOrder) {
                return sequence.OrderBy(s => s).SequenceEqual<T>(second.OrderBy(s => s));
            }
            return sequence.SequenceEqual<T>(second);
        }

        /// <summary>
        /// Used from an instance of Random to get a 50/50 chance.
        /// </summary>
        /// <param name="sourceRNG">A Random Number Generator instance.</param>
        /// <returns></returns>
        public static bool CoinToss(this System.Random sourceRNG) {
            //  'this' sourceRNG can never be null without the CLR throwing a Null reference exception
            return sourceRNG.Next(2) == 0;
        }

        /// <summary>
        /// Randomly picks an arg from itemsToPickFrom. Used from an instance of Random.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceRNG">The Random Number Generator instance.</param>
        /// <param name="itemsToPickFrom">The array of items of Type T to pick from.</param>
        /// <returns></returns>
        public static T OneOf<T>(this System.Random sourceRNG, params T[] itemsToPickFrom) {
            Arguments.ValidateNotNullOrEmpty(itemsToPickFrom);
            return itemsToPickFrom[sourceRNG.Next(itemsToPickFrom.Length)];
        }

        /// <summary>
        /// Provides for the application of a work action to all the elements in an IEnumerable source sequence.
        /// Throws an exception of source sequence is null. If it is empty, the method simply returns.
        /// Syntax: <code>sequenceOfTypeT.ForAll((T n) => Console.WriteLine(n.ToString()));</code> read as
        /// "For each element in the T sourceSequence, write the string version to the console."
        /// OPTIMIZE use MoreLinq.ForEach()? What about when the action modifies the underlying IEnumerable?
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence">The Sequence of Type T calling the extension.</param>
        /// <param name="actionToExecute">The work to perform on the sequence, usually expressed in lambda form.</param>
        public static void ForAll<T>(this IEnumerable<T> sequence, Action<T> actionToExecute) {
            Arguments.ValidateNotNull(sequence);
            Arguments.ValidateNotNull(actionToExecute);
            sequence.ToList<T>().ForEach(actionToExecute);
            // Warning: Per Microsoft, modifying the underlying collection in the body of the action is not supported and causes undefined behaviour.
            // Starting in .Net 4.5, an InvalidOperationException will be thrown if this occurs. Prior to this no exception is thrown.
        }

        /// <summary>
        /// Checks the range of the number against an allowed variation percentage around the number.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="target">The target value.</param>
        /// <param name="allowedPercentageVariation">Optional allowed percentage variation as a whole number. 1.0 = one percent variation, the default.</param>
        /// <returns></returns>
        public static bool CheckRange(this float number, float target, float allowedPercentageVariation = 1.0F) {
            float allowedLow = (100F - allowedPercentageVariation) / 100 * target;
            float allowedHigh = (100F + allowedPercentageVariation) / 100 * target;
            return Utility.IsInRange(number, allowedLow, allowedHigh);
        }

        /// <summary>
        /// Returns value formatted for display. If value is greater than threshold then no decimal point is included.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="showZero">if value is Zero and set to <c>true</c> then "0" is returned. If <c>false</c> then string.Empty is returned.</param>
        /// <param name="threshold">The value above which no decimal point is included.</param>
        /// <returns></returns>
        public static string FormatValue(this float value, bool showZero = true, float threshold = 10F) {
            Arguments.ValidateNotNegative(value);
            Arguments.ValidateForRange(threshold, float.Epsilon, float.PositiveInfinity);

            string formattedValue = string.Empty;
            if (value == Constants.ZeroF) {
                if (showZero) {
                    formattedValue = Constants.FormatFloat_0Dp.Inject(value);
                }
            }
            else {
                formattedValue = value < threshold ? Constants.FormatFloat_2DpMax.Inject(value) : Constants.FormatFloat_0Dp.Inject(value);
            }
            return formattedValue;
        }

        ///<summary>Finds the index of the first item matching an expression in an enumerable.</summary>
        ///<param name="items">The enumerable to search.</param>
        ///<param name="predicate">The expression to test the items against.</param>
        ///<returns>The index of the first matching item, or -1 if no items match.</returns>
        public static int FindIndex<T>(this IEnumerable<T> items, Func<T, bool> predicate) {
            if (items == null)
                throw new ArgumentNullException("items");
            if (predicate == null)
                throw new ArgumentNullException("predicate");

            int retVal = 0;
            foreach (var item in items) {
                if (predicate(item))
                    return retVal;
                retVal++;
            }
            return -1;
        }

        ///<summary>Finds the index of the first occurence of an item in an enumerable.</summary>
        ///<param name="items">The enumerable to search.</param>
        ///<param name="item">The item to find.</param>
        ///<returns>The index of the first matching item, or -1 if the item was not found.</returns>
        public static int IndexOf<T>(this IEnumerable<T> items, T item) {
            return items.FindIndex(i => EqualityComparer<T>.Default.Equals(item, i));
        }

        /// <summary>
        /// Removes items from the source sequence.
        /// Note: Useful as the Linq Except() extension requires an IEnumerable. This version also handles single values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence">The IEnumerable sequence of Type T.</param>
        /// <param name="itemsToRemove">The one or more items to remove.</param>
        /// <returns>
        /// An IEnumerable sequence of Type T with items removed.
        /// </returns>
        public static IEnumerable<T> Except<T>(this IEnumerable<T> sequence, params T[] itemsToRemove) {
            return sequence.Except<T>(itemsToRemove as IEnumerable<T>);
        }

        /// <summary>
        /// Determines whether the collection is null or contains no elements.
        /// </summary>
        /// <typeparam name="T">The IEnumerable type.</typeparam>
        /// <param name="sequence">The enumerable, which may be null or empty.</param>
        /// <returns>
        ///     <c>true</c> if the IEnumerable is null or empty; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> sequence) {
            if (sequence == null) {
                return true;
            }
            // If this is a list, use the Count property for efficiency. The Count property is O(1) while IEnumerable.Count() is O(N).
            var collection = sequence as ICollection<T>;
            if (collection != null) {
                return collection.Count == Constants.Zero;  // < 1;
            }
            return !sequence.Any();
        }

        /// <summary>
        /// Constructs a string separated by the provided delimiter from the elements of the IEnumerable source.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence">The source.</param>
        /// <param name="delimiter">The delimiter string. Default is ",".</param>
        /// <returns></returns>
        public static string Concatenate<T>(this IEnumerable<T> sequence, string delimiter = Constants.Comma) {
            var sb = new StringBuilder();
            bool first = true;
            foreach (T t in sequence) {
                if (first) {
                    first = false;
                }
                else {
                    sb.Append(delimiter);
                    sb.Append(Constants.NewLine);
                }
                sb.Append(t);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Shuffles the specified source.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence">The source.</param>
        /// <returns></returns>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> sequence) {
            return RandomExtended.Shuffle<T>(sequence.ToArray());
        }

        /// <summary>
        /// Returns true if targetValue is within a reasonable tolerance of the value of source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static bool ApproxEquals(this float source, float value) {
            return Mathfx.Approx(source, value, UnityConstants.FloatEqualityPrecision);
        }

        /// <summary>
        ///  Tests if <c>value</c> is &gt;= <c>targetValue</c> within the provided acceptableRange. 
        ///  Useful in dealing with floating point imprecision.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <param name="targetValue">The targetValue.</param>
        /// <param name="acceptableRange">The acceptableRange to either side of the targetValue.
        /// Default is UnityConstants.FloatEqualityPrecision.</param>
        /// <returns></returns>
        public static bool IsGreaterThanOrEqualTo(this float value, float targetValue, float acceptableRange = UnityConstants.FloatEqualityPrecision) {
            if (value > targetValue) {
                return true;
            }
            return ((Mathf.Abs(value - targetValue) < acceptableRange));
        }

        /// <summary>
        ///  Tests if <c>value</c> is &lt;= <c>targetValue</c> within the provided acceptableRange. 
        ///  Useful in dealing with floating point imprecision.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <param name="targetValue">The targetValue.</param>
        /// <param name="acceptableRange">The acceptableRange to either side of the targetValue.
        /// Default is UnityConstants.FloatEqualityPrecision.</param>
        /// <returns></returns>
        public static bool IsLessThanOrEqualTo(this float value, float targetValue, float acceptableRange = UnityConstants.FloatEqualityPrecision) {
            if (value < targetValue) {
                return true;
            }
            return ((Mathf.Abs(value - targetValue) < acceptableRange));
        }

        /// <summary>
        /// Populates the collection with the provided T value. The number of slots
        /// to populate is determined by quantity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="value">The value to populate with.</param>
        /// <param name="quantity">The quantity of slots in the collection that will be populated.</param>
        public static void Populate<T>(this ICollection<T> list, T value, int quantity) {
            list.Clear();
            for (int i = 0; i < quantity; i++) {
                list.Add(value);
            }
        }

        /// <summary>
        /// Populates the collection with default(T) value. The number of slots
        /// to populate is determined by quantity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="quantity">The quantity.</param>
        public static void Populate<T>(this ICollection<T> list, int quantity) {
            Populate<T>(list, default(T), quantity);
        }

        /// <summary>
        /// Populates the capacity of the List with the provided T value. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="value">The value.</param>
        public static void Populate<T>(this List<T> list, T value) {
            Populate<T>(list, value, list.Capacity);
        }

        /// <summary>
        /// Populates the capacity of the List with default(T).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        public static void Populate<T>(this List<T> list) {
            Populate<T>(list, list.Capacity);
        }

        /// <summary>
        /// Returns the next item in the list that follows <c>item</c>. If <c>item</c> is not 
        /// present or the last item in the list, the first item in the list is returned. Warns if the
        /// result is the default value of T, aka null, 0, 0.0, etc.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public static T Next<T>(this IList<T> list, T item) {
            Arguments.ValidateNotNullOrEmpty<T>(list);
            T result = default(T);
            var nextIndex = list.IndexOf(item) + 1;
            if (nextIndex == list.Count) {
                result = list[0];
            }
            else {
                result = list[nextIndex];
            }
            if (result.Equals(default(T))) {
                string msg = result == null ? "null" : result.ToString();
                D.Warn("Next result {0} is default of {1}.", msg, typeof(T).Name);
            }
            return result;
        }

        #region Generic INotifyPropertyChanged, INotifyPropertyChanging Extensions

        /// <summary>
        /// Generic extension method that subscribes to SPECIFIC PropertyChanged event notifications from objects that implement 
        /// INotifyPropertyChanged and use APropertyChangeTracking's SetProperty method in their property setters.
        /// </summary>
        /// <typeparam name="TSource">The type of the publisher.</typeparam>
        /// <typeparam name="TProp">The type of the publisher's property.</typeparam>
        /// <param name="source">The publisher.</param>
        /// <param name="propertySelector">The lambda property selector: pub => pub.Property</param>
        /// <param name="onChanged">The subsciber's no parameter/no return method to call when the property changed.</param>
        public static IDisposable SubscribeToPropertyChanged<TSource, TProp>(this TSource source, Expression<Func<TSource, TProp>> propertySelector, Action onChanged) where TSource : INotifyPropertyChanged {
            Arguments.ValidateNotNull(source);
            Arguments.ValidateNotNull(propertySelector);
            Arguments.ValidateNotNull(onChanged);

            var subscribedPropertyName = GetPropertyName<TSource, TProp>(propertySelector);
            PropertyChangedEventHandler handler = (s, e) => {
                if (string.Equals(e.PropertyName, subscribedPropertyName, StringComparison.InvariantCulture)) {
                    onChanged();
                }
            };
            source.PropertyChanged += handler;
            //D.Log("{0}.{1} successfully subscribed by someone to receive changes.", typeof(TSource), subscribedPropertyName);
            //return System.Reactive.Disposables.Disposable.Create(() => source.PropertyChanged -= handler);    // FIXME Mono 2.0 does not support ReactiveExtensions.dll
            return new DisposePropertyChangedSubscription<TSource>(source, handler);
        }

        /// <summary>
        /// Generic extension method that subscribes to SPECIFIC PropertyChanging event notifications from objects that implement
        /// INotifyPropertyChanging and use APropertyChangeTracking's SetProperty method in their property setters.
        /// 
        /// WARNING: Use the specific type (or interface) of the publisher and property, not a derived type. Use of a derived Property
        /// Type can result in a casting exception when onChanging is called.
        /// </summary>
        /// <typeparam name="TSource">The type of the publisher where this method is called.</typeparam>
        /// <typeparam name="TProp">The type of the publisher's property.</typeparam>
        /// <param name="source">The publisher.</param>
        /// <param name="propertySelector">The lambda property selector: pub =&gt; pub.Property</param>
        /// <param name="onChanging">The subsciber's one parameter/no return method to call when the property is in the process of changing.</param>
        /// <returns>IDisposable for Unsubscribing</returns>
        public static IDisposable SubscribeToPropertyChanging<TSource, TProp>(this TSource source, Expression<Func<TSource, TProp>> propertySelector, Action<TProp> onChanging) where TSource : INotifyPropertyChanging {
            Arguments.ValidateNotNull(source);
            Arguments.ValidateNotNull(propertySelector);
            Arguments.ValidateNotNull(onChanging);

            var subscribedPropertyName = GetPropertyName<TSource, TProp>(propertySelector);
            PropertyChangingEventHandler handler = (s, e) => {
                if (string.Equals(e.PropertyName, subscribedPropertyName, StringComparison.InvariantCulture)) {
                    //D.Warn("TSource = {0}, TProp = {1}.", typeof(TSource).Name, typeof(TProp).Name);
                    //D.Warn("Type of e = {0}, genericType = {1}.", e.GetType().Name, e.GetType().GetGenericArguments().First().Name);
                    onChanging(((PropertyChangingValueEventArgs<TProp>)e).NewValue);    // My custom modification to provide the newValue
                }
            };
            source.PropertyChanging += handler;
            //return System.Reactive.Disposables.Disposable.Create(() => source.PropertyChanging -= handler);   // FIXME Mono 2.0 does not support ReactiveExtensions.dll
            return new DisposePropertyChangingSubscription<TSource>(source, handler);
        }


        /// <summary>
        /// Helper that checks the propertySelector for errors and returns the name of the property it refers to.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TProp">The type of the prop.</typeparam>
        /// <param name="propertySelector">The property selector.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">
        /// Must be a member accessor;propertySelector
        /// or
        /// Must yield a single property on the given object;propertySelector
        /// </exception>
        private static string GetPropertyName<TSource, TProp>(Expression<Func<TSource, TProp>> propertySelector) {
            var memberExpr = propertySelector.Body as MemberExpression;
            if (memberExpr == null) {
                throw new ArgumentException("Must be a member accessor", "propertySelector");
            }
            var propertyInfo = memberExpr.Member as PropertyInfo;
            if (propertyInfo == null) {
                throw new ArgumentException("No property named {0} of type {1} present on Type {2}.".Inject(memberExpr.Member.Name, typeof(TProp), typeof(TSource)));
            }
            if (propertyInfo.DeclaringType != typeof(TSource) && !typeof(TSource).IsSubclassOf(propertyInfo.DeclaringType)) {
                // my modification that allows a base class to hold the Property rather than just the derived class
                throw new ArgumentException("TSource Type [{0}] not derived from Property DeclaringType {1}.".Inject(typeof(TSource).Name, propertyInfo.DeclaringType.Name));
            }
            return propertyInfo.Name;
        }

        #endregion

    }
}

