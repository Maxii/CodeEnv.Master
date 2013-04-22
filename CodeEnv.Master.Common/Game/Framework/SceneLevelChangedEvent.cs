// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SceneLevelChangedEvent.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {


    public class SceneLevelChangedEvent : AGameEvent {

        public SceneLevel Level { get; private set; }

        public SceneLevelChangedEvent(object source, SceneLevel level)
            : base(source) {
            Level = level;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }


    }
}

