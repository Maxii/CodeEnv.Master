// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PropertyChangingValueEventArgs.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.ComponentModel;

    public class PropertyChangingValueEventArgs<T> : PropertyChangingEventArgs {

        public T Newvalue { get; private set; }

        public PropertyChangingValueEventArgs(string propertyName, T newValue)
            : base(propertyName) {
            Newvalue = newValue;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

