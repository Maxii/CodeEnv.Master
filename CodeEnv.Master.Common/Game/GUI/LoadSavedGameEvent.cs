// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LoadSavedGameEvent.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    public class LoadSavedGameEvent : AGameEvent {

        public string GameID { get; private set; }
        // TODO other things needed to rebuild a saved game go here

        public LoadSavedGameEvent(object source, string gameID)
            : base(source) {
            GameID = gameID;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

