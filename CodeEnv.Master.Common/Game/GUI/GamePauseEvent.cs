// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GamePauseEvent.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common.LocalResources;

    public class GamePauseEvent : GameEvent {

        public bool Paused { get; private set; }

        public GamePauseEvent(bool paused) {
            Paused = paused;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

