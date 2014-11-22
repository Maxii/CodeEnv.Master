// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SaveGameEvent.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    public class SaveGameEvent : AGameEvent {

        public string GameName { get; private set; }

        public SaveGameEvent(object source, string gameName)
            : base(source) {
            GameName = gameName;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }


    }
}

