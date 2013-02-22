// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PauseGameEvent.cs
// Event containing a PauseGameCommand from GameManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Event containing a PauseGameCommand from GameManager.
    /// </summary>
    public class PauseGameEvent : GameEvent {

        public PauseGameCommand PauseCmd { get; private set; }

        public PauseGameEvent(PauseGameCommand pauseCmd) {
            PauseCmd = pauseCmd;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

