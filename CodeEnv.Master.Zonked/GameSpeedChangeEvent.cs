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


    public class GameSpeedChangeEvent : AEnumValueChangeEvent<GameClockSpeed> {

        public GameSpeedChangeEvent(object source, GameClockSpeed newSpeed)
            : base(source, newSpeed) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

