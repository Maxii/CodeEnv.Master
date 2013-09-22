// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CustomStringComparer.cs
// Custom Comparer class for strings that makes it easy to put selected strings first in order.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;

    /// <summary>
    /// Custom Comparer class for strings that makes it easy to put selected strings first in order.
    /// Typically used with OrderBy&lt;string&gt;(s => s, new CustomStringComparer(StringComparer.CurrentCulture, firsts))
    /// </summary>
    public class CustomStringComparer : IComparer<string> {

        private string[] _firstOrder;
        private readonly IComparer<string> _baseComparer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomStringComparer"/> class.
        /// </summary>
        /// <param name="baseComparer">The base comparer, typically StringComparer.CurrentCulture.</param>
        /// <param name="firstOrder">The first order.</param>
        public CustomStringComparer(IComparer<string> baseComparer, params string[] firstOrder) {
            _baseComparer = baseComparer;
            _firstOrder = firstOrder;
        }

        public int Compare(string x, string y) {
            if (_baseComparer.Compare(x, y) == 0) {
                return 0;
            }

            if (!_firstOrder.IsNullOrEmpty()) {
                foreach (var s in _firstOrder) {
                    if (_baseComparer.Compare(x, s) == 0) {
                        return -1;
                    }
                    if (_baseComparer.Compare(y, s) == 0) {
                        return 1;
                    }
                }
            }
            return _baseComparer.Compare(x, y);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

