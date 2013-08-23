// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PropertyChangingValueEventArgs.cs
// Custom PropertyChangingEventArgs class that includes the proposed new value
// of a property just prior to its change.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.ComponentModel;

    /// <summary>
    /// Custom PropertyChangingEventArgs class that includes the proposed new value
    /// of a property just prior to its change. IMPORTANT: The client must cast the PropertyChangingEventArgs provided
    /// by the PropertyChanging delegate to this type in order to get access to NewValue.
    /// </summary>
    /// <remarks>http://stackoverflow.com/questions/8577207/better-propertychanged-and-propertychanging-event-handling</remarks>
    /// <typeparam name="T"></typeparam>
    public class PropertyChangingValueEventArgs<T> : PropertyChangingEventArgs {

        public T NewValue { get; private set; }

        public PropertyChangingValueEventArgs(string propertyName, T newValue)
            : base(propertyName) {
            NewValue = newValue;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

