// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameStateChangingEvent.cs
//  Event called when the GameState is about to be changed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Event called when the GameState is about to be changed.
    /// </summary>
    public class GameStateChangingEvent : AGameEvent {

        public GameState NewState { get; private set; }

        public GameStateChangingEvent(object source, GameState newState)
            : base(source) {
            NewState = newState;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

