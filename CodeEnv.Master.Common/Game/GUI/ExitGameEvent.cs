// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ExitGameEvent.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    public class ExitGameEvent : AGameEvent {

        public ExitGameEvent(object source) : base(source) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

