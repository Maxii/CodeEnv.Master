// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CursorHudTextEvent.cs
// AGameEvent containing a StringBuilder destined for the CursorHud. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Text;

    /// <summary>
    /// AGameEvent containing a StringBuilder destined for the CursorHud. 
    /// WARNING - appears to introduce lots of lag.
    /// </summary>
    [Obsolete]
    public class CursorHudTextEvent : AStringBuilderTextEvent {

        public CursorHudTextEvent(Object source, StringBuilder sb) : base(source, sb) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

