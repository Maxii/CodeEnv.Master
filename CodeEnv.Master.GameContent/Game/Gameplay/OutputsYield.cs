// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OutputsYield.cs
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
    using System.Text;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable data container holding nullable yield values associated with Outputs.
    /// <remarks>11.8.16 I've considered changing to a class, but I really want to 
    /// use value semantics here. See Marc Gravell's comments at 
    /// http://stackoverflow.com/questions/10415157/when-would-a-value-type-contain-a-reference-type 
    /// </remarks>
    /// </summary>
    public struct OutputsYield : IEquatable<OutputsYield> {

        private const string Unknown = Constants.QuestionMark;
        private const string DebugNameFormat = "{0}.{1}";
        private const string FirstOutputStringBuilderFormat = "{0}({1})"; // use of [ ] causes Ngui label problems
        private const string ContinuingStringBuilderFormat = ", {0}({1})";

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(OutputsYield left, OutputsYield right) {
            return left.Equals(right);
        }

        public static bool operator !=(OutputsYield left, OutputsYield right) {
            return !left.Equals(right);
        }

        /// <summary>
        /// Adds the two OutputsYields together returning a OutputsYield that represents the combined value.
        /// <remarks>Overrides normal float? addition behaviour which returns null if either value is null.</remarks>
        /// <remarks>For each OutputID present, sums their values that aren't null, ignoring those that are. 
        /// If both values are null, that OutputID will have a null combined value. My purpose is to not throw
        /// away known info and treat unknown info as unknown and therefore ignored.
        /// </remarks>
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns></returns>
        public static OutputsYield operator +(OutputsYield left, OutputsYield right) {
            HashSet<OutputValuePair> combinedValuePairs = new HashSet<OutputValuePair>();

            var allIDs = Enums<OutputID>.GetValues(excludeDefault: true);
            foreach (var id in allIDs) {
                float? leftValue = null;
                bool leftIdIsPresent = left.IsPresent(id);
                if (leftIdIsPresent) {
                    leftValue = left.GetYield(id);
                }
                float? rightValue = null;
                bool rightIdIsPresent = right.IsPresent(id);
                if (rightIdIsPresent) {
                    rightValue = right.GetYield(id);
                }
                if (leftIdIsPresent || rightIdIsPresent) {
                    float? combinedValue = leftValue.NullableSum(rightValue);
                    combinedValuePairs.Add(new OutputValuePair(id, combinedValue));
                }
            }
            return new OutputsYield(combinedValuePairs.ToArray());
        }

        public static OutputsYield operator *(OutputsYield yieldToScale, float scaler) {
            HashSet<OutputValuePair> scaledValuePairs = new HashSet<OutputValuePair>();

            var allIDs = Enums<OutputID>.GetValues(excludeDefault: true);
            foreach (var id in allIDs) {
                float? leftValue = null;
                bool leftIdIsPresent = yieldToScale.IsPresent(id);
                if (leftIdIsPresent) {
                    leftValue = yieldToScale.GetYield(id);
                }
                if (leftIdIsPresent) {
                    float? scaledValue = leftValue.HasValue ? leftValue.Value * scaler : leftValue;
                    scaledValuePairs.Add(new OutputValuePair(id, scaledValue));
                }
            }
            return new OutputsYield(scaledValuePairs.ToArray());
        }

        public static OutputsYield operator *(float scaler, OutputsYield yieldToScale) {
            return yieldToScale * scaler;
        }

        // 10.20.17 For subtraction use left.Subtract(right). See extension comments for limitations

        #endregion

        private static StringBuilder _stringBuilder = new StringBuilder();

        public static OutputsYield OneProduction = new OutputsYield(OutputID.Production, Constants.OneF);   // after _stringBuilder

        private string _debugName;
        public string DebugName {
            get {
                if (_debugName.IsNullOrEmpty()) {
                    _debugName = DebugNameFormat.Inject(GetType().Name, OutputID.None.GetValueName());
                }
                return _debugName;
            }
        }

        private IDictionary<OutputID, float?> _outputValueLookup;
        private IDictionary<OutputID, float?> OutputValueLookup {
            get { return _outputValueLookup = _outputValueLookup ?? new Dictionary<OutputID, float?>(OutputIDEqualityComparer.Default); }
        }

        /// <summary>
        /// Gets a copy of all the OutputIDs present in this OutputsYield. Can be empty. 
        /// To get the yield of each output use GetYield(OutputID).
        /// </summary>
        /// <returns></returns>
        public IEnumerable<OutputID> OutputsPresent { get { return OutputValueLookup.Keys.ToArray(); } }

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputsYield"/> struct. 
        /// OutputID.None is illegal.
        /// </summary>
        /// <param name="outputID">The output ID.</param>
        /// <param name="value">The value.</param>
        public OutputsYield(OutputID outputID, float? value)
            : this(new OutputValuePair(outputID, value)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputsYield" /> struct.
        /// OutputID.None is illegal.
        /// </summary>
        /// <param name="outputValuePairs">The output value pairs.</param>
        /// <exception cref="System.ArgumentException">if more than 1 value pair specifying the same Resource are found.</exception>
        public OutputsYield(params OutputValuePair[] outputValuePairs)
            : this() {
            __Validate(outputValuePairs);

            foreach (var vp in outputValuePairs) {
                OutputValueLookup.Add(vp.OutputID, vp.Value);
            }

            // as a struct field, _debugName must be assigned in the Constructor
            _debugName = ToColorizedString(Constants.FormatFloat_1DpMax, useNetIncome: false);
        }

        public bool IsPresent(OutputID outputID) { return OutputValueLookup.ContainsKey(outputID); }

        /// <summary>
        /// Returns <c>true</c> if the OutputID is present in which case yield can still be null (unknown value);
        /// If the OutputID is not present, then will return <c>false</c> with a yield of Zero.
        /// </summary>
        /// <param name="outputID">The OutputID.</param>
        /// <param name="yield">The yield.</param>
        /// <returns></returns>
        [Obsolete("Dangerous when false as default yield returned (null) means unknown! Use IsPresent and GetYield in combination instead.")]
        public bool TryGetYield(OutputID outputID, out float? yield) {
            bool isOutputPresent = OutputValueLookup.TryGetValue(outputID, out yield);
            if (!isOutputPresent) {
                yield = Constants.ZeroF;
            }
            return isOutputPresent;
        }

        /// <summary>
        /// Gets the yield for this OutputID. If not present the yield returned will be Zero. If present,
        /// the yield can be null (unknown).
        /// </summary>
        /// <param name="outputID">The OutputID.</param>
        /// <returns></returns>
        public float? GetYield(OutputID outputID) {
            float? yield;
            if (!OutputValueLookup.TryGetValue(outputID, out yield)) {
                yield = Constants.ZeroF;
            }
            return yield;
        }

        [Obsolete("Not currently used")]
        public bool TryGetOutput(OutputID id, out OutputValuePair valuePair) {
            float? yield;
            if (OutputValueLookup.TryGetValue(id, out yield)) {
                valuePair = new OutputValuePair(id, yield);
                return true;
            }
            valuePair = default(OutputValuePair);
            return false;
        }

        /// <summary>
        /// Returns a OutputsYield representing the output if present.
        /// Will be the default value if the output is not present.
        /// </summary>
        /// <param name="id">The OutputID.</param>
        /// <returns></returns>
        public OutputsYield GetOutput(OutputID id) {
            float? yield;
            if (OutputValueLookup.TryGetValue(id, out yield)) {
                return new OutputsYield(id, yield);
            }
            return default(OutputsYield);
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is OutputsYield)) { return false; }
            return Equals((OutputsYield)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See Page 254, C# 4.0 in a Nutshell.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                int hash = 17;  // 17 = some prime number
                foreach (var key in OutputValueLookup.Keys) {
                    hash = hash * 31 + key.GetHashCode(); // 31 = another prime number
                    hash = hash * 31 + OutputValueLookup[key].GetHashCode();
                }
                return hash;
            }
        }

        #endregion

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// <remarks>If yieldHasValueFormat is for an int, the yield value will be truncated.</remarks>
        /// </summary>
        /// <param name="yieldHasValueFormat">The numeric format to use when the yield has value (is not null).</param>
        /// <param name="useNetIncome">if set to <c>true</c> [use net income].</param>
        /// <returns></returns>
        public string ToColorizedString(string yieldHasValueFormat, bool useNetIncome) {
            _stringBuilder.Clear();
            bool isFirst = true;

            IEnumerable<OutputID> idsToInclude = useNetIncome ? OutputValueLookup.Keys.Except(OutputID.Income, OutputID.Expense) : OutputValueLookup.Keys.Except(OutputID.NetIncome);
            foreach (var id in idsToInclude) {
                float? yield = OutputValueLookup[id];
                string format = isFirst ? FirstOutputStringBuilderFormat : ContinuingStringBuilderFormat;
                isFirst = false;

                GameColor color = GameColor.White;
                string yieldText = Unknown;
                if (yield.HasValue) {
                    if (id == OutputID.Income) {
                        color = GameColor.Green;
                    }
                    else if (id == OutputID.Expense) {
                        color = GameColor.Red;
                    }
                    else if (id == OutputID.NetIncome) {
                        color = yield.Value < Constants.ZeroF ? GameColor.Red : GameColor.Green;
                    }
                    yieldText = yieldHasValueFormat.Inject(yield.Value).SurroundWith(color);
                }
                _stringBuilder.AppendFormat(format, id.GetEnumAttributeText(), yieldText);
            }
            string result = _stringBuilder.ToString();
            result = !result.IsNullOrEmpty() ? result : DebugName;
            return result;
        }

        public override string ToString() { return DebugName; }

        #region Debug

        private void __Validate(OutputValuePair[] valuePairs) {
            // To simplify possible combinations, if Income or NetIncome are present, the other should be too as well as Expense
            var outputIDs = valuePairs.Select(vp => vp.OutputID);
            if (outputIDs.Contains(OutputID.Income)) {
                D.Assert(outputIDs.Contains(OutputID.NetIncome));
                D.Assert(outputIDs.Contains(OutputID.Expense));
                var incomeRelatedValues = valuePairs.Where(vp => vp.OutputID == OutputID.Income || vp.OutputID == OutputID.NetIncome).Select(vp => vp.Value);
                D.Assert(incomeRelatedValues.All(irv => irv.HasValue) || incomeRelatedValues.All(irv => !irv.HasValue));    // Income and NetIncome should also be consistent having value
                if (incomeRelatedValues.All(irv => irv.HasValue)) {
                    D.Assert(valuePairs.Single(vp => vp.OutputID == OutputID.Expense).Value.HasValue);  // if income-related have value, expense should too
                }
            }
            else if (outputIDs.Contains(OutputID.NetIncome)) {
                D.Assert(outputIDs.Contains(OutputID.Income));
                D.Assert(outputIDs.Contains(OutputID.Expense));
                var incomeRelatedValues = valuePairs.Where(vp => vp.OutputID == OutputID.Income || vp.OutputID == OutputID.NetIncome).Select(vp => vp.Value);
                D.Assert(incomeRelatedValues.All(irv => irv.HasValue) || incomeRelatedValues.All(irv => !irv.HasValue));    // Income and NetIncome should also be consistent having value
                if (incomeRelatedValues.All(irv => irv.HasValue)) {
                    D.Assert(valuePairs.Single(vp => vp.OutputID == OutputID.Expense).Value.HasValue);  // if income-related have value, expense should too
                }
            }

            __CheckForDuplicateValues(valuePairs);
        }

        private void __CheckForDuplicateValues(OutputValuePair[] valuePairs) {
            var duplicates = valuePairs.GroupBy(vp => vp.OutputID).Where(group => group.Count() > 1);
            if (duplicates.Any()) {
                string duplicateOutputTypes = duplicates.Select(group => group.Key).Concatenate();
                throw new ArgumentException("Duplicate {0} values found: {1}.".Inject(typeof(OutputID).Name, duplicateOutputTypes));
            }
        }

        #endregion

        #region IEquatable<OutputsYield> Members

        public bool Equals(OutputsYield other) {
            IEnumerable<OutputID> outputIDsPresent = OutputValueLookup.Keys;
            bool keysEqual = outputIDsPresent.OrderBy(id => id).SequenceEqual(other.OutputValueLookup.Keys.OrderBy(id => id));
            if (!keysEqual) {
                return false;
            }
            foreach (var id in outputIDsPresent) {
                if (GetYield(id) != other.GetYield(id)) {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Nested Classes

        public struct OutputValuePair : IEquatable<OutputValuePair> {

            private const string ToStringFormat = "{0}[{1}]";

            #region Operators Override

            // see C# 4.0 In a Nutshell, page 254

            public static bool operator ==(OutputValuePair left, OutputValuePair right) {
                return left.Equals(right);
            }

            public static bool operator !=(OutputValuePair left, OutputValuePair right) {
                return !left.Equals(right);
            }

            public static OutputValuePair operator +(OutputValuePair left, OutputValuePair right) {
                D.AssertEqual(left.OutputID, right.OutputID);
                var newValue = left.Value + right.Value;
                return new OutputValuePair(left.OutputID, newValue);
            }

            #endregion

            public OutputID OutputID { get; private set; }
            public float? Value { get; private set; }

            public OutputValuePair(OutputID outputID, float? value)
                : this() {
                OutputID = outputID;
                Value = value;
            }

            #region Object.Equals and GetHashCode Override

            public override bool Equals(object obj) {
                if (!(obj is OutputValuePair)) { return false; }
                return Equals((OutputValuePair)obj);
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// See Page 254, C# 4.0 in a Nutshell.
            /// </summary>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
            /// </returns>
            public override int GetHashCode() {
                int hash = 17;  // 17 = some prime number
                hash = hash * 31 + OutputID.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + Value.GetHashCode();
                return hash;
            }

            #endregion

            public override string ToString() {
                string valueText = Value.HasValue ? Constants.FormatFloat_1DpMax.Inject(Value.Value) : Unknown;
                return ToStringFormat.Inject(OutputID.GetValueName(), valueText);
            }

            #region IEquatable<OutputValuePair> Members

            public bool Equals(OutputValuePair other) {
                return OutputID == other.OutputID && Value == other.Value;
            }

            #endregion

        }

        #endregion

    }
}

