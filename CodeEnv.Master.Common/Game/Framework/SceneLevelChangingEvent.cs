// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SceneLevelChangingEvent.cs
// Event indicating a SceneLevel change via Application.LoadLevel(level) is imminent.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    public class SceneLevelChangingEvent : AGameEvent {

        public SceneLevel NewSceneLevel { get; private set; }

        public SceneLevelChangingEvent(object source, SceneLevel newLevel)
            : base(source) {
            NewSceneLevel = newLevel;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

