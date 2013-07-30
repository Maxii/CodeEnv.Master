// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AStringBuilderTextEvent.cs
// Abstract Base AGameEvent that holds a StringBuilder.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR


namespace CodeEnv.Master.Common {

    using System;
    using System.Text;

    /// <summary>
    /// Abstract Base AGameEvent that holds a StringBuilder.
    /// </summary>
    public abstract class AStringBuilderTextEvent : AGameEvent {

        /// <summary>
        /// Text in the form of a StringBuilder.
        /// </summary>
        public StringBuilder SbText { get; private set; }

        public AStringBuilderTextEvent(Object source, StringBuilder sb)
            : base(source) {
            SbText = sb;
        }
    }
}

