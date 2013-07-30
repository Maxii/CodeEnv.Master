// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GamePauseStateChangeEvent.cs
// Event indicating the GamePauseState has changed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Event indicating the GamePauseState has changed.
    /// </summary>
    public class GamePauseStateChangeEvent : AEnumValueChangeEvent<GamePauseState> {

        public GamePauseStateChangeEvent(object source, GamePauseState newPauseState)
            : base(source, newPauseState) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

