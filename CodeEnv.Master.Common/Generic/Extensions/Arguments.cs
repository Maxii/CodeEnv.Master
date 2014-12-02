// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Arguments.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// A collection of static utility methods for common argument
    ///validation. Replaces if statements at the start of a method with more
    ///compact and readable method calls.
    ///
    /// @author James L. Evans. Derived from javapractices.com. 
    /// </summary>
    public static class Arguments {

        /// <summary>
        /// Validates that the provided argument is not null. Commonly used to check method parameters. If the parameter is used to
        /// call a method, then an exception is automatically thrown and this check
        /// is not needed. If the parameter is simply assigned to another field, or
        /// if it is simply passed onto another method as a parameter, then this
        /// explicit test can be useful.
        /// </summary>
        /// <param name="arg">The arg.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void ValidateNotNull(object arg) {
            if (arg == null) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentNullException(ErrorMessages.Null.Inject(callingMethodName));
            }
        }

        /// <summary>
        /// Validates the provided text is not null and that it contains non-whitespace content.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <exception cref="ArgumentException">SbText is null or has no content.</exception>
        public static void ValidateForContent(string text) {
            if (!Utility.CheckForContent(text)) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentNullException(ErrorMessages.EmptyOrNullString.Inject(callingMethodName));
            }
        }

        /// <summary>
        /// Validates the provided number is within the designated range, inclusive.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="low">The acceptable low.</param>
        /// <param name="high">The acceptable high.</param>
        /// <exception cref="IllegalArgumentException"></exception>
        public static void ValidateForRange(int number, int low, int high) {
            if (!Utility.IsInRange(number, low, high)) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentOutOfRangeException(ErrorMessages.OutOfRange.Inject(number, low, high, callingMethodName));
            }
        }

        /// <summary>
        /// Validates the provided number is within the designated range, inclusive.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="low">The low.</param>
        /// <param name="high">The high.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void ValidateForRange(float number, float low, float high) {
            if (!Utility.IsInRange(number, low, high)) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentOutOfRangeException(ErrorMessages.OutOfRange.Inject(number, low, high, callingMethodName));
            }
        }

        /// <summary>
        /// Validates the provided number(s) are not negative.
        /// </summary>
        /// <param name="numbers">The numbers to validate.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void ValidateNotNegative(params int[] numbers) {
            numbers.ForAll(n => {
                if (n < Constants.Zero) {
                    string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                    throw new ArgumentOutOfRangeException(ErrorMessages.NegativeValue.Inject(n, callingMethodName));

                }
            });
        }

        /// <summary>
        /// Validates the provided number(s) are not negative.
        /// </summary>
        /// <param name="numbers">The numbers to validate.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void ValidateNotNegative(params float[] numbers) {
            numbers.ForAll(n => {
                if (n < Constants.ZeroF) {
                    string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                    throw new ArgumentOutOfRangeException(ErrorMessages.NegativeValue.Inject(n, callingMethodName));

                }
            });
        }

        /// <summary>
        /// Validates the provided IEnumerable is not empty or null;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable">The collection.</param>
        /// <exception cref="System.ArgumentException">enumerable is null or empty</exception>
        public static void ValidateNotNullOrEmpty<T>(IEnumerable<T> enumerable) {
            if (enumerable.IsNullOrEmpty()) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentException(ErrorMessages.CollectionEmpty.Inject(callingMethodName));
            }
        }


        /// <summary>
        /// Validates the objects provided are all of Type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args">The args.</param>
        /// <exception cref="System.ArgumentException"></exception>
        public static void ValidateType<T>(params object[] args) {
            foreach (object arg in args) {
                if (arg.GetType() != typeof(T)) {
                    string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                    throw new ArgumentException(ErrorMessages.IncorrectType.Inject(arg.GetType(), typeof(T), callingMethodName));
                }
            }
        }

        public static void ValidateTypeAndLength<T>(int length, params object[] args) {
            if (args.Length != length) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentException(ErrorMessages.IncorrectLength.Inject(args.Length, length, callingMethodName));
            }
            foreach (object arg in args) {
                if (arg.GetType() != typeof(T)) {
                    string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                    throw new ArgumentException(ErrorMessages.IncorrectType.Inject(arg.GetType(), typeof(T), callingMethodName));
                }
            }
        }

        /// <summary>
        /// General purpose validation method that throws an exception if isValid is false.
        /// </summary>
        /// <param name="isValid">if set to <c>true</c> [is valid].</param>
        /// <exception cref="System.ArgumentException"></exception>
        public static void Validate(bool isValid) {
            if (!isValid) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentException(ErrorMessages.InvalidArguments.Inject(callingMethodName));
            }
        }

    }
}

