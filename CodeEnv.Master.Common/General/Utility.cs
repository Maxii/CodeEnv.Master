// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: General.cs
// TODO - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.General {

    using System;
    using System.Collections.Generic;
    using System.Resources;
    using CodeEnv.Master.Common.Extensions;
    using CodeEnv.Master.Common.ResourceMgmt;

    /// <summary>
    /// TODO 
    /// </summary>
    public static class Utility {

        private static ResourceMgrFactory resourceMgrFactory = ResourceMgrFactory.Instance;

        /// <summary>
        /// Determines whether a value is in the range low..high, inclusive.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="low">The acceptable low.</param>
        /// <param name="high">The acceptable high.</param>
        /// <returns>
        ///   <c>true</c> if in range; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentException">Low is greater than High.</exception>
        public static bool IsInRange(int number, int low, int high) {
            if (low > high) {
                string errorMsg = resourceMgrFactory.GetString(ResourceMgrFactory.ResourceFileName.ErrorStrings, "ArgumentException_LowGreaterThanHigh");
                throw new ArgumentException(errorMsg.Inject(low, high));
            }
            return ((low <= number) && (number <= high));
        }

        /// <summary>
        /// Parses the boolean.
        /// </summary>
        /// <param name="boolString">The boolean string.</param>
        /// <returns> Returns <c>true</c> if bool parameter equals "true" (ignoring
        /// case), or <c>false</c>  if bool equals "false" (ignoring case).</returns>
        /// <exception cref="System.ArgumentException">Cannot parse into Boolean: 
        ///                                                    + boolString</exception>
        public static bool ParseBoolean(string boolString) {
            if (boolString.Equals("true", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
            else if (boolString.Equals("false", StringComparison.OrdinalIgnoreCase)) {
                return false;
            }
            else {
                throw new ArgumentException("Cannot parse into Boolean: "
                                                   + boolString);
            }
        }

        /// <summary>
        /// Checks for legal content - ie. not null, not empty and not all whitespace.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public static bool CheckForContent(string text) {
            bool result = true;
            if (String.IsNullOrEmpty(text) || text.Trim().Length == 0) {
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Validates access to the method or object throwing an Exception if access is not valid.
        /// </summary>
        /// <param name="accessIsValid">if set to <c>true</c> [is valid].</param>
        /// <exception cref="System.MethodAccessException"></exception>
        public static void ValidateAccess(bool accessIsValid) {
            if (!accessIsValid) {
                String msg =
                        "Accessing an object or method that is either not initialized, closed or otherwise invalid.";
                throw new MethodAccessException(msg);
            }
        }
    }
}

