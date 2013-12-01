﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GenericExtensions.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
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

    /// <summary>
    /// COMMENT 
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
        /// <exception cref="System.ArgumentNullException">source</exception>
        public static bool EqualsAnyOf<T>(this T source, params T[] itemsToCompare) {
            //  'this' source can never be null without the CLR throwing a Null reference exception
            Arguments.ValidateNotNullOrEmpty(itemsToCompare);
            return itemsToCompare.Contains<T>(source);
        }

        /// <summary>
        /// Evaluates the equality of two sequences with an option to ignore order of the members.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="second">The second.</param>
        /// <param name="ignoreOrder">if set to <c>true</c> [ignore order].</param>
        /// <returns></returns>
        public static bool SequenceEquals<T>(this IEnumerable<T> source, IEnumerable<T> second, bool ignoreOrder = false) {
            if (ignoreOrder) {
                return source.OrderBy(s => s).SequenceEqual<T>(second.OrderBy(s => s));
            }
            return source.SequenceEqual<T>(second);
        }

        /// <summary>
        /// Used from an instance of Random to get a 50/50 chance.
        /// </summary>
        /// <param name="sourceRNG">A Random Number Generator instance.</param>
        /// <returns></returns>
        public static bool CoinToss(this Random sourceRNG) {
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
        public static T OneOf<T>(this Random sourceRNG, params T[] itemsToPickFrom) {
            Arguments.ValidateNotNullOrEmpty(itemsToPickFrom);
            return itemsToPickFrom[sourceRNG.Next(itemsToPickFrom.Length)];
        }

        /// <summary>
        /// Provides for the application of a work action to all the elements in an IEnumerable sourceSequence.
        /// Syntax: <code>sequenceOfTypeT.ForAll((T n) => Console.WriteLine(n.ToString()));</code> read as
        /// "For each element in the T sourceSequence, write the string version to the console."
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceSequence">The Sequence of Type T calling the extension.</param>
        /// <param name="actionToExecute">The work to perform on the sequence, usually expressed in lambda form.</param>
        public static void ForAll<T>(this IEnumerable<T> sourceSequence, Action<T> actionToExecute) {
            foreach (T item in sourceSequence.ToList<T>()) {   // ToList avoids exceptions when the sequence is modified by the action
                actionToExecute(item);
            }
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
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence">The IEnumerable sequence of Type T.</param>
        /// <param name="itemsToRemove">The items to remove.</param>
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
        /// <param name="enumerable">The enumerable, which may be null or empty.</param>
        /// <returns>
        ///     <c>true</c> if the IEnumerable is null or empty; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable) {
            if (enumerable == null) {
                return true;
            }
            // If this is a list, use the Count property for efficiency. The Count property is O(1) while IEnumerable.Count() is O(N).
            var collection = enumerable as ICollection<T>;
            if (collection != null) {
                return collection.Count < 1;
            }
            return !enumerable.Any();
        }

        /// <summary>
        /// Constructs a string separated by the provided delimiter from the elements of the IEnumerable source.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="delimiter">The delimiter string. Default is ",".</param>
        /// <returns></returns>
        public static string Concatenate<T>(this IEnumerable<T> source, string delimiter = Constants.Comma) {
            var sb = new StringBuilder();
            bool first = true;
            foreach (T t in source) {
                if (first) {
                    first = false;
                }
                else {
                    sb.Append(delimiter);
                }
                sb.Append(t);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Shuffles the specified source.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source) {
            return RandomExtended<T>.Shuffle(source.ToArray());
        }

        /// <summary>
        /// Returns true if targetValue is within a reasonable tolerance of the value of source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static bool ApproxEquals(this float source, float value) {
            return Mathfx.Approx(source, value, .001F);
        }

        /// <summary>
        /// Populates the source array with the provided value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array">The array.</param>
        /// <param name="value">The value.</param>
        public static void Populate<T>(this T[] array, T value) {
            for (int i = 0; i < array.Length; i++) {
                array[i] = value;
            }
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
        /// </summary>
        /// <typeparam name="TSource">The type of the publisher.</typeparam>
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
                D.Error("TSource Type [{0}] not equal to or derived from  Property DeclaringType {1}.", typeof(TSource).Name, propertyInfo.DeclaringType.Name);
            }
            return propertyInfo.Name;
        }
        #endregion

    }
}

