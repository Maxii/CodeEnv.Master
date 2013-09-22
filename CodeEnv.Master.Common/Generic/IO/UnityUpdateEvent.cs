// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnityUpdateEvent.cs
// Event indicating that the final Unity Update call this frame has just occurred.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;

    /// <summary>
    /// Event indicating that the final Unity Update call this frame has just occurred.
    /// </summary>
    public class UnityUpdateEvent : AGameEvent {

        public UnityUpdateEvent(Object source) : base(source) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

