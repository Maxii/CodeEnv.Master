// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Reference.cs
// Simple wrapper class that provides access to a variable.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;

    /// <summary>
    /// Simple wrapper class that provides access to a variable. Used when you
    /// want to be able to get or set the current value of a field in another class without having 
    /// direct access to that field.
    /// Usage:
    /// int variableYouWantToAccess;
    /// Reference&lt;int&gt; wrapper = new Reference&lt;int&gt;(() => variableYouWantToAccess, z => {variableYouWantToAccess = z;});
    /// wrapper.Value = 123;    // variableYouWantToAccess value is now 123
    /// </summary>
    /// <see cref="http://stackoverflow.com/questions/2980463/how-do-i-assign-by-reference-to-a-class-field-in-c/2982037#2982037"/>
    /// <typeparam name="T"></typeparam>
    public sealed class Reference<T> {

        private readonly Func<T> _getter;
        private readonly Action<T> _setter;

        public Reference(Func<T> getter, Action<T> setter = null) {
            _getter = getter;
            _setter = setter;
        }

        public T Value {
            get { return _getter(); }
            set {
                if (_setter == null) {
                    throw new InvalidOperationException("Attempting to set value of {0}.".Inject(_getter.Target));
                }
                _setter(value);
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

