// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GeneralExtensions.cs
// General purpose Extensions. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using LocalResources;
    using UnityEngine;

    /// <summary>
    /// General purpose Extensions. 
    /// </summary>
    public static class GeneralExtensions {

        #region Enum Extensions

        /// <summary>Gets the string name of the enum constant. Much faster than sourceEnumConstant.ToString().</summary>
        /// <param name="sourceEnumConstant">The enum constant.</param>
        /// <returns>The string name of this sourceEnumConstant. </returns>
        /// <remarks>Not localizable. For localizable descriptions, use GetDescription().</remarks>
        public static string GetValueName(this Enum sourceEnumConstant) {
            Type enumType = sourceEnumConstant.GetType();
            return Enum.GetName(enumType, sourceEnumConstant);
        }

        /// <summary>
        /// Gets the alternative text from the EnumAttribute if present. If not, the name is returned.
        /// Commonly used to get a short abbreviation for the enum name, e.g. "O" for Organics, "CV" for Carrier.
        /// </summary>
        /// <param name="sourceEnumConstant">The source enum constant.</param>
        /// <returns>
        /// Alternative text for the enum value if the <see cref="EnumAttribute" /> is present.
        /// </returns>
        public static string GetEnumAttributeText(this Enum sourceEnumConstant) {
            EnumAttribute attribute = GetAttribute(sourceEnumConstant);
            if (attribute == null) {
                return GetValueName(sourceEnumConstant);
            }
            return attribute.AttributeText;
        }

        /// <summary>
        /// Converts the <see cref="Enum" /> sourceEnumType to an <see cref="IList" /> 
        /// compatible object of Descriptions.
        /// </summary>
        /// <param friendlyDescription="sourceEnumType">The <see cref="Enum"/> Type of the enum.</param>
        /// <returns>An <see cref="IList"/> containing the enumerated
        /// values (key) and descriptions of the provided Type.</returns>
        public static IList GetDescriptions(this Type sourceEnumType) {
            IList list = new ArrayList();
            Array enumValues = Enum.GetValues(sourceEnumType);

            foreach (Enum value in enumValues) {
                list.Add(new KeyValuePair<Enum, string>(value, GetEnumAttributeText(value)));
            }
            return list;
        }

        private static EnumAttribute GetAttribute(Enum enumConstant) {
            EnumAttribute attribute = Attribute.GetCustomAttribute(ForValue(enumConstant), typeof(EnumAttribute)) as EnumAttribute;
            if (attribute == null) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                D.Warn(ErrorMessages.EnumNoAttribute.Inject(enumConstant.GetValueName(), typeof(EnumAttribute).Name, callingMethodName));
            }
            return attribute;
        }

        private static MemberInfo ForValue(Enum enumConstant) {
            Type enumType = enumConstant.GetType();
            return enumType.GetField(Enum.GetName(enumType, enumConstant));
        }

        #endregion

        #region String Extensions

        /// <summary>
        /// Inserts the ToString() version of the arguments provided into the calling string using string.Format().
        /// Usage syntax: "The {0} jumped over the {1}.".Inject("Cat", "Moon");
        /// <remarks>string.Format uses StringBuilder internally so is slower and allocates more memory.
        /// Best use is where format actually contains formatting, otherwise concatenate.</remarks>
        /// </summary>
        /// <param name="format">The calling formatted string.</param>
        /// <param name="itemsToInject">The items to inject into the calling string. If the item is null, it is replaced by string.Empty</param>
        /// <returns></returns>
        /// <exception cref="System.FormatException"></exception>
        public static string Inject(this string format, params object[] itemsToInject) {
            //  'this' format can never be null without the CLR throwing a Null reference exception
            Utility.ValidateForContent(format);
            // IMPROVE see Effective C#, Item 45 Minimize Boxing and Unboxing
            return string.Format(CultureInfo.CurrentCulture, format, itemsToInject);
        }

        /// <summary>
        /// Adds the specified delimiter to each string item in the sequence except the last one.
        /// </summary>
        /// <param name="sequence">The string sequence.</param>
        /// <param name="delimiter">The delimiter. Default is ",".</param>
        /// <returns></returns>
        public static IEnumerable<string> AddDelimiter(this IEnumerable<string> sequence, string delimiter = Constants.Comma) {
            Utility.ValidateNotNull(sequence);
            IList<string> delimitedList = new List<string>();
            foreach (string item in sequence) {
                delimitedList.Add(item + Constants.NewLine);
            }
            int lastItemIndex = delimitedList.Count - 1;
            string lastItem = delimitedList[lastItemIndex];
            string lastItemWithoutDelineationEnding = lastItem.Replace(Constants.NewLine, string.Empty);
            delimitedList[lastItemIndex] = lastItemWithoutDelineationEnding;
            return delimitedList;
        }

        /// <summary>
        ///     Clears the contents of the string builder.
        /// </summary>
        /// <param name="sb">
        ///     The <see cref="StringBuilder"/> to clear.
        /// </param>
        public static void Clear(this StringBuilder sb) {
            sb.Length = 0;
            sb.Capacity = 16;
        }

        /// <summary>
        /// Removes the specified string (if present) from the source string and returns the result. 
        /// Only the first instance of <c>stringToRemove</c> is removed. Case sensitive.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="stringToRemove">The string to remove.</param>
        /// <returns></returns>
        public static string Remove(this string source, string stringToRemove) {
            int index = source.IndexOf(stringToRemove);
            string result = index < 0 ? source : source.Remove(index, stringToRemove.Length);
            if (source.Equals(result)) {
                D.Warn("Attempted to remove string {0} from {1} but did not find it.", stringToRemove, source);
            }
            else {
                D.Log("Removed string {0} from {1} resulting in {2}.", stringToRemove, source, result);
            }
            return result;
        }

        #endregion

        #region Enumerables

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
        /// Creates a union (no duplicates) of sequence with all the provided otherSequences.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence">The sequence.</param>
        /// <param name="otherSequences">The other sequences.</param>
        /// <returns></returns>
        public static IEnumerable<T> UnionBy<T>(this IEnumerable<T> sequence, params IEnumerable<T>[] otherSequences) {
            IEnumerable<T> result = sequence;
            otherSequences.ForAll(seq => result = result.Union(seq));
            return result;
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
            Utility.ValidateNotNull(sequence);
            Utility.ValidateNotNull(actionToExecute);
            sequence.ToList<T>().ForEach(actionToExecute);
            // Warning: Per Microsoft, modifying the underlying collection in the body of the action is not supported and causes undefined behaviour.
            // Starting in .Net 4.5, an InvalidOperationException will be thrown if this occurs. Prior to this no exception is thrown.
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

        ///<summary>Finds the index of the first occurrence of an item in an enumerable.</summary>
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
        /// Returns <c>true</c> if the sequence contains any duplicates, <c>false</c> otherwise.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence">The sequence.</param>
        /// <param name="duplicate">The first duplicate found, if any.</param>
        /// <returns></returns>
        public static bool ContainsDuplicates<T>(this IEnumerable<T> sequence, out T duplicate) {
            var set = new HashSet<T>();
            foreach (var t in sequence) {
                if (!set.Add(t)) {
                    duplicate = t;
                    return true;
                }
            }
            duplicate = default(T);
            return false;
        }

        #endregion

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
            Utility.ValidateNotNullOrEmpty(itemsToCompare);
            return itemsToCompare.Contains<T>(source);
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
        /// Randomly picks an argument from itemsToPickFrom. Used from an instance of Random.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceRNG">The Random Number Generator instance.</param>
        /// <param name="itemsToPickFrom">The array of items of Type T to pick from.</param>
        /// <returns></returns>
        public static T OneOf<T>(this System.Random sourceRNG, params T[] itemsToPickFrom) {
            Utility.ValidateNotNullOrEmpty(itemsToPickFrom);
            return itemsToPickFrom[sourceRNG.Next(itemsToPickFrom.Length)];
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
            Utility.ValidateNotNegative(value);
            Utility.ValidateForRange(threshold, float.Epsilon, float.PositiveInfinity);

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


        /// <summary>
        /// Returns true if targetValue is within <c>UnityConstants.FloatEqualityPrecision</c> of the value of source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static bool ApproxEquals(this float source, float value) {
            return Mathfx.Approx(source, value, UnityConstants.FloatEqualityPrecision);
        }

        /// <summary>
        ///  Tests if <c>value</c> is &gt;= <c>targetValue</c> with buffer added. 
        ///  Useful in dealing with floating point imprecision.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <param name="targetValue">The targetValue.</param>
        /// <param name="equalsTolerance">The positive equals tolerance that is subtracted from targetValue.
        /// Default is UnityConstants.FloatEqualityPrecision.</param>
        /// <returns></returns>
        public static bool IsGreaterThanOrEqualTo(this float value, float targetValue, float equalsTolerance = UnityConstants.FloatEqualityPrecision) {
            Utility.ValidateNotNegative(equalsTolerance);
            return value >= targetValue - equalsTolerance;
        }

        /// <summary>
        ///  Tests if <c>value</c> is &lt;= <c>targetValue</c> with buffer added. 
        ///  Useful in dealing with floating point imprecision.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <param name="targetValue">The targetValue.</param>
        /// <param name="equalsTolerance">The positive equals tolerance that is added to targetValue.
        /// Default is UnityConstants.FloatEqualityPrecision.</param>
        /// <returns></returns>
        public static bool IsLessThanOrEqualTo(this float value, float targetValue, float equalsTolerance = UnityConstants.FloatEqualityPrecision) {
            Utility.ValidateNotNegative(equalsTolerance);
            return value <= targetValue + equalsTolerance;
        }

        /// <summary>
        /// Fills the collection with the provided T value. The number of slots
        /// to populate is determined by quantity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="value">The value to populate with.</param>
        /// <param name="quantity">The quantity of slots in the collection that will be populated.</param>
        public static void Fill<T>(this ICollection<T> list, T value, int quantity) {
            list.Clear();
            for (int i = 0; i < quantity; i++) {
                list.Add(value);
            }
        }

        /// <summary>
        /// Fills the collection with default(T) value. The number of slots
        /// to populate is determined by quantity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="quantity">The quantity.</param>
        public static void Fill<T>(this ICollection<T> list, int quantity) {
            Fill<T>(list, default(T), quantity);
        }

        /// <summary>
        /// Fills the capacity of the List with the provided T value. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="value">The value.</param>
        public static void Fill<T>(this List<T> list, T value) {
            Fill<T>(list, value, list.Capacity);
        }

        /// <summary>
        /// Fills the capacity of the List with default(T).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        public static void Fill<T>(this List<T> list) {
            Fill<T>(list, list.Capacity);
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
            Utility.ValidateNotNullOrEmpty<T>(list);
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

        /// <summary>
        /// Aggregates the nullable values provided and returns their addition-based sum. If one or more of these
        /// nullable values has no value (its null), it is excluded from the sum. If all values are null, the value returned is null.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public static float? NullableSum(this float? source, params float?[] args) {
            var argList = new List<float?>(args);
            argList.Add(source);
            return argList.NullableSum();
        }

        /// <summary>
        /// Aggregates the nullable values provided and returns their addition-based sum. If one or more of these
        /// nullable values has no value (its null), it is excluded from the sum. If all values are null, the value returned is null.
        /// </summary>
        /// <param name="sequence">The nullable values.</param>
        /// <returns></returns>
        public static float? NullableSum(this IEnumerable<float?> sequence) {
            var result = sequence.Sum();
            D.Assert(result.HasValue);  // Sum() will never return a null result
            if (result.Value == Constants.ZeroF && !sequence.IsNullOrEmpty() && sequence.All(fVal => !fVal.HasValue)) {
                // if the result is zero, then that result is not valid IFF the entire sequence is filled with null
                result = null;
            }
            return result;
        }

        /// <summary>
        /// Aggregates the nullable values provided and returns their addition-based sum. If one or more of these
        /// nullable values has no value (its null), it is excluded from the sum. If all values are null, the value returned is null.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public static int? NullableSum(this int? source, params int?[] args) {
            var argList = new List<int?>(args);
            argList.Add(source);
            return argList.NullableSum();
        }

        /// <summary>
        /// Aggregates the nullable values provided and returns their addition-based sum. If one or more of these
        /// nullable values has no value (its null), it is excluded from the sum. If all values are null, the value returned is null.
        /// </summary>
        /// <param name="sequence">The nullable values.</param>
        /// <returns></returns>
        public static int? NullableSum(this IEnumerable<int?> sequence) {
            var result = sequence.Sum();
            D.Assert(result.HasValue);  // Sum() will never return a null result
            if (result.Value == Constants.Zero && !sequence.IsNullOrEmpty() && sequence.All(fVal => !fVal.HasValue)) {
                // if the result is zero, then that result is not valid IFF the entire sequence is filled with null
                result = null;
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
        /// <param name="onChanged">The subscriber's no parameter/no return method to call when the property changed.</param>
        public static IDisposable SubscribeToPropertyChanged<TSource, TProp>(this TSource source, Expression<Func<TSource, TProp>> propertySelector, Action onChanged) where TSource : INotifyPropertyChanged {
            Utility.ValidateNotNull(source);
            Utility.ValidateNotNull(propertySelector);
            Utility.ValidateNotNull(onChanged);

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
        /// <param name="onChanging">The subscriber's one parameter/no return method to call when the property is in the process of changing.</param>
        /// <returns>IDisposable for unsubscribing</returns>
        public static IDisposable SubscribeToPropertyChanging<TSource, TProp>(this TSource source, Expression<Func<TSource, TProp>> propertySelector, Action<TProp> onChanging) where TSource : INotifyPropertyChanging {
            Utility.ValidateNotNull(source);
            Utility.ValidateNotNull(propertySelector);
            Utility.ValidateNotNull(onChanging);

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

