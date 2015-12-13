﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameStateChangedEvent.cs
// Event called after the GameState has been changed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Event called after the GameState has been changed.
    /// </summary>
    public class GameStateChangedEvent : AEnumValueChangeEvent<GameState> {

        public GameStateChangedEvent(object source, GameState newState)
            : base(source, newState) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

