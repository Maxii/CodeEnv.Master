// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameClockEvent.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    public class GameClockEvent : GameEvent {

        public GameClockCommand ClockCommand { get; private set; }

        public GameClockEvent(GameClockCommand clockCmd) {
            ClockCommand = clockCmd;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

