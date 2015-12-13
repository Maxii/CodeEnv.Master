// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GamePauseStateChangingEvent.cs
// Event indicating the GamePauseState is about to change.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Event indicating the GamePauseState is about to change.
    /// </summary>
    public class GamePauseStateChangingEvent : AEnumValueChangeEvent<GamePauseState> {

        public GamePauseStateChangingEvent(object source, GamePauseState pauseState)
            : base(source, pauseState) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

