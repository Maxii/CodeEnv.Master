// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameStateChangeEvent.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    public class GameStateChangeEvent : GameEvent {

        public GameState NewState { get; private set; }

        public GameStateChangeEvent(GameState newState) {
            NewState = newState;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

