// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameSpeedChangeEvent.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common.LocalResources;

    public class GameSpeedChangeEvent : GameEvent {

        public GameClockSpeed GameSpeed { get; private set; }

        public GameSpeedChangeEvent(GameClockSpeed newSpeed) {
            GameSpeed = newSpeed;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

